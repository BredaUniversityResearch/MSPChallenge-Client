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
		private List<AP_ContentToggle> m_policyToggles = new List<AP_ContentToggle>();
		private Dictionary<string, AP_PopoutWindow> m_policyPopouts = new Dictionary<string, AP_PopoutWindow>();
		private AP_ContentToggle m_selectedContentToggle;

		private Dictionary<PlanLayer, ActivePlanLayer> layers;
		private bool m_ignoreLayerCallback;

		//General
		private Plan m_currentPlan;
		private bool m_editing;
		private DialogBox m_cancelChangesConfirmationWindow = null;
		public Plan CurrentPlan => m_currentPlan;
		public bool Editing => m_editing;

		void Awake()
		{
			m_changeLayersToggle.Initialise(this, m_layerSelect);
			m_changePoliciesToggle.Initialise(this, m_policySelect);
			m_communication.Initialise(this, m_communicationContent);
			m_approval.Initialise(this, m_approvalContent);
			m_issues.Initialise(this, m_issuesContent);

			m_startEditingButton.onClick.AddListener(TryEnterEditMode);

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
		}

		private bool OnAttemptHideWindow()
		{
			if (Main.InEditMode)
			{
				if (m_cancelChangesConfirmationWindow == null || !m_cancelChangesConfirmationWindow.isActiveAndEnabled)
				{
					UnityEngine.Events.UnityAction lb = () => { };
					UnityEngine.Events.UnityAction rb = () =>
					{
						if (Main.InEditMode)
							PlanDetails.LayersTab.ForceCancelChanges();
						else
							PlanDetails.instance.CancelEditingContent();

						PlanManager.Instance.HideCurrentPlan();
						gameObject.SetActive(false);
					};
					m_cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "All changes made to the plan will be lost. Are you sure you want to cancel?", lb, rb);
				}

				return false;
			}
			else
			{
				PlanManager.Instance.HideCurrentPlan();
				return true;
			}
		}


		public void SetToPlan(Plan plan)
		{
			gameObject.SetActive(true);
			m_currentPlan = plan;
			m_countryIndicator.color = SessionManager.Instance.FindTeamByID(plan.Country).color;
			UpdateEditButtonActivity();
		}

		public void UpdateEditButtonActivity()
		{
			m_editButtonParent.SetActive(!Main.InEditMode	 && m_currentPlan != null
															 && m_currentPlan.State == Plan.PlanState.DESIGN
															 && (SessionManager.Instance.AreWeManager || m_currentPlan.Country == SessionManager.Instance.CurrentUserTeamID));
		}

		public void TryEnterEditMode()
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

		void EnterEditMode()
		{
			m_editing = true;
			//TODO

			if (!m_viewAllToggle.isOn)
				m_viewAllToggle.isOn = true;
			m_viewModeSection.SetActive(false);
			m_cancelAcceptButtonParent.SetActive(true);
			m_editButtonParent.gameObject.SetActive(false);

			m_changeLayersToggle.gameObject.SetActive(true);
			m_changePoliciesToggle.gameObject.SetActive(true);
		}

		void ExitEditMode()
		{
			m_editing = false;
			//TODO

			m_viewModeSection.SetActive(true);
			UpdateEditButtonActivity();
			m_cancelAcceptButtonParent.SetActive(false);

			m_changeLayersToggle.gameObject.SetActive(false);
			m_changePoliciesToggle.gameObject.SetActive(false);
		}

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		public void StartEditingLayer(PlanLayer layer)
		{
			//Handle visual selection
			if (!m_ignoreLayerCallback)
			{
				m_ignoreLayerCallback = true;
				layers[layer].toggle.isOn = true;
				m_ignoreLayerCallback = false;
			}

			//TODO: set geometry tool active & content
		}

		private void SetEntriesToPolicies()
		{
			//TODO
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
			obj.Initialise(this, m_geometryTool);
			obj.SetContent(layer.BaseLayer.ShortName, LayerManager.Instance.GetSubcategoryIcon(layer.BaseLayer.SubCategory));
			m_layerToggles.Add(obj);
		}

		void OnLayerContentToggled(bool a_value, int a_layerIndex)
		{
			//TODO
			//Ignore if we just set the planlayer to active
			if (m_ignoreLayerCallback)
				return;

			//Ignore callback from Main.Instance.StartEditingLayer
			m_ignoreLayerCallback = true;
			PlanDetails.LayersTab.StartEditingLayer(m_currentPlan.PlanLayers[a_layerIndex]);
			m_ignoreLayerCallback = false;
		}

		private void CreatePolicyEntry(PlanLayer layer)
		{
			AP_ContentToggle obj = Instantiate(m_policyPrefab, m_policyParent).GetComponent<AP_ContentToggle>();
			int policyIndex = m_policyToggles.Count;
			obj.Initialise((b) => OnpolicyContentToggled(b, policyIndex), m_contentToggleGroup);
			obj.SetContent(layer.BaseLayer.ShortName, LayerManager.Instance.GetSubcategoryIcon(layer.BaseLayer.SubCategory));
			m_policyToggles.Add(obj);
		}

		void OnpolicyContentToggled(bool a_value, int a_policyIndex)
		{
			//TODO
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

		public bool MayOpenNewPopout()
		{
			if(m_selectedContentToggle != null)
				return m_selectedContentToggle.TryClose();
			return true;
		}
	}
}
