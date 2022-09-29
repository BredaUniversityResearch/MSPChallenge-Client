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
		[SerializeField] Image countryBall;
		[SerializeField] CustomInputField m_planName;
		[SerializeField] CustomInputField m_planDescription;
		[SerializeField] AP_ContentToggle m_planDate;
		[SerializeField] AP_ContentToggle m_planState;

		[Header("View mode")]
		[SerializeField] GameObject m_viewModeSection;
		[SerializeField] Toggle m_viewAllToggle;
		[SerializeField] Toggle m_viewPlanToggle;
		[SerializeField] Toggle m_viewBaseToggle;

		[Header("Layers")]
		[SerializeField] GameObject m_layerSection;
		[SerializeField] Transform m_layerParent;
		[SerializeField] GameObject m_layerPrefab;
		[SerializeField] Button m_changeLayersButton;

		List<AP_ContentToggle> m_layerToggles;

		[Header("Policies")]
		[SerializeField] GameObject m_policySection;
		[SerializeField] Transform m_policyParent;
		[SerializeField] GameObject m_policyPrefab;
		[SerializeField] Button m_changePoliciesButton;

		List<AP_ContentToggle> m_policyToggles;

		private Dictionary<PlanLayer, ActivePlanLayer> layers;
		private bool m_ignoreLayerCallback;

		//General
		private Plan m_selectedPlan;
		private DialogBox m_cancelChangesConfirmationWindow = null;

		void Awake()
		{
			m_startEditingButton.onClick.AddListener(() =>
			{
				if (m_selectedPlan != null)
				{
					PlanDetails.SelectPlan(m_selectedPlan);
					PlanDetails.instance.TabSelect(PlanDetails.EPlanDetailsTab.Layers);
					PlanDetails.instance.editTabContentButton.onClick.Invoke();
				}
			});

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
				m_selectedPlan.ZoomToPlan();
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
			m_selectedPlan = plan;
			countryBall.color = SessionManager.Instance.FindTeamByID(plan.Country).color;
			UpdateNameAndDate();
			UpdateEditButtonActivity();
		}

		public void UpdateNameAndDate()
		{
			m_planDate.text = string.Format("{0} ({1})", m_selectedPlan.Name, Util.MonthToText(m_selectedPlan.StartTime, true));

		}

		public void UpdateEditButtonActivity()
		{
			m_editButtonParent.SetActive(!Main.InEditMode && !Main.Instance.EditingPlanDetailsContent
																	 && m_selectedPlan != null
																	 && m_selectedPlan.State == Plan.PlanState.DESIGN
																	 && (SessionManager.Instance.AreWeManager || m_selectedPlan.Country == SessionManager.Instance.CurrentUserTeamID);
		}

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		public void CloseEditingUI()
		{
			m_viewModeSection.SetActive(true);
			UpdateEditButtonActivity();
			m_cancelAcceptButtonParent.SetActive(false);

			m_changeLayersButton.gameObject.SetActive(false);
			m_changePoliciesButton.gameObject.SetActive(false);
		}

		public void OpenEditingUI()
		{
			if (!m_viewAllToggle.isOn)
				m_viewAllToggle.isOn = true;
			m_viewModeSection.SetActive(false);
			m_cancelAcceptButtonParent.SetActive(true);
			m_editButtonParent.gameObject.SetActive(false);

			m_changeLayersButton.gameObject.SetActive(true);
			m_changePoliciesButton.gameObject.SetActive(true);
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
			for (; i < m_selectedPlan.PlanLayers.Count; i++)
			{
				if (i < m_layerToggles.Count)
					m_layerToggles[i].SetContent(m_selectedPlan.PlanLayers[i].BaseLayer.ShortName, LayerInterface.GetIconStatic(m_selectedPlan.PlanLayers[i].BaseLayer.SubCategory));
				else
					CreateLayerEntry(m_selectedPlan.PlanLayers[i]);
			}
			for (; i < m_layerToggles.Count; i++)
			{
				m_layerToggles[i].gameObject.SetActive(false);
			}
			m_changeLayersButton.transform.SetAsLastSibling();
		}

		private void CreateLayerEntry(PlanLayer layer)
		{
			AP_ContentToggle obj = Instantiate(m_layerPrefab, m_layerParent).GetComponent<AP_ContentToggle>();
			int layerIndex = m_layerToggles.Count;
			obj.Initialise((b) => OnLayerContentToggled(b, layerIndex), m_contentToggleGroup);
			obj.SetContent(layer.BaseLayer.ShortName, LayerInterface.GetIconStatic(layer.BaseLayer.SubCategory));
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
			PlanDetails.LayersTab.StartEditingLayer(planLayer);
			m_ignoreLayerCallback = false;
		}

		private void CreatePolicyEntry(PlanLayer layer)
		{
			AP_ContentToggle obj = Instantiate(m_policyPrefab, m_policyParent).GetComponent<AP_ContentToggle>();
			int policyIndex = m_policyToggles.Count;
			obj.Initialise((b) => OnpolicyContentToggled(b, policyIndex), m_contentToggleGroup);
			obj.SetContent(layer.BaseLayer.ShortName, LayerInterface.GetIconStatic(layer.BaseLayer.SubCategory));
			m_policyToggles.Add(obj);
		}

		void OnpolicyContentToggled(bool a_value, int a_policyIndex)
		{
			//TODO
		}
	}
}
