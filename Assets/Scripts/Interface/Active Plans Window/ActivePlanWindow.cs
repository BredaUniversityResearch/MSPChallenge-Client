using ColourPalette;
using System.Collections.Generic;
using System.Reactive.Joins;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActivePlanWindow : MonoBehaviour
	{
		[Header("General")]
		[SerializeField] GenericWindow m_window;
		//[SerializeField] ToggleGroup m_contentToggleGroup;
		[SerializeField] Transform m_popoutParent;
		[SerializeField] GameObject m_popoutLip;

		[Header("Buttons")]
		[SerializeField] GameObject m_buttonContainer;
		[SerializeField] Button m_startEditingButton;
		[SerializeField] Button m_acceptEditButton;
		[SerializeField] Button m_cancelEditButton;
		[SerializeField] Button m_zoomToPlanButton;
		[SerializeField] AddTooltip m_acceptButtonTooltip;

		[Header("Plan info")]
		[SerializeField] Image m_countryIndicator;
		[SerializeField] CustomInputField m_planName;
		[SerializeField] CustomInputField m_planDescription;
		[SerializeField] GameObject m_planDescriptionContainer;
		[SerializeField] AP_ContentToggle m_planDateToggle;
		[SerializeField] AP_ContentToggle m_planStateToggle;
		public AP_TimeSelect m_timeSelect;
		[SerializeField] AP_StateSelect m_stateSelect;

		[Header("Communication")]
		[SerializeField] GameObject m_communicationSection;
		[SerializeField] AP_ContentToggle m_communicationToggle;
		public AP_ContentToggle m_approvalToggle;
		public AP_ContentToggle m_issuesToggle;
		[SerializeField] AP_Communication m_communicationContent;
		[SerializeField] AP_Approval m_approvalContent;
		[SerializeField] AP_IssueList m_issuesContent;
		[SerializeField] ColourAsset m_infoIssueColour;
		[SerializeField] ColourAsset m_warningIssueColour;
		[SerializeField] ColourAsset m_errorIssueColour;

		[Header("Layers")]
		[SerializeField] GameObject m_layerSection;
		[SerializeField] Transform m_layerParent;
		[SerializeField] GameObject m_layerPrefab;
		[SerializeField] AP_ContentToggle m_changeLayersToggle;
		[SerializeField] GameObject m_changeLayersToggleContainer;
		public AP_GeometryTool m_geometryTool;
		[SerializeField] AP_LayerSelect m_layerSelect;
		[SerializeField] GameObject m_noLayersEntry;

		[Header("Policies")]
		[SerializeField] GameObject m_policySection;
		[SerializeField] Transform m_policyParent;
		[SerializeField] GameObject m_policyPrefab;
		[SerializeField] AP_ContentToggle m_changePoliciesToggle;
		[SerializeField] GameObject m_changePoliciesToggleContainer;
		[SerializeField] AP_PolicySelect m_policySelect;
		[SerializeField] GameObject m_noPoliciesEntry;

		private List<AP_ContentToggle> m_layerToggles = new List<AP_ContentToggle>();
		private Dictionary<string, AP_ContentToggle> m_policyToggles = new Dictionary<string, AP_ContentToggle>(); //popouts can be reached through toggles
		private AP_ContentToggle m_selectedContentToggle;

		private bool m_ignoreContentCallback;

		//General
		private DialogBox m_cancelChangesConfirmationWindow = null;
		private Plan m_currentPlan;
		public enum EInteractionMode { View, EditExisting, SetupNew, EditNew, RestoreArchived }
		private EInteractionMode m_interactionMode;
		private bool m_initialised;

		//Editing backup
		private PlanBackup m_planBackup;
		private int m_delayedPolicyEffects;

		//Properties
		public Plan CurrentPlan => m_currentPlan;
		public bool Editing => m_interactionMode != EInteractionMode.View;
		public EInteractionMode InteractionMode => m_interactionMode;
		public PlanBackup PlanBackup => m_planBackup;

		void Initialise()
		{
			m_initialised = true;

			m_changeLayersToggle.Initialise(this, m_layerSelect);
			m_changePoliciesToggle.Initialise(this, m_policySelect);
			m_communicationToggle.Initialise(this, m_communicationContent);
			m_approvalToggle.Initialise(this, m_approvalContent);
			m_issuesToggle.Initialise(this, m_issuesContent);
			m_planDateToggle.Initialise(this, m_timeSelect);
			m_planStateToggle.Initialise(this, m_stateSelect);

			if (m_zoomToPlanButton != null)
			{
				m_zoomToPlanButton.onClick.AddListener(() =>
				{
					m_currentPlan.ZoomToPlan();
				});
			}

			m_window.OnAttemptHideWindow = OnAttemptHideWindow;
			m_startEditingButton.onClick.AddListener(OnEditButtonPressed);
			m_acceptEditButton.onClick.AddListener(OnAcceptButton);
			m_cancelEditButton.onClick.AddListener(OnCancelButton);
			m_planName.onValueChanged.AddListener((s) =>
			{
				if (!m_ignoreContentCallback)
				{
					m_currentPlan.Name = s;
					RefreshSectionActivity();
				}
			});
			m_planDescription.onValueChanged.AddListener((s) =>
			{
				if (!m_ignoreContentCallback)
				{
					m_currentPlan.Description = s;
				}
			});

			//create policy popouts and toggles
			foreach (var kvp in PolicyManager.Instance.PolicyLogic)
			{
				AP_PopoutWindow popout = Instantiate(kvp.Value.m_definition.m_activePlanPrefab, m_popoutParent).GetComponent<AP_PopoutWindow>();
				popout.gameObject.SetActive(false);

				AP_ContentToggle toggle = Instantiate(m_policyPrefab, m_policyParent).GetComponent<AP_ContentToggle>();
				toggle.Initialise(this, popout);
				toggle.SetContent(kvp.Value.m_definition.m_displayName, kvp.Value.m_definition.m_activePlanIcon);
				m_policyToggles.Add(kvp.Key, toggle);
			}
		}

		private bool OnAttemptHideWindow()
		{
			if (m_interactionMode != EInteractionMode.View && m_interactionMode != EInteractionMode.RestoreArchived)
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
			if (m_cancelChangesConfirmationWindow == null || !m_cancelChangesConfirmationWindow.isActiveAndEnabled)
			{
				if (m_interactionMode == EInteractionMode.EditExisting)
					m_cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "All changes made to the plan will be lost. Are you sure you want to cancel?", null, () => ForceCancel(false));
				else
					m_cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "The new plan you are currently editing will be deleted. Are you sure you want to cancel?", null, () => ForceCancel(true));
			}
		}

		public void ForceCancel(bool a_closeWindow)
		{
			if (m_selectedContentToggle != null)
			{
				m_selectedContentToggle.ForceClose(false);
			}

			if (m_interactionMode == EInteractionMode.EditExisting)
			{
				m_planBackup.ResetPlanToBackup(m_currentPlan);
				PolicyManager.Instance.RestoreBackupForPlan(m_currentPlan);
				Main.Instance.fsm.ClearUndoRedo();
				LayerManager.Instance.UpdateVisibleLayersToPlan(m_currentPlan);
				IssueManager.Instance.SetIssueInstancesToPlan(m_currentPlan);
				RefreshContent();
				m_currentPlan.AttemptUnlock();
			}
			else if (m_interactionMode == EInteractionMode.EditNew)
			{
				PlanManager.Instance.RemovePlan(m_currentPlan);
				foreach (PlanLayer planLayer in m_currentPlan.PlanLayers)
				{
					planLayer.BaseLayer.RemovePlanLayer(planLayer);
					planLayer.RemoveGameObjects();
				}
			}
			else if (m_interactionMode == EInteractionMode.RestoreArchived)
			{
				m_currentPlan.StartTime = m_planBackup.m_startTime;
				m_currentPlan.Name = m_planBackup.m_name;
				m_currentPlan.State = Plan.PlanState.DELETED;
				IssueManager.Instance.SetIssueInstancesToPlan(m_currentPlan);
				RefreshContent();
				m_currentPlan.AttemptUnlock();
			}
			if (m_interactionMode != EInteractionMode.View)
				ExitEditMode(!a_closeWindow);

			if (a_closeWindow)
			{
				m_currentPlan = null;
				PlanManager.Instance.HideCurrentPlan();
				gameObject.SetActive(false);
			}
		}

		void OnAcceptButton()
		{
			if (m_selectedContentToggle != null && !m_selectedContentToggle.TryClose())
			{
				return;
			}

			if (m_interactionMode == EInteractionMode.SetupNew)
			{
				PlanManager.Instance.AddPlan(m_currentPlan);
				PlanManager.Instance.ForceSetPlanViewing(m_currentPlan);
				m_interactionMode = EInteractionMode.EditNew;
				RefreshContent();
			}
			else if (m_interactionMode == EInteractionMode.RestoreArchived)
			{
				InterfaceCanvas.ShowNetworkingBlocker();
				SubmitRestoration();
			}
			else
			{
				InterfaceCanvas.ShowNetworkingBlocker();
				CalculateEffectsOfEditing();
			}
		}

		/// <summary>
		/// Calculates the effect on energy grids and restrictions of the edits of the current plan.
		/// A plan should not be acceptable without its effect being calculated beforehand.
		/// </summary>
		private void CalculateEffectsOfEditing()
		{
			//Aborts any geometry being created
			Main.Instance.fsm.AbortCurrentState();

			//Check invalid geometry
			SubEntity invalid = m_currentPlan.CheckForInvalidGeometry();
			if (invalid != null)
			{
				CameraManager.Instance.ZoomToBounds(invalid.m_boundingBox);
				DialogBoxManager.instance.NotificationWindow("Invalid geometry", "The plan contains invalid geometry and cannot be accepted until these have been fixed.", null);
				InterfaceCanvas.HideNetworkingBlocker();
				return;
			}

			m_delayedPolicyEffects = PolicyManager.Instance.CalculateEffectsOfEditing(m_currentPlan);
			if (m_delayedPolicyEffects == 0)
				SubmitChanges();
		}

		private void SubmitChanges()
		{
			BatchRequest batch = new BatchRequest();

			//Newly created plans are not locked
			if (m_interactionMode != EInteractionMode.EditExisting)
				m_currentPlan.SendPlanCreation(batch);

			//Calculate and submit the countries this plan requires approval from
			m_currentPlan.CalculateRequiredApproval();
			m_currentPlan.SubmitRequiredApproval(batch);

			//Check issues again, to ensure that changes in other plans while editing this plan get detected as well.
			ConstraintManager.Instance.CheckConstraints(m_currentPlan, out var unavailableTypeNames);

			//Submit all layer and geometry changes (including issues).
			//Automatically submits corresponding energy_output and connection for geom.
			//Will reset 'edited' on all changed geometry
			m_planBackup.SubmitChanges(m_currentPlan, batch);

			//Submit policy data after geometry has a batch id
			PolicyManager.Instance.SubmitChangesToPlan(m_currentPlan, batch);

			//Plan info
			m_currentPlan.Description = m_planDescription.text;
			m_currentPlan.Name = m_planName.text;
			m_currentPlan.SubmitDescription(batch);
			m_currentPlan.SubmitName(batch);
			m_currentPlan.SubmitPlanDate(batch);

			if (m_interactionMode == EInteractionMode.EditExisting)
				m_currentPlan.AttemptUnlock(batch);
			batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
		}

		void SubmitRestoration()
		{
			BatchRequest batch = new BatchRequest();
			m_currentPlan.Description = m_planDescription.text;
			m_currentPlan.Name = m_planName.text;
			m_currentPlan.SendMessage("Restored the plans status to: " + Plan.PlanState.DESIGN.GetDisplayName(), batch);
			m_currentPlan.SubmitState(Plan.PlanState.DESIGN, batch);
			m_currentPlan.SubmitDescription(batch);
			m_currentPlan.SubmitName(batch);
			m_currentPlan.SubmitPlanDate(batch);
			m_currentPlan.AttemptUnlock(batch);
			batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
		}

		void HandleChangesSubmissionSuccess(BatchRequest batch)
		{
			InterfaceCanvas.HideNetworkingBlocker();
			ExitEditMode(true);
		}

		void HandleChangesSubmissionFailure(BatchRequest batch)
		{
			InterfaceCanvas.HideNetworkingBlocker();
			DialogBoxManager.instance.NotificationWindow("Submitting data failed", "There was an error when submitting the plan's changes to the server. Please try again or see the error log for more information.", null);
		}

		public void OnEditButtonPressed()
		{
			Main.Instance.PreventPlanChange = true;
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();

			m_currentPlan.AttemptLock(
				(plan) =>
				{
					Main.Instance.PreventPlanChange = false;
					m_interactionMode = EInteractionMode.EditExisting;
					EnterEditMode();
				},
				(plan) => {
					Main.Instance.PreventPlanChange = false;
					InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
				});
		}

		public void SetToPlan(Plan plan)
		{
			if (!m_initialised)
				Initialise();

			if (m_selectedContentToggle != null)
			{
				m_selectedContentToggle.ForceClose(false);
			}

			gameObject.SetActive(true);
			if (plan == null)
			{
				//Open in create new mode
				m_interactionMode = EInteractionMode.SetupNew;
				m_currentPlan = new Plan();
				EnterEditMode();
			}
			else if (plan.State == Plan.PlanState.DELETED)
			{
				m_currentPlan = plan;
				m_interactionMode = EInteractionMode.RestoreArchived;
				EnterEditMode();
			}
			else
			{
				m_currentPlan = plan;
				m_interactionMode = EInteractionMode.View;
			}
			if (m_countryIndicator != null)
				m_countryIndicator.color = SessionManager.Instance.FindTeamByID(m_currentPlan.Country).color;
			RefreshContent();
			if (plan != null && plan.State == Plan.PlanState.APPROVAL)
			{
				m_approvalToggle.IsOn = true;
			}
		}

		public void RefreshSectionActivity()
		{
			//Buttons
			bool buttonsActive = Editing || (m_currentPlan.State == Plan.PlanState.DESIGN && (SessionManager.Instance.AreWeManager || m_currentPlan.Country == SessionManager.Instance.CurrentUserTeamID));
			m_buttonContainer.gameObject.SetActive(buttonsActive);
			m_startEditingButton.gameObject.SetActive(!Editing);
			m_cancelEditButton.gameObject.SetActive(Editing);
			m_acceptEditButton.gameObject.SetActive(Editing);
			if(string.IsNullOrEmpty(m_planName.text))
			{
				m_acceptEditButton.interactable = false;
				m_acceptButtonTooltip.enabled = true;
				m_acceptButtonTooltip.SetText("Set a name for the plan");
			}
			else if(m_currentPlan.StartTime < -1)
			{
				m_acceptEditButton.interactable = false;
				m_acceptButtonTooltip.enabled = true;
				m_acceptButtonTooltip.SetText("Set an implementation time for the plan");
			}
			else if(m_currentPlan.ConstructionStartTime < TimeManager.Instance.GetCurrentMonth() && TimeManager.Instance.GetCurrentMonth() > -1 && m_currentPlan.StartTime > -1)
			{
				m_acceptEditButton.interactable = false;
				m_acceptButtonTooltip.enabled = true;
				m_acceptButtonTooltip.SetText("The plan cannot be implemented by the designated time, select a later implementation time");
			}
			else
			{
				m_acceptEditButton.interactable = true;
				m_acceptButtonTooltip.enabled = false;
				m_acceptButtonTooltip.SetText("");
			}

			//Content
			m_planName.interactable = Editing;
			m_planDescription.interactable = Editing;
			m_planDescriptionContainer.gameObject.SetActive((!string.IsNullOrEmpty(m_currentPlan.Description) && m_currentPlan.Description != " ") || Editing);
			m_layerSection.SetActive(m_interactionMode == EInteractionMode.EditExisting || m_interactionMode == EInteractionMode.EditNew || m_interactionMode == EInteractionMode.View);
			m_policySection.SetActive(m_interactionMode == EInteractionMode.EditExisting || m_interactionMode == EInteractionMode.EditNew || m_interactionMode == EInteractionMode.View);
			m_communicationSection.gameObject.SetActive(m_interactionMode != EInteractionMode.SetupNew);
			m_communicationToggle.gameObject.SetActive(m_interactionMode != EInteractionMode.EditNew && m_interactionMode != EInteractionMode.SetupNew);
			m_issuesToggle.gameObject.SetActive(m_interactionMode != EInteractionMode.RestoreArchived);
			TimeBar.instance.SetGeometryViewModeVisible(!Editing);
			m_changeLayersToggleContainer.gameObject.SetActive(m_interactionMode == EInteractionMode.EditExisting || m_interactionMode == EInteractionMode.EditNew);
			m_changePoliciesToggleContainer.gameObject.SetActive(m_interactionMode == EInteractionMode.EditExisting || m_interactionMode == EInteractionMode.EditNew);
			m_planStateToggle.gameObject.SetActive(!Editing);
			m_planStateToggle.SetInteractable(m_currentPlan.Country == SessionManager.Instance.CurrentUserTeamID || SessionManager.Instance.AreWeManager);
			m_planDateToggle.SetInteractable(Editing);

			foreach(var toggle in m_layerToggles)
			{
				toggle.SetInteractable(Editing);
			}
		}

		void EnterEditMode()
		{
			if (m_selectedContentToggle != null)
			{
				m_selectedContentToggle.ForceClose(false);
			}
			TimeBar.instance.SetViewMode(PlanManager.PlanViewState.All);

			if(m_interactionMode == EInteractionMode.SetupNew)
				m_planBackup = new PlanBackup(null);
			else
				m_planBackup = new PlanBackup(m_currentPlan);
			PolicyManager.Instance.StartEditingPlan(m_currentPlan);
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();

			RefreshSectionActivity();
		}

		void ExitEditMode(bool a_windowStaysOpen)
		{
			m_interactionMode = EInteractionMode.View;

			m_planBackup = null;
			PolicyManager.Instance.StopEditingPlan(m_currentPlan);

			Main.Instance.fsm.ClearUndoRedo();
			Main.Instance.fsm.StopEditing();

			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
			LayerManager.Instance.ClearNonReferenceLayers();
			if(a_windowStaysOpen)
				PlanManager.Instance.ShowPlan(m_currentPlan); //Also refreshed our content & activity
		}

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		public void OnTimeChange()
		{
			if(m_interactionMode != EInteractionMode.SetupNew) //In setup this will be done on accept
				PlanManager.Instance.ForceSetPlanViewing(m_currentPlan);
			RefreshContent();
			PolicyManager.Instance.EditedPlanTimeChanged(m_currentPlan);
		}

		public void OnPreviousPlanChangedInfluence()
		{
			//TODO: set active entities again
			PolicyManager.Instance.PreviousPlanChangedInfluence(m_currentPlan);
		}

		public void RefreshContent()
		{
			//Info
			m_ignoreContentCallback = true;
			m_planName.text = m_currentPlan.Name;
			m_planDescription.text = m_currentPlan.Description;
			m_ignoreContentCallback = false;

			//Time
			if (m_interactionMode == EInteractionMode.SetupNew && m_currentPlan.StartTime < -50)
			{
				m_planDateToggle.SetContent($"Set implementation time");
			}
			else
			{
				int maxConstructionTime = 0;
				foreach (PlanLayer layer in m_currentPlan.PlanLayers)
				{
					if (layer.BaseLayer.AssemblyTime > maxConstructionTime)
						maxConstructionTime = layer.BaseLayer.AssemblyTime;
				}
				if (maxConstructionTime == 0)
					m_planDateToggle.SetContent($"{Util.MonthToText(m_currentPlan.StartTime)}. No construction time required.");
				else if (maxConstructionTime == 1)
					m_planDateToggle.SetContent($"{Util.MonthToText(m_currentPlan.StartTime)}, after 1 month construction.");
				else
					m_planDateToggle.SetContent($"{Util.MonthToText(m_currentPlan.StartTime)}, after {maxConstructionTime} months construction.");
			}

			//State
			m_planStateToggle.SetContent($"Plan state: {m_currentPlan.State.GetDisplayName()}", VisualizationUtil.Instance.VisualizationSettings.GetplanStateSprite(m_currentPlan.State));

			//Messages
			m_communicationToggle.SetContent($"See {m_currentPlan.PlanMessages.Count} messages");

			//Approval
			if (m_currentPlan.State == Plan.PlanState.APPROVAL)
            {
				m_approvalToggle.SetContent($"Approval required from {m_currentPlan.countryApproval.Count} teams");
            }
			else
				m_approvalToggle.gameObject.SetActive(false);


			//Content
			RefreshIssueText();
			SetEntriesToPolicies();
			SetEntriesToLayers();
			RefreshSectionActivity();
			if (m_approvalContent.IsOpen)
			{
				m_approvalContent.RefreshContent(m_currentPlan);
			}
		}

		public void RefreshIssueText()
		{
			int issueCount = m_currentPlan.GetMaximumIssueSeverityAndCount(out var severity);
			if (issueCount == 0)
			{
				m_issuesToggle.SetContent("No issues", m_infoIssueColour.GetColour());
			}
			else
			{
				switch (severity)
				{
					case ERestrictionIssueType.Info:
						m_issuesToggle.SetContent($"Plan has {issueCount} issues", m_infoIssueColour.GetColour());
						break;
					case ERestrictionIssueType.Warning:
						m_issuesToggle.SetContent($"Plan has {issueCount} issues", m_warningIssueColour.GetColour());
						break;
					default:
						m_issuesToggle.SetContent($"Plan has {issueCount} issues", m_errorIssueColour.GetColour());
						break;
				}
			}
		}

		private void SetEntriesToPolicies()
		{
			foreach (var kvp in m_policyToggles)
			{
				kvp.Value.gameObject.SetActive(false);
			}
			if (m_currentPlan.Policies != null)
			{
				bool onePlusPolicyActive = false;
				foreach (var kvp in m_currentPlan.Policies)
				{
					if (kvp.Value.logic.ShowPolicyToggled(kvp.Value))
					{
						m_policyToggles[kvp.Key].gameObject.SetActive(true);
						onePlusPolicyActive = true;
					}
				}
				m_noPoliciesEntry.SetActive(!Editing && !onePlusPolicyActive);
			}
			else
			{ 
				m_noPoliciesEntry.SetActive(!Editing);
			}
		}

		private void SetEntriesToLayers()
		{
			int i = 0;
			for (; i < m_currentPlan.PlanLayers.Count; i++)
			{
				if (i < m_layerToggles.Count)
					m_layerToggles[i].SetContent(m_currentPlan.PlanLayers[i].BaseLayer.ShortName, LayerManager.Instance.GetSubcategoryIcon(m_currentPlan.PlanLayers[i].BaseLayer.m_subCategory));
				else
					CreateLayerEntry(m_currentPlan.PlanLayers[i]);
			}
			for (; i < m_layerToggles.Count; i++)
			{
				m_layerToggles[i].gameObject.SetActive(false);
			}
			m_noLayersEntry.SetActive(!Editing && m_currentPlan.PlanLayers.Count == 0);
		}

		private void CreateLayerEntry(PlanLayer layer)
		{
			AP_ContentToggle obj = Instantiate(m_layerPrefab, m_layerParent).GetComponent<AP_ContentToggle>();
			int layerIndex = m_layerToggles.Count;
			obj.Initialise(this, m_geometryTool, () => OnLayerContentToggled(layerIndex));
			obj.SetContent(layer.BaseLayer.ShortName, LayerManager.Instance.GetSubcategoryIcon(layer.BaseLayer.m_subCategory));
			obj.GetComponent<RegisterInteraction>().SetTags(new string[] { "AP_Layer" });
			m_layerToggles.Add(obj);
		}

		void OnLayerContentToggled(int a_layerIndex)
		{
			//Ignore if we just set the planlayer to active
			if (m_ignoreContentCallback)
				return;

			//Ignore callback from Main.Instance.StartEditingLayer
			m_ignoreContentCallback = true;
			m_geometryTool.StartEditingLayer(m_currentPlan.PlanLayers[a_layerIndex]);
			m_ignoreContentCallback = false;
		}

		public bool MayOpenNewPopout(AP_ContentToggle a_newToggle)
		{
			bool result = true;
			if(m_selectedContentToggle != null)
				result = m_selectedContentToggle.TryClose();
			if (result)
			{
				m_selectedContentToggle = a_newToggle;
				m_popoutLip.SetActive(true);
			}
			return result;
		}

		public void ClearSelectedContentToggle()
		{
			m_selectedContentToggle = null;
			m_popoutLip.SetActive(false);
		}

		public void OnDelayedPolicyEffectCalculated()
		{
			m_delayedPolicyEffects--;
			if(m_delayedPolicyEffects == 0)
			{
				SubmitChanges();
			}
		}

		public AbstractLayer CurrentlyEditingBaseLayer
		{
			get
			{
				if (!Editing || !m_geometryTool.IsOpen)
					return null;
				return m_geometryTool.CurrentlyEditingLayer.BaseLayer;
			}
		}
	}
}
