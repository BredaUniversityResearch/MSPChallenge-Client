using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GeoJSON.Net.Feature;

public class PlanDetailsTabLayers : LockablePlanDetailsTab
{
	[SerializeField]
	Transform layerEntryParent;
	[SerializeField]
	GameObject layerEntryPrefab;

	List<PlanDetailsLayerEntry> layerEntries = new List<PlanDetailsLayerEntry>();

	private HashSet<int> countriesAffectedByRemovedGrids;
	private List<EnergyGrid> energyGridBackup;
	private List<EnergyGrid> energyGridsBeforePlan;
	private HashSet<int> energyGridRemovedBackup;
	private List<EnergyLineStringSubEntity> removedInvalidCables;
	private List<PlanIssueObject> issuesBackup;
	private bool backupMade;
	private PlanLayer currentlyEditingLayer;

	protected override string ContentName => "layers";
	protected override PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Layers;

	protected override void Initialise()
	{
		base.Initialise();
	}

	public override void UpdateTabAvailability()
	{
		base.UpdateTabAvailability();
		tabToggle.gameObject.SetActive(planDetails.SelectedPlan.PlanLayers.Count > 0);
		if (isActive && planDetails.SelectedPlan != null && planDetails.SelectedPlan.PlanLayers.Count == 0)
		{
			SetEditContentButtonEnabled(false);
			planDetails.TabSelect(PlanDetails.EPlanDetailsTab.Feedback);
			SetTabActive(false);
		}
	}

	public override void UpdateTabContent()
	{
		if (!isActiveAndEnabled || Main.InEditMode)
			return;
		UpdateLayerEntries();
	}

	protected override void BeginEditing(Plan plan)
	{
		//Should not use base, as this is diverted into the usual geom editing flow
		lockedPlan = plan;
		SetAcceptChangesButtonEnabled(true);
		PlansMonitor.instance.plansMonitorToggle.toggle.isOn = false;

		//Show the plan to be edited (also recalculates active entities)
		PlanManager.ShowPlan(plan);
		PlanDetails.SelectPlan(plan);

		if (plan.energyPlan)
		{
			removedInvalidCables = LayerManager.ForceEnergyLayersActiveUpTo(plan);
			energyGridsBeforePlan = PlanManager.GetEnergyGridsBeforePlan(plan, EnergyGrid.GridColor.Either);
		}

		//Enter edit mode in FSM
		InterfaceCanvas.Instance.activePlanWindow.OpenEditingUI(plan.PlanLayers[0]);
		StartEditingLayer(plan.PlanLayers[0]);

		PlansMonitor.RefreshPlanButtonInteractablity();
	}

	/// <summary>
	/// Called after a plan is locked and when the player switches between layers while editing.
	/// </summary>
	public void StartEditingLayer(PlanLayer layer, bool calledByUndo = false)
	{
		if (!calledByUndo)
			Main.FSM.SetInterruptState(null);

		if (currentlyEditingLayer != null)
		{
			UIManager.SetLayerVisibilityLock(currentlyEditingLayer.BaseLayer, false);
			if (!calledByUndo)
				Main.FSM.AddToUndoStack(new SwitchLayerOperation(currentlyEditingLayer, layer));
		}

		InterfaceCanvas.Instance.activePlanWindow.StartEditingLayer(layer);
		UIManager.SetLayerVisibilityLock(layer.BaseLayer, true);
		currentlyEditingLayer = layer;
		lockedPlan = layer.Plan;
		UIManager.StartEditingLayer(layer.BaseLayer);
		Main.FSM.StartEditingLayer(layer);
		LayerManager.RedrawVisibleLayers();
	}

	public void ForceCancelChanges()
	{
		CancelChangesAndUnlock();
	}

	public override void CancelChangesAndUnlock()
	{
		//This already unlocks and calls StoppedEditingSuccessfully if succesful
		Main.FSM.UndoAllAndClearStacks();
		lockedPlan.energyGrids = energyGridBackup;
		lockedPlan.removedGrids = energyGridRemovedBackup;
		if (issuesBackup != null)
		{
			IssueManager.instance.SetIssuesForPlan(lockedPlan, issuesBackup);
		}
		if (removedInvalidCables != null)
		{
			LayerManager.RestoreRemovedCables(removedInvalidCables);
		}
		lockedPlan.AttemptUnlock();
		StopEditing();
	}

