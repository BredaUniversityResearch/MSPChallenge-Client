using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MSP2050.Scripts
{
	public class ActivePlanWindow : MonoBehaviour
	{
		public delegate void EntityTypeChangeCallback(List<EntityType> newTypes);
		public delegate void TeamChangeCallback(int newTeamID);
		public delegate void ParameterChangeCallback(EntityPropertyMetaData parameter, string value);

		[Header("General")]
		[SerializeField] GenericWindow m_window;
		[SerializeField] ToggleGroup m_contentToggleGroup;
		[SerializeField] Transform m_popoutParent;

		[Header("Buttons")]
		[SerializeField] GameObject m_editButtonParent;
		[SerializeField] Button m_startEditingButton;
		[SerializeField] GameObject m_cancelAcceptButtonParent;
		[SerializeField] Button m_acceptEditButton;
		[SerializeField] Button m_cancelEditButton;
		[SerializeField] Button m_zoomToPlanButton;

		[Header("Plan info")]
		[SerializeField] Image m_countryIndicator;
		[SerializeField] CustomInputField m_planName;
		[SerializeField] CustomInputField m_planDescription;
		[SerializeField] AP_ContentToggle m_planDate;
		[SerializeField] AP_ContentToggle m_planState;
		[SerializeField] GameObject m_creationTimeSection;
		[SerializeField] CustomDropdown m_creationTimeYearDropdown;
		[SerializeField] CustomDropdown m_creationTimeMonthDropdown;

		[Header("Communication")]
		[SerializeField] GameObject m_communicationSection;
		[SerializeField] AP_ContentToggle m_communication;
		[SerializeField] AP_ContentToggle m_approval;
		[SerializeField] AP_ContentToggle m_issues;
		[SerializeField] AP_Communication m_communicationContent;
		[SerializeField] AP_Approval m_approvalContent;
		[SerializeField] AP_IssueList m_issuesContent;

		[Header("View mode")]
		[SerializeField] GameObject m_viewModeSection;
		[SerializeField] Toggle m_viewAllToggle;
		[SerializeField] Toggle m_viewPlanToggle;
		[SerializeField] Toggle m_viewBaseToggle;

		[Header("Layers")]
		[SerializeField] GameObject m_layerSection;
		[SerializeField] Transform m_layerParent;
		[SerializeField] GameObject m_layerPrefab;
		[SerializeField] AP_ContentToggle m_changeLayersToggle;
		[SerializeField] AP_GeometryTool m_geometryTool;
		[SerializeField] AP_LayerSelect m_layerSelect;

		[Header("Policies")]
		[SerializeField] GameObject m_policySection;
		[SerializeField] Transform m_policyParent;
		[SerializeField] GameObject m_policyPrefab;
		[SerializeField] AP_ContentToggle m_changePoliciesToggle;
		[SerializeField] AP_PolicySelect m_policySelect;

		private List<AP_ContentToggle> m_layerToggles = new List<AP_ContentToggle>();
		private Dictionary<string, AP_ContentToggle> m_policyToggles = new Dictionary<string, AP_ContentToggle>(); //popouts can be reached through toggles
		private AP_ContentToggle m_selectedContentToggle;

		private bool m_ignoreLayerCallback;

		//General
		private DialogBox m_cancelChangesConfirmationWindow = null;
		private Plan m_currentPlan;
		private bool m_editing;
		private bool m_creatingNew;

		//Properties
		public Plan CurrentPlan => m_currentPlan;
		public bool Editing => m_editing;
		public bool CreatingNew => m_creatingNew;

		void Awake()
		{
			m_changeLayersToggle.Initialise(this, m_layerSelect);
			m_changePoliciesToggle.Initialise(this, m_policySelect);
			m_communication.Initialise(this, m_communicationContent);
			m_approval.Initialise(this, m_approvalContent);
			m_issues.Initialise(this, m_issuesContent);

			m_startEditingButton.onClick.AddListener(OnEditButtonPressed);

			m_viewAllToggle.onValueChanged.AddListener((value) =>
			{
				if (value)
					PlanManager.Instance.SetPlanViewState(PlanManager.PlanViewState.All);
			});

			m_viewPlanToggle.onValueChanged.AddListener((value) =>
			{
				if (value)
					PlanManager.Instance.SetPlanViewState(PlanManager.PlanViewState.Changes);
			});

			m_viewBaseToggle.onValueChanged.AddListener((value) =>
			{
				if (value)
					PlanManager.Instance.SetPlanViewState(PlanManager.PlanViewState.Base);
			});

			m_zoomToPlanButton.onClick.AddListener(() =>
			{
				m_currentPlan.ZoomToPlan();
			});

			m_window.OnAttemptHideWindow = OnAttemptHideWindow;
			m_acceptEditButton.onClick.AddListener(OnCancelButton);

			//create policy popouts and toggles
			foreach (var kvp in PolicyManager.Instance.PolicyLogic)
			{
				AP_PopoutWindow popout = Instantiate(kvp.Value.m_definition.m_activePlanPrefab, m_popoutParent).GetComponent<AP_PopoutWindow>();
				popout.gameObject.SetActive(false);

				AP_ContentToggle toggle = Instantiate(m_policyPrefab, m_policyParent).GetComponent<AP_ContentToggle>();
				toggle.Initialise(this, popout);
				toggle.SetContent(kvp.Value.m_definition.m_displayName);
				m_policyToggles.Add(kvp.Key, toggle);
			}
		}

		private bool OnAttemptHideWindow()
		{
			if (m_editing)
			{
				if (m_cancelChangesConfirmationWindow == null || !m_cancelChangesConfirmationWindow.isActiveAndEnabled)
				{
					m_cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "All changes made to the plan will be lost. Are you sure you want to cancel?", null, () => ForceCancel(true));
				}
				return false;
			}
			else
			{
				m_currentPlan = null;
				PlanManager.Instance.HideCurrentPlan();
			}
			return true;
		}

		void OnCancelButton()
		{ 
			if(m_creatingNew)
			{ 

			}
			else if(m_editing)
			{
				if (m_cancelChangesConfirmationWindow == null || !m_cancelChangesConfirmationWindow.isActiveAndEnabled)
				{
					m_cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "All changes made to the plan will be lost. Are you sure you want to cancel?", null, () => ForceCancel(false));
				}
			}
		}

		void ForceCancel(bool a_closeWindow)
		{
			//TODO: add cancel code
			if (m_selectedContentToggle != null)
			{
				m_selectedContentToggle.ForceClose(false);
			}

			if (a_closeWindow)
			{
				m_currentPlan = null;
				PlanManager.Instance.HideCurrentPlan();
				gameObject.SetActive(false);
			}

			//pdt layers (cancelchangesandunlock)
			Main.Instance.fsm.UndoAllAndClearStacks();
			lockedPlan.energyGrids = energyGridBackup;
			lockedPlan.removedGrids = energyGridRemovedBackup;
			if (issuesBackup != null)
			{
				IssueManager.Instance.SetIssuesForPlan(lockedPlan, issuesBackup);
			}
			if (removedInvalidCables != null)
			{
				PolicyLogicEnergy.Instance.RestoreRemovedCables(removedInvalidCables);
			}
			lockedPlan.AttemptUnlock();
		}

		void OnAcceptButton()
		{
			if (m_selectedContentToggle != null && !m_selectedContentToggle.TryClose())
			{
				return;
			}

			InterfaceCanvas.ShowNetworkingBlocker();
			if (lockedPlan.energyPlan && !string.IsNullOrEmpty(SessionManager.Instance.MspGlobalData.windfarm_data_api_url))
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
				ServerCommunication.Instance.DoExternalAPICall<FeatureCollection>(SessionManager.Instance.MspGlobalData.windfarm_data_api_url, energyEntities, (result) => ExternalEnergyEffectsReturned(result, energyEntities), ExternalEnergyEffectsFailed);
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

		void ExternalEnergyEffectsFailed(ARequest request, string message)
		{
			if (request.retriesRemaining > 0)
			{
				Debug.LogError($"External API call failed, message: {message}. Retrying {request.retriesRemaining} more times.");
				ServerCommunication.Instance.RetryRequest(request);
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
			Main.Instance.fsm.AbortCurrentState();

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
				issuesBackup = IssueManager.Instance.FindIssueDataForPlan(lockedPlan);
				backupMade = true;
			}

			//Check constraints and show them in the UI.
			ConstraintManager.Instance.CheckConstraints(lockedPlan, issuesBackup, true);

			//Energy effects
			if (lockedPlan.energyPlan)
			{
				//Reset plan's grids
				List<EnergyGrid> oldGrids = lockedPlan.energyGrids; //Can't use backup as player might switch between geom and distribution multiple times
				lockedPlan.removedGrids = new HashSet<int>();
				lockedPlan.energyGrids = new List<EnergyGrid>();

				foreach (EnergyGrid grid in energyGridsBeforePlan)
					lockedPlan.removedGrids.Add(grid.persistentID);

				foreach (AbstractLayer layer in LayerManager.Instance.energyLayers)
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
			BatchRequest batch = new BatchRequest(true);
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
			RestrictionIssueDeltaSet issuesToSubmit = ConstraintManager.Instance.CheckConstraints(lockedPlan, issuesBackup, true);
			if (issuesToSubmit != null)
			{
				issuesToSubmit.SubmitToServer(batch);
			}
			issuesBackup = null;

			//Submit all geometry changes 
			//Automatically submits corresponding energy_output and connection for geom. 
			Main.Instance.fsm.SubmitAllChanges(batch);

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

			//Description
			lockedPlan.SetDescription(descriptionInputField.text, batch);

			//TODO: move this to policy logic
			//Shipping
			RestrictionAreaManager.instance.SubmitSettingsForPlan(lockedPlan, batch);
			//Fishing
			lockedPlan.fishingDistributionDelta.SubmitToServer(lockedPlan.ID, batch);

			lockedPlan.AttemptUnlock(batch);
			batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
		}

		void HandleChangesSubmissionSuccess(BatchRequest batch)
		{
			countriesAffectedByRemovedGrids = null;
			Main.Instance.fsm.ClearUndoRedoAndFinishEditing();
			base.HandleChangesSubmissionSuccess(batch);
		}

		void HandleChangesSubmissionFailure(BatchRequest batch)
		{
			InterfaceCanvas.HideNetworkingBlocker();
			DialogBoxManager.instance.NotificationWindow("Submitting data failed", "There was an error when submitting the plan's changes to the server. Please try again or see the error log for more information.", null);
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

		void ForceAccept()
		{ 
		
		}

		public void OnEditButtonPressed()
		{
			Main.Instance.PreventPlanAndTabChange = true;
			PlansMonitor.RefreshPlanButtonInteractablity();

			m_currentPlan.AttemptLock(
				(plan) =>
				{
					Main.Instance.PreventPlanAndTabChange = false;
					EnterEditMode();
				},
				(plan) => {
					Main.Instance.PreventPlanAndTabChange = false;
					PlansMonitor.RefreshPlanButtonInteractablity();
				});
		}

		public void SetToPlan(Plan plan)
		{
			if(plan == null)
			{
				//TODO: open in create mode
				m_editing = true;
				m_creatingNew = true;
				m_currentPlan = new Plan();
			}
			else
			{
				m_currentPlan = plan;
				m_editing = false;
				m_creatingNew = false;
			}
			gameObject.SetActive(true);
			m_countryIndicator.color = SessionManager.Instance.FindTeamByID(plan.Country).color;
			RefreshContent();
			UpdateSectionActivity();
		}

		public void UpdateSectionActivity()
		{
			//Buttons
			m_editButtonParent.gameObject.SetActive(!m_editing && m_currentPlan != null && m_currentPlan.State == Plan.PlanState.DESIGN && (SessionManager.Instance.AreWeManager || m_currentPlan.Country == SessionManager.Instance.CurrentUserTeamID));
			m_cancelAcceptButtonParent.SetActive(m_editing);
			m_acceptEditButton.interactable = !string.IsNullOrEmpty(m_planName.text);

			//Content
			m_planName.interactable = m_editing;
			m_planDescription.interactable = m_editing;
			m_layerSection.SetActive(!m_creatingNew);
			m_policySection.SetActive(!m_creatingNew);
			m_communicationSection.SetActive(!m_creatingNew);
			m_viewModeSection.SetActive(!m_editing);
			m_changeLayersToggle.gameObject.SetActive(m_editing);
			m_changePoliciesToggle.gameObject.SetActive(m_editing);
			m_creationTimeSection.SetActive(m_creatingNew);
			m_planDate.gameObject.SetActive(!m_creatingNew);
			m_planState.gameObject.SetActive(!m_editing);
		}

		void EnterEditMode()
		{
			m_editing = true;
			//TODO

			PlansMonitor.instance.plansMonitorToggle.toggle.isOn = false;

			if (plan.energyPlan)
			{
				removedInvalidCables = PolicyLogicEnergy.Instance.ForceEnergyLayersActiveUpTo(plan);
				energyGridsBeforePlan = PlanManager.Instance.GetEnergyGridsBeforePlan(plan, EnergyGrid.GridColor.Either);
			}

			if (!m_viewAllToggle.isOn)
				m_viewAllToggle.isOn = true;
			m_viewModeSection.SetActive(false);
			m_cancelAcceptButtonParent.SetActive(true);
			m_editButtonParent.gameObject.SetActive(false);

			m_changeLayersToggle.gameObject.SetActive(true);
			m_changePoliciesToggle.gameObject.SetActive(true);
			PlansMonitor.RefreshPlanButtonInteractablity();
			UpdateSectionActivity();
		}

		void ExitEditMode()
		{
			m_editing = false;
			//TODO

			if (currentlyEditingLayer != null)
			{
				InterfaceCanvas.Instance.layerInterface.SetLayerVisibilityLock(currentlyEditingLayer.BaseLayer, false);
				PlansMonitor.UpdatePlan(lockedPlan, false, false, false);
			}

			

			//pdt layers (stopediting)
			InterfaceCanvas.Instance.StopEditing();
			Main.Instance.fsm.StopEditing();

			currentlyEditingLayer = null;
			energyGridBackup = null;
			energyGridRemovedBackup = null;
			removedInvalidCables = null;
			issuesBackup = null;
			backupMade = false;

			PlansMonitor.RefreshPlanButtonInteractablity();
			LayerManager.Instance.ClearNonReferenceLayers();
			LayerManager.Instance.RedrawVisibleLayers();
			UpdateSectionActivity();
		}

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		public void RefreshContent()
		{
			//Info
			m_planName.text = m_currentPlan.Name;
			m_planDescription.text = m_currentPlan.Description;

			//Time
			int maxConstructionTime = 0;
			foreach(PlanLayer layer in m_currentPlan.PlanLayers)
			{
				if (layer.BaseLayer.AssemblyTime > maxConstructionTime)
					maxConstructionTime = layer.BaseLayer.AssemblyTime;
			}
			if (maxConstructionTime == 0)
				m_planDate.SetContent($"Implementation in {Util.MonthToText(m_currentPlan.StartTime)}. No construction time required.");
			else if (maxConstructionTime == 1)
				m_planDate.SetContent($"Implementation in {Util.MonthToText(m_currentPlan.StartTime)}, after 1 month construction.");
			else
				m_planDate.SetContent($"Implementation in {Util.MonthToText(m_currentPlan.StartTime)}, after {maxConstructionTime} months construction.");

			//State
			m_planState.SetContent($"Plan state: {m_currentPlan.State.GetDisplayName()}", VisualizationUtil.Instance.VisualizationSettings.GetplanStateSprite(m_currentPlan.State));

			//Messages

			//Approval

			//Issues

			//Content
			SetEntriesToPolicies();
			SetEntriesToLayers();
		}

		private void SetEntriesToPolicies()
		{
			foreach (var kvp in m_policyToggles)
			{
				kvp.Value.gameObject.SetActive(false);
			}
			if(m_currentPlan.m_policies != null)
			{
				foreach(var kvp in m_currentPlan.m_policies)
				{
					m_policyToggles[kvp.Key].gameObject.SetActive(true);
				}
			}
		}

		private void SetEntriesToLayers()
		{
			int i = 0;
			for (; i < m_currentPlan.PlanLayers.Count; i++)
			{
				if (i < m_layerToggles.Count)
					m_layerToggles[i].SetContent(m_currentPlan.PlanLayers[i].BaseLayer.ShortName, LayerManager.Instance.GetSubcategoryIcon(m_currentPlan.PlanLayers[i].BaseLayer.SubCategory));
				else
					CreateLayerEntry(m_currentPlan.PlanLayers[i]);
			}
			for (; i < m_layerToggles.Count; i++)
			{
				m_layerToggles[i].gameObject.SetActive(false);
			}
			m_changeLayersToggle.transform.SetAsLastSibling();
		}

		private void CreateLayerEntry(PlanLayer layer)
		{
			AP_ContentToggle obj = Instantiate(m_layerPrefab, m_layerParent).GetComponent<AP_ContentToggle>();
			int layerIndex = m_layerToggles.Count;
			obj.Initialise(this, m_geometryTool, () => OnLayerContentToggled(layerIndex));
			obj.SetContent(layer.BaseLayer.ShortName, LayerManager.Instance.GetSubcategoryIcon(layer.BaseLayer.SubCategory));
			m_layerToggles.Add(obj);
		}

		void OnLayerContentToggled(int a_layerIndex)
		{
			//Ignore if we just set the planlayer to active
			if (m_ignoreLayerCallback)
				return;

			//Ignore callback from Main.Instance.StartEditingLayer
			m_ignoreLayerCallback = true;
			//TODO
			m_geometryTool.StartEditingLayer(m_currentPlan.PlanLayers[a_layerIndex]);
			PlanDetails.LayersTab.StartEditingLayer(m_currentPlan.PlanLayers[a_layerIndex]);
			m_ignoreLayerCallback = false;
		}

		public void SetViewMode(PlanManager.PlanViewState a_viewMode)
		{
			if (a_viewMode == PlanManager.PlanViewState.All)
			{
				m_viewAllToggle.isOn = true;
			}
			else if (a_viewMode == PlanManager.PlanViewState.Changes)
			{
				m_viewPlanToggle.isOn = true;
			}
			else if (a_viewMode == PlanManager.PlanViewState.Base)
			{
				m_viewBaseToggle.isOn = true;
			}
		}

		public bool MayOpenNewPopout(AP_ContentToggle a_newToggle)
		{
			bool result = true;
			if(m_selectedContentToggle != null)
				result = m_selectedContentToggle.TryClose();
			if (result)
				m_selectedContentToggle = a_newToggle;
			return result;
		}

		public void ClearSelectedContentToggle()
		{
			m_selectedContentToggle = null;
		}
	}
}
