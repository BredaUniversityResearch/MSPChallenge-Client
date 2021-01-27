using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanDetailsTabEnergy : LockablePlanDetailsTab
{
	[SerializeField]
	Distribution energyDistribution;
	[SerializeField]
	GameObject contentInfo;

	List<EnergyGrid> energyGridBackup;
	List<EnergyGrid> energyGridsBeforePlan;
	HashSet<int> energyGridRemovedBackup;
	HashSet<int> countriesAffectedByRemovedGrids;
	List<EnergyLineStringSubEntity> removedCables;

	protected override string ContentName => "energy distribution";
	protected override PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Energy;

	protected override void Initialise()
	{
		base.Initialise();
		energyDistribution.SetInteractability(false);
	}

	public override void UpdateTabAvailability()
	{
		base.UpdateTabAvailability();
		tabToggle.gameObject.SetActive(planDetails.SelectedPlan.altersEnergyDistribution);
		if (isActive && planDetails.SelectedPlan != null && !planDetails.SelectedPlan.altersEnergyDistribution)
		{
			SetEditContentButtonEnabled(false);
			planDetails.TabSelect(PlanDetails.EPlanDetailsTab.Feedback);
			SetTabActive(false);
		}
	}

	public override void UpdateTabContent()
	{
		if (!isActiveAndEnabled || lockedPlan != null)
			return;
		energyDistribution.SetSliderValuesToEnergyDistribution(planDetails.SelectedPlan, PlanManager.GetEnergyGridsBeforePlan(planDetails.SelectedPlan, EnergyGrid.GridColor.Either, true, true));
		emptyContentOverlay.SetActive(energyDistribution.NumberGroups == 0);
	}

	protected override void BeginEditing(Plan plan)
	{
		base.BeginEditing(plan);

		removedCables = LayerManager.ForceEnergyLayersActiveUpTo(plan);
		emptyContentOverlay.SetActive(false);

		energyGridBackup = lockedPlan.energyGrids;
		energyGridRemovedBackup = lockedPlan.removedGrids;
		energyGridsBeforePlan = PlanManager.GetEnergyGridsBeforePlan(lockedPlan, EnergyGrid.GridColor.Either);

		//Reset plan's grids
		List<EnergyGrid> oldGrids = lockedPlan.energyGrids; 
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
				//TODO: this adds all energygrids to the plan while that shouldnt happen!
			}
		}

		//Add countries affected by removed grids
		countriesAffectedByRemovedGrids = new HashSet<int>();
		foreach (EnergyGrid grid in energyGridsBeforePlan)
			if (lockedPlan.removedGrids.Contains(grid.persistentID))
				foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.energyDistribution.distribution)
					if (!countriesAffectedByRemovedGrids.Contains(countryAmount.Key))
						countriesAffectedByRemovedGrids.Add(countryAmount.Key);

		energyDistribution.SetSliderValuesToEnergyDistribution(planDetails.SelectedPlan, PlanManager.GetEnergyGridsBeforePlan(planDetails.SelectedPlan, EnergyGrid.GridColor.Either, true, true));
		energyDistribution.SetInteractability(true);
		contentInfo.SetActive(false);
	}
	
	protected override void StopEditing()
	{
		lockedPlan = null;
		energyGridBackup = null;
		energyGridsBeforePlan = null;
		energyGridRemovedBackup = null;
		countriesAffectedByRemovedGrids = null;
		removedCables = null;
		energyDistribution.SetInteractability(false);
		contentInfo.SetActive(true);
		emptyContentOverlay.SetActive(energyDistribution.NumberGroups == 0);

		base.StopEditing();
	}

	protected override void SubmitChangesAndUnlock()
	{
		BatchRequest batch = new BatchRequest();

		energyDistribution.SetGridsToSliderValues(lockedPlan);

		// Commit new grids (not distributions/sockets/sources yet)
		foreach (EnergyGrid grid in lockedPlan.energyGrids)
			grid.SubmitEmptyGridToServer(batch);
		// Delete grids no longer in this plan
		foreach (int gridID in GetGridsRemovedFromPlanSinceBackup())
			EnergyGrid.SubmitGridDeletionToServer(gridID, batch);
		lockedPlan.SubmitRemovedGrids(batch);
		lockedPlan.SubmitEnergyError(false, false, batch);

		//Update required approval
		Dictionary<int, EPlanApprovalState> newApproval = lockedPlan.CalculateRequiredApproval(countriesAffectedByRemovedGrids);
		lockedPlan.SubmitRequiredApproval(batch, newApproval);

		//Submit grids
		foreach (EnergyGrid grid in lockedPlan.energyGrids)
			grid.SubmitEnergyDistribution(batch);

		foreach (EnergyLineStringSubEntity cable in removedCables)
			cable.SubmitDelete(batch);

		lockedPlan.AttemptUnlock(batch);
		InterfaceCanvas.ShowNetworkingBlocker();
		batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
	}

	protected override void HandleChangesSubmissionSuccess(BatchRequest batch)
	{
		countriesAffectedByRemovedGrids = null;		
		base.HandleChangesSubmissionSuccess(batch);
	}

	public override void CancelChangesAndUnlock()
	{
		base.CancelChangesAndUnlock();

		lockedPlan.energyGrids = energyGridBackup;
		lockedPlan.removedGrids = energyGridRemovedBackup;
		LayerManager.RestoreRemovedCables(removedCables);

		lockedPlan.AttemptUnlock();
		StopEditing();
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
}

