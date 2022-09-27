using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace MSP2050.Scripts
{
	public class PlansList : MonoBehaviour
	{
		enum EPlanSorting { State, Team, Time }

		[SerializeField] Transform m_contentParent;
		[SerializeField] GameObject m_plansGroupPrefab;
		[SerializeField] GameObject m_planBarPrefab;
		[SerializeField] SearchBar m_searchbar;
		[SerializeField] TMP_Dropdown m_sortingDropdown;

		private Dictionary<Plan.PlanState, PlansGroupBar> m_planGroupsPerState;
		private Dictionary<Plan, PlanBar> m_planBarsPerPlan;
		EPlanSorting m_currentSorting = EPlanSorting.State;

		private bool m_needsSorting;

		void Awake()
		{
			m_planGroupsPerState = new Dictionary<Plan.PlanState, PlansGroupBar>();
			m_planBarsPerPlan = new Dictionary<Plan, PlanBar>();

			IssueManager.instance.SubscribeToIssueChangedEvent(OnIssueChanged);

			m_planGroupsPerState.Add(Plan.PlanState.DESIGN, CreatePlansGroup("Design", "A plan's content (layers and policies) can only be edited in the DESIGN state.\nPlans in DESIGN are not visible in other plans or to other teams."));
			m_planGroupsPerState.Add(Plan.PlanState.CONSULTATION, CreatePlansGroup("Consultation", "Plans in CONSULTATION are visible in other plans and to other teams.\nUse the CONSULTATION state for early drafts that will need to be discussed with other teams."));
			m_planGroupsPerState.Add(Plan.PlanState.APPROVAL, CreatePlansGroup("Awaiting Approval", "Plans in the APPROVAL state will automatically be set to APPROVED once all required teams have accepted.\nPlans that require approval cannot be manually set to APPROVED, they must go through APPROVAL."));
			m_planGroupsPerState.Add(Plan.PlanState.APPROVED, CreatePlansGroup("Approved", "Plans in the APPROVED state will be implemented when their implementation time is reached."));
			m_planGroupsPerState.Add(Plan.PlanState.IMPLEMENTED, CreatePlansGroup("Implemented", "IMPLEMENTED plans have had their proposed changes applied to the world."));
			m_planGroupsPerState.Add(Plan.PlanState.DELETED, CreatePlansGroup("Archived", "When a plan's implementation time is reached and it is not in APPROVED or it has issues, it will automatically be ARCHIVED.\nIf an ARCHIVED plan's implementation time has passed, it must be updated before it can be set back to another state."));

			List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
			foreach(EPlanSorting sorting in (EPlanSorting[])Enum.GetValues(typeof(EPlanSorting)))
			{
				options.Add(new TMP_Dropdown.OptionData(sorting.ToString()));
			}
			m_sortingDropdown.ClearOptions();
			m_sortingDropdown.AddOptions(options);
			m_sortingDropdown.onValueChanged.AddListener(OnSortingDropdownChange);

			m_searchbar.m_ontextChange = OnSearchBarValueChanged;
		}

		public void LateUpdate()
		{
			//Only sort after all the updates so we don't do it multiple times
			if (m_needsSorting)
			{
				Sort();
				m_needsSorting = false;
			}
		}

		public void OnDestroy()
		{
			IssueManager issueManager = IssueManager.instance;
			if (issueManager != null)
			{
				issueManager.UnsubscribeFromIssueChangedEvent(OnIssueChanged);
			}
		}

		private void OnIssueChanged(PlanLayer a_changedIssueLayer)
		{
			SetPlanIssues(a_changedIssueLayer.Plan);
		}

		public void AddPlanToList(Plan a_plan)
		{
			PlanBar planBar = Instantiate(m_planBarPrefab, GetparentForState(a_plan.State)).GetComponent<PlanBar>();
			m_planBarsPerPlan.Add(a_plan, planBar);

			planBar.Initialise(a_plan);
			planBar.MoveToGroup(m_planGroupsPerState[a_plan.State]);
			planBar.UpdateActionRequired();
			planBar.Filter(m_searchbar.Text);

			//Hide new plan if it shouldnt be visible
			if (!a_plan.ShouldBeVisibleInUI)
				planBar.gameObject.SetActive(false);

			RefreshPlanBarInteractablity(a_plan, planBar);
			SetLockIcon(a_plan, a_plan.IsLocked);
			SetPlanIssues(a_plan);
			m_needsSorting = true;
		}

		void OnSearchBarValueChanged(string a_newValue)
		{
			foreach (var kvp in m_planBarsPerPlan)
			{
				kvp.Value.Filter(a_newValue);
			}
			if (m_currentSorting == EPlanSorting.State)
			{
				foreach (var kvp in m_planGroupsPerState)
				{
					kvp.Value.CheckEmpty();
				}
			}
		}

		void OnSortingDropdownChange(int a_newValue)
		{
			if(m_currentSorting == EPlanSorting.State)
			{
				//Remove state bars, reparent plans
				foreach (var kvp in m_planGroupsPerState)
				{
					kvp.Value.gameObject.SetActive(false);
				}
				foreach (var kvp in m_planBarsPerPlan)
				{
					kvp.Value.MoveToParent(m_contentParent);
				}
			}
			m_currentSorting = (EPlanSorting)a_newValue;

			if(m_currentSorting == EPlanSorting.State)
			{
				//Reparent to state bars and enable bars
				foreach (var kvp in m_planBarsPerPlan)
				{
					kvp.Value.MoveToGroup(m_planGroupsPerState[kvp.Key.State]);
				}
				foreach (var kvp in m_planGroupsPerState)
				{
					kvp.Value.gameObject.SetActive(true);
				}
			}
			m_needsSorting = true;
		}

		Transform GetparentForState(Plan.PlanState a_state)
		{
			if (m_currentSorting == EPlanSorting.State)
				return m_planGroupsPerState[a_state].ContentParent;
			return m_contentParent;
		}

		public void Sort()
		{
			if (m_currentSorting == EPlanSorting.State)
			{
				int uiPlanIndex = 0;
				for (int planId = 0; planId < PlanManager.Instance.GetPlanCount(); ++planId)
				{
					Plan planInstance = PlanManager.Instance.GetPlanAtIndex(planId);
					if (m_planBarsPerPlan.TryGetValue(planInstance, out PlanBar planBar))
					{
						m_planBarsPerPlan[planInstance].transform.SetSiblingIndex(uiPlanIndex);
						++uiPlanIndex;
					}
				}
			}
			else if (m_currentSorting == EPlanSorting.Time)
			{
				//Plans manager list is already sorted by time, so just use that order
				int uiPlanIndex = 0;
				for (int i = 0; i < PlanManager.Instance.GetPlanCount(); i++)
				{
					Plan planInstance = PlanManager.Instance.GetPlanAtIndex(i);
					if (m_planBarsPerPlan.TryGetValue(planInstance, out PlanBar planBar))
					{
						m_planBarsPerPlan[planInstance].transform.SetSiblingIndex(uiPlanIndex);
						uiPlanIndex++;
					}
				}
			}
			else //Sort by country
			{
				List<Plan> plans = new List<Plan>(PlanManager.Instance.Plans);
				plans.Sort(ComparePlanByCountry);
				int uiPlanIndex = 0;
				for (int i = 0; i < plans.Count; i++)
				{
					if (m_planBarsPerPlan.TryGetValue(plans[i], out PlanBar planBar))
					{
						m_planBarsPerPlan[plans[i]].transform.SetSiblingIndex(uiPlanIndex);
						uiPlanIndex++;
					}
				}
			}
		}

		public void UpdatePlan(Plan plan, bool nameChanged, bool timeChanged, bool stateChanged)
		{
			if (m_planBarsPerPlan.TryGetValue(plan, out PlanBar planBar))
			{
				if (nameChanged || timeChanged || stateChanged)
				{
					planBar.UpdateInfo();
					m_needsSorting = true;
				}
				if (stateChanged)
				{
					//Update plan visibility 
					if (planBar.gameObject.activeSelf)
					{
						if (!plan.ShouldBeVisibleInUI)
							planBar.SetPlanVisibility(false);
					}
					else if (plan.ShouldBeVisibleInUI)
						planBar.SetPlanVisibility(true);

					//Reparents the planbar to the right group
					m_planBarsPerPlan[plan].MoveToGroup(m_planGroupsPerState[plan.State]);
				}

				planBar.UpdateActionRequired();
				SetPlanIssues(plan);
			}
		}

		public void SetPlanBarToggleState(Plan a_plan, bool a_state)
		{
			m_planBarsPerPlan[a_plan].SetPlanBarToggleValue(a_state);
		}

		private void SetPlanIssues(Plan a_plan)
		{
			if (m_planBarsPerPlan.TryGetValue(a_plan, out var planBar))
			{
				ERestrictionIssueType maximumSeverity = ERestrictionIssueType.None;
				if (a_plan.energyError)
					maximumSeverity = ERestrictionIssueType.Error;

				planBar.SetIssue(maximumSeverity);

				if (a_plan.Country == SessionManager.Instance.CurrentUserTeamID)
				{
					if (maximumSeverity <= ERestrictionIssueType.Error)
					{
						PlayerNotifications.AddPlanIssueNotification(a_plan);
					}
					else
					{
						PlayerNotifications.RemovePlanIssueNotification(a_plan);
					}
				}
			}
		}

		public void SetLockIcon(Plan a_plan, bool a_value)
		{
			if (m_planBarsPerPlan.TryGetValue(a_plan, out var planBar))
			{
				planBar.SetLockActive(a_value);
			}
		}

		public PlansGroupBar CreatePlansGroup(string a_title = "New Plans Group", string a_tooltip = "")
		{
			PlansGroupBar group = Instantiate(m_plansGroupPrefab, m_contentParent).GetComponent<PlansGroupBar>();
			group.SetContent(a_title, a_tooltip, null); //TODO: get state icon
			return group;
		}

		public void SetAllButtonInteractable(bool a_value)
		{
			foreach (var kvp in m_planBarsPerPlan)
			{
				kvp.Value.SetPlanBarToggleInteractability(a_value);
			}
		}

		private Plan GetPlanForPlanBar(PlanBar a_planBar)
		{
			Plan result = null;
			foreach (var kvp in m_planBarsPerPlan)
			{
				if (kvp.Value == a_planBar)
				{
					result = kvp.Key;
				}
			}
			if (result == null)
			{
				Debug.LogError("Could not find plan associated with plan bar.");
			}
			return result;
		}

		public void RefreshPlanBarInteractablityForAllPlans()
		{
			foreach (var kvp in m_planBarsPerPlan)
			{
				RefreshPlanBarInteractablity(kvp.Key, kvp.Value);
			}
		}

		private void RefreshPlanBarInteractablity(Plan a_planRepresenting, PlanBar a_planBar)
		{
			a_planBar.SetPlanBarToggleInteractability(!Main.InEditMode && !Main.Instance.EditingPlanDetailsContent && !Main.Instance.PreventPlanAndTabChange);
		}

		int ComparePlanByCountry(Plan a_plan1, Plan a_plan2)
		{
			return a_plan1.Country.CompareTo(a_plan2.Country);
		}
	}
}
