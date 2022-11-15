using ColourPalette;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MSP2050.Scripts
{
	public class ActivePlanWindow : MonoBehaviour
	{
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
		public AP_TimeSelect m_timeSelect;

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
		public AP_GeometryTool m_geometryTool;
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
		private enum ECreationStage { None, Setup, Normal }
		private ECreationStage m_creationStage;

		//Properties
		public Plan CurrentPlan => m_currentPlan;
		public bool Editing => m_editing;
		//public bool CreatingNew => m_creatingNew;

		//Editing backup
		private PlanBackup m_planBackup;
		private int m_delayedPolicyEffects;

		void Awake()
		{
			m_changeLayersToggle.Initialise(this, m_layerSelect);
			m_changePoliciesToggle.Initialise(this, m_policySelect);
			m_communicationToggle.Initialise(this, m_communicationContent);
			m_approvalToggle.Initialise(this, m_approvalContent);
			m_issuesToggle.Initialise(this, m_issuesContent);
			m_planDate.Initialise(this, m_timeSelect);


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
			m_startEditingButton.onClick.AddListener(OnEditButtonPressed);
			m_acceptEditButton.onClick.AddListener(OnAcceptButton);
			m_cancelEditButton.onClick.AddListener(OnCancelButton);

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
			if(m_creationStage != ECreationStage.None)
			{ 
				//TODO
			}
			else if(m_editing)
			{
				if (m_cancelChangesConfirmationWindow == null || !m_cancelChangesConfirmationWindow.isActiveAndEnabled)
				{
					m_cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "All changes made to the plan will be lost. Are you sure you want to cancel?", null, () => ForceCancel(false));
				}
			}
		}

		public void ForceCancel(bool a_closeWindow)
		{
			if (m_selectedContentToggle != null)
			{
				m_selectedContentToggle.ForceClose(false);
			}

			m_planBackup.ResetPlanToBackup(m_currentPlan);
			PolicyManager.Instance.RestoreBackupForPlan(m_currentPlan);
			Main.Instance.fsm.ClearUndoRedo();
			LayerManager.Instance.UpdateVisibleLayersToPlan(m_currentPlan);
			m_currentPlan.AttemptUnlock();

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

			InterfaceCanvas.ShowNetworkingBlocker();
			CalculateEffectsOfEditing();
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
				CameraManager.Instance.ZoomToBounds(invalid.BoundingBox);
				DialogBoxManager.instance.NotificationWindow("Invalid geometry", "The plan contains invalid geometry and cannot be accepted until these have been fixed.", null);
				InterfaceCanvas.HideNetworkingBlocker();
				return;
			}

			m_delayedPolicyEffects = PolicyManager.Instance.CalculateEffectsOfEditing(m_currentPlan);
			if(m_delayedPolicyEffects == 0)
				SubmitChanges();
		}

		private void SubmitChanges()
		{
			BatchRequest batch = new BatchRequest(true);

			if (m_currentPlan.ID == -1)
				m_currentPlan.SendPlanCreation(batch);

			//Calculate and submit the countries this plan requires approval from
			m_currentPlan.CalculateRequiredApproval();
			m_currentPlan.SubmitRequiredApproval(batch);

			//Check issues again, to ensure that changes in other plans while editing this plan get detected as well.
			ConstraintManager.Instance.CheckConstraints(m_currentPlan, out bool hasUnavailableTypes);

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

			if(m_currentPlan.ID != -1)
				m_currentPlan.AttemptUnlock(batch);
			batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
		}

		void HandleChangesSubmissionSuccess(BatchRequest batch)
		{
			InterfaceCanvas.HideNetworkingBlocker();
			ExitEditMode();
		}

		void HandleChangesSubmissionFailure(BatchRequest batch)
		{
			InterfaceCanvas.HideNetworkingBlocker();
			DialogBoxManager.instance.NotificationWindow("Submitting data failed", "There was an error when submitting the plan's changes to the server. Please try again or see the error log for more information.", null);
		}

		public void OnEditButtonPressed()
		{
			Main.Instance.PreventPlanAndTabChange = true;
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();

			m_currentPlan.AttemptLock(
				(plan) =>
				{
					Main.Instance.PreventPlanAndTabChange = false;
					EnterEditMode();
				},
				(plan) => {
					Main.Instance.PreventPlanAndTabChange = false;
					InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
				});
		}

		public void SetToPlan(Plan plan)
		{
			if(plan == null)
			{
				//TODO: open in create mode
				m_editing = true;
				m_creationStage = ECreationStage.Setup;
				m_currentPlan = new Plan();
			}
			else
			{
				m_currentPlan = plan;
				m_editing = false;
				m_creationStage = ECreationStage.None;
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
			m_layerSection.SetActive(m_creationStage == ECreationStage.None || m_creationStage == ECreationStage.Normal);
			m_policySection.SetActive(m_creationStage == ECreationStage.None || m_creationStage == ECreationStage.Normal);
			m_communicationSection.SetActive(m_creationStage == ECreationStage.None);
			m_viewModeSection.SetActive(!m_editing);
			m_changeLayersToggle.gameObject.SetActive(m_editing);
			m_changePoliciesToggle.gameObject.SetActive(m_editing);
			//m_planDate.gameObject.SetActive(m_creationStage == ECreationStage.None || m_creationStage == ECreationStage.Normal);
			m_planState.gameObject.SetActive(!m_editing);
			m_issuesToggle.gameObject.SetActive(m_creationStage == ECreationStage.None || m_creationStage == ECreationStage.Normal);
		}

		void EnterEditMode()
		{
			m_editing = true;

			if (!m_viewAllToggle.isOn)
				m_viewAllToggle.isOn = true;

			m_planBackup = new PlanBackup(m_currentPlan);
			PolicyManager.Instance.StartEditingPlan(m_currentPlan);
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();

			UpdateSectionActivity();
		}

		void ExitEditMode()
		{
			m_editing = false;

			m_planBackup = null;
			PolicyManager.Instance.StopEditingPlan(m_currentPlan);

			InterfaceCanvas.Instance.StopEditing();//TODO: remove once toolbar removed
			Main.Instance.fsm.ClearUndoRedo();
			Main.Instance.fsm.StopEditing();

			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
			LayerManager.Instance.ClearNonReferenceLayers();
			PlanManager.Instance.ShowPlan(m_currentPlan); //Also refreshed our content & activity
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
			//TODO: implementation time text before its set during creation

			//State
			m_planState.SetContent($"Plan state: {m_currentPlan.State.GetDisplayName()}", VisualizationUtil.Instance.VisualizationSettings.GetplanStateSprite(m_currentPlan.State));

			//Messages
			m_communicationToggle.SetContent($"See {m_currentPlan.PlanMessages.Count} messages");

			//Approval
			if (m_currentPlan.State == Plan.PlanState.APPROVAL)
				m_approvalToggle.SetContent($"Approval required from {m_currentPlan.countryApproval.Count} teams");
			else
				m_approvalToggle.gameObject.SetActive(false);

			//Issues
			int issueCount = m_currentPlan.GetMaximumIssueSeverityAndCount(out var severity);
			if(issueCount == 0)
			{
				m_issuesToggle.SetContent("No issues", Color.clear);
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