	protected override void SubmitChangesAndUnlock()
	{
		InterfaceCanvas.ShowNetworkingBlocker();
		if (lockedPlan.energyPlan && !string.IsNullOrEmpty(Main.MspGlobalData.windfarm_data_api_url))
		{
			int nextTempID = -1;
			Dictionary<int, SubEntity> energyEntities = new Dictionary<int, SubEntity>();
			foreach (PlanLayer planLayer in lockedPlan.PlanLayers)
			{
				if (planLayer.BaseLayer.editingType == AbstractLayer.EditingType.SourcePolygon)
				{
					//Ignores removed geometry
					foreach (Entity entity in planLayer.GetNewGeometry())
					{
						//Because entities might be newly created and not have IDs, use temporary IDs.
						int id = entity.DatabaseID;
						if (id < 0)
							id = nextTempID--;

						energyEntities.Add(id, entity.GetSubEntity(0));
					}
				}
			}
			//Try getting external data before calculating the effects of editing
			ServerCommunication.DoExternalAPICall<FeatureCollection>(Main.MspGlobalData.windfarm_data_api_url, energyEntities, (result) => ExternalEnergyEffectsReturned(result, energyEntities), ExternalEnergyEffectsFailed);
		}
		else
		{
			CalculateEffectsOfEditing();
		}
	}

	void ExternalEnergyEffectsReturned(FeatureCollection collection, Dictionary<int, SubEntity> passedEnergyEntities)
	{
		double totalCost = 0;
		foreach (Feature feature in collection.Features)
		{
			SubEntity se;
			if (passedEnergyEntities.TryGetValue(int.Parse(feature.Id), out se))
			{
				object cost;
				if (feature.Properties.TryGetValue("levelized_cost_of_energy", out cost) && cost != null)
				{
					totalCost += (double)cost;
				}
				se.SetPropertiesToGeoJSONFeature(feature);
			}

		}
		lockedPlan.AddSystemMessage("Levelized cost of energy for windfarms in plan: " + totalCost.ToString("N0") + " €/MWh");

		CalculateEffectsOfEditing();
	}

	void ExternalEnergyEffectsFailed(ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			Debug.LogError($"External API call failed, message: {message}. Retrying {request.retriesRemaining} more times.");
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			Debug.LogError($"External API call failed, message: {message}. Using built in alternative.");
			CalculateEffectsOfEditing();
		}
	}

	/// <summary>
	/// Calculates the effect on energy grids and restrictions of the edits of the current plan.
	/// A plan should not be acceptable without its effect being calculated beforehand.
	/// Makes a backup of the plan's current energy distributions, so they can be reverted upon canceling.
	/// </summary>
	private void CalculateEffectsOfEditing()
	{
		//Aborts any geometry being created
		Main.FSM.AbortCurrentState();

		//Check invalid geometry
		SubEntity invalid = lockedPlan.CheckForInvalidGeometry();
		if (invalid != null)
		{
			CameraManager.Instance.ZoomToBounds(invalid.BoundingBox);
			DialogBoxManager.instance.NotificationWindow("Invalid geometry", "The plan contains invalid geometry and cannot be accepted until these have been fixed.", null);
			InterfaceCanvas.HideNetworkingBlocker();
			SetConfirmCancelChangesInteractable(true);
			return;
		}

		//Create a backup in case the changes are canceled
		if (!backupMade) //If effects calculated multiple times, backup is only made the first time.
		{
			energyGridBackup = lockedPlan.energyGrids;
			energyGridRemovedBackup = lockedPlan.removedGrids;
			issuesBackup = IssueManager.instance.FindIssueDataForPlan(lockedPlan);
			backupMade = true;
		}

		//Check constraints and show them in the UI.
		ConstraintManager.CheckConstraints(lockedPlan, issuesBackup, true);

		//Energy effects
		if (lockedPlan.energyPlan)
		{
			//Reset plan's grids
			List<EnergyGrid> oldGrids = lockedPlan.energyGrids; //Can't use backup as player might switch between geom and distribution multiple times
			lockedPlan.removedGrids = new HashSet<int>();
			lockedPlan.energyGrids = new List<EnergyGrid>();

			foreach (EnergyGrid grid in energyGridsBeforePlan)
				lockedPlan.removedGrids.Add(grid.persistentID);

			foreach (AbstractLayer layer in LayerManager.energyLayers)
			{
				if (layer.editingType == AbstractLayer.EditingType.Socket)
				{
					//Add results of the grids on the socket layer to the existing ones
					lockedPlan.energyGrids.AddRange(layer.DetermineGrids(lockedPlan, oldGrids, energyGridsBeforePlan, lockedPlan.removedGrids, out lockedPlan.removedGrids));
				}
			}

			//Add countries affected by removed grids
			countriesAffectedByRemovedGrids = new HashSet<int>();
			foreach (EnergyGrid grid in energyGridsBeforePlan)
				if (lockedPlan.removedGrids.Contains(grid.persistentID))
					foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.energyDistribution.distribution)
						if (!countriesAffectedByRemovedGrids.Contains(countryAmount.Key))
							countriesAffectedByRemovedGrids.Add(countryAmount.Key);
			lockedPlan.energyError = false;
		}

		SubmitChanges();
	}

	private void SubmitChanges()
	{
		BatchRequest batch = new BatchRequest();
		if (lockedPlan.energyPlan)
		{
			// Commit new grids (not distributions/sockets/sources yet)
			foreach (EnergyGrid grid in lockedPlan.energyGrids)
				grid.SubmitEmptyGridToServer(batch);
			// Delete grids no longer in this plan
			foreach (int gridID in GetGridsRemovedFromPlanSinceBackup())
				EnergyGrid.SubmitGridDeletionToServer(gridID, batch);
			lockedPlan.SubmitRemovedGrids(batch);
			lockedPlan.SubmitEnergyError(false, false, batch);
		}

		//Calculate and submit the countries this plan requires approval from
		Dictionary<int, EPlanApprovalState> newApproval = lockedPlan.CalculateRequiredApproval(countriesAffectedByRemovedGrids);
		lockedPlan.SubmitRequiredApproval(batch, newApproval);

		//Check issues again and submit according to latest tests. To ensure that changes in other plans while editing this plan get detected as well.
		RestrictionIssueDeltaSet issuesToSubmit = ConstraintManager.CheckConstraints(lockedPlan, issuesBackup, true);
		if (issuesToSubmit != null)
		{
			issuesToSubmit.SubmitToServer(batch);
		}
		issuesBackup = null;

		//Submit all geometry changes 
		//Automatically submits corresponding energy_output and connection for geom. 
		Main.FSM.SubmitAllChanges(batch);

		//If energy plan, submit grid content after geometry has at least a batch id
		if (lockedPlan.energyPlan && lockedPlan.energyGrids.Count > 0)
		{
			foreach (EnergyGrid grid in lockedPlan.energyGrids)
				grid.SubmitEnergyDistribution(batch);
		}
		if (removedInvalidCables != null)
		{
			foreach (EnergyLineStringSubEntity cable in removedInvalidCables)
				cable.SubmitDelete(batch);
		}

		lockedPlan.AttemptUnlock(batch);
		batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
	}

	protected override void HandleChangesSubmissionSuccess(BatchRequest batch)
	{
		countriesAffectedByRemovedGrids = null;
		Main.FSM.ClearUndoRedoAndFinishEditing();
		base.HandleChangesSubmissionSuccess(batch);
	}

	public List<int> GetGridsRemovedFromPlanSinceBackup()
	{
		List<int> result = new List<int>();
		if (energyGridBackup == null)
			return result;
		bool found;
		foreach (EnergyGrid oldGrid in energyGridBackup)
		{
			found = false;
			foreach (EnergyGrid newGrid in lockedPlan.energyGrids)
				if (newGrid.GetDatabaseID() == oldGrid.GetDatabaseID())
				{
					found = true;
					break;
				}
			if (!found)
				result.Add(oldGrid.GetDatabaseID());
		}
		return result;
	}

	protected override void StopEditing()
	{
		if (currentlyEditingLayer != null)
		{
			UIManager.SetLayerVisibilityLock(currentlyEditingLayer.BaseLayer, false);
			PlansMonitor.UpdatePlan(lockedPlan, false, false, false);
		}

		base.StopEditing();

		UIManager.StopEditing();
		Main.FSM.StopEditing();

		currentlyEditingLayer = null;
		energyGridBackup = null;
		energyGridRemovedBackup = null;
		removedInvalidCables = null;
		issuesBackup = null;
		backupMade = false;

		PlanDetails.UpdateTabAvailability();

		LayerManager.ClearNonReferenceLayers();
		LayerManager.RedrawVisibleLayers();
		InterfaceCanvas.Instance.activePlanWindow.CloseEditingUI();
		PlansMonitor.RefreshPlanButtonInteractablity();

		//Open & maximize plansmonitor
		InterfaceCanvas.Instance.menuBarPlansMonitor.toggle.isOn = true;
		PlansMonitor.instance.plansMinMax.Maximize();
	}

	public AbstractLayer CurrentlyEditingBaseLayer
	{
		get { return currentlyEditingLayer != null ? currentlyEditingLayer.BaseLayer : null; }
	}
	
	private void UpdateLayerEntries()
	{
		int nextIndex = 0;
		//Set entries

		for (; nextIndex < planDetails.SelectedPlan.PlanLayers.Count; nextIndex++)
		{
			if (nextIndex < layerEntries.Count)
			{
				layerEntries[nextIndex].SetLayer(planDetails.SelectedPlan.PlanLayers[nextIndex]);
				layerEntries[nextIndex].gameObject.SetActive(true);
			}
			else
			{
				CreateEntry(planDetails.SelectedPlan.PlanLayers[nextIndex]);
			}
		}
		
		//Turn off unused entries
		for (int i = nextIndex; i < layerEntries.Count; i++)
		{
			layerEntries[i].gameObject.SetActive(false);
		}
	}

	void CreateEntry(PlanLayer layer)
	{
		PlanDetailsLayerEntry entry = Instantiate(layerEntryPrefab, layerEntryParent).GetComponent<PlanDetailsLayerEntry>();
		entry.SetLayer(layer);
		layerEntries.Add(entry);
	}
}

