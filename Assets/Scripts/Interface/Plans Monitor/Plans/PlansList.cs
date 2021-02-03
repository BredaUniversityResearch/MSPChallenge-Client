using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Interface.Notifications;

public class PlansList : MonoBehaviour
{
	public Transform contentLocation;
	public List<PlansGroupBar> planGroups;
	public List<PlanBar> plans;
	public List<PlanLayerBar> planLayerBars;

	[Header("Prefabs")]
	public GameObject plansGroupPrefab;
	public GameObject planBarPrefab;
	public GameObject planLayerPrefab;

	private PlansGroupBar designStatePlans;
	private PlansGroupBar consultationStatePlans;
	private PlansGroupBar needApprovalStatePlans;
	private PlansGroupBar approvedStatePlans;
	private PlansGroupBar implementedStatePlans;
	private PlansGroupBar deletedPlans;

	private Dictionary<Plan.PlanState, PlansGroupBar> stateGroup;
	private Dictionary<Plan, PlanBar> planToPlanBar;
	private Dictionary<PlanLayer, PlanLayerBar> planLayerToPlanLayerBar;

	private bool needsSorting;

	void Awake()
	{
		planGroups = new List<PlansGroupBar>();
		plans = new List<PlanBar>();
		planLayerBars = new List<PlanLayerBar>();
		stateGroup = new Dictionary<Plan.PlanState, PlansGroupBar>();
		planToPlanBar = new Dictionary<Plan, PlanBar>();
		planLayerToPlanLayerBar = new Dictionary<PlanLayer, PlanLayerBar>();

		IssueManager.instance.SubscribeToIssueChangedEvent(OnIssueChanged);

		CreateGroups();
		AddPlanStates();
	}

	public void LateUpdate()
	{
		//Only sort after all the updates so we don't do it multiple times
		if (needsSorting)
		{
			SortByDate();
			needsSorting = false;
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

	private void OnIssueChanged(PlanLayer changedIssueLayer)
	{
		SetPlanIssues(changedIssueLayer.Plan);
	}

	private void CreateGroups()
	{
		designStatePlans = CreatePlansGroup("Design", "A plan's content (layers and policies) can only be edited in the DESIGN state.\nPlans in DESIGN are not visible in other plans or to other teams.");
		consultationStatePlans = CreatePlansGroup("Consultation", "Plans in CONSULTATION are visible in other plans and to other teams.\nUse the CONSULTATION state for early drafts that will need to be discussed with other teams.");
		needApprovalStatePlans = CreatePlansGroup("Awaiting Approval", "Plans in the APPROVAL state will automatically be set to APPROVED once all required teams have accepted.\nPlans that require approval cannot be manually set to APPROVED, they must go through APPROVAL.");
		approvedStatePlans = CreatePlansGroup("Approved", "Plans in the APPROVED state will be implemented when their implementation time is reached.");
		implementedStatePlans = CreatePlansGroup("Implemented", "IMPLEMENTED plans have had their proposed changes applied to the world.");
		deletedPlans = CreatePlansGroup("Archived", "When a plan's implementation time is reached and it is not in APPROVED or it has issues, it will automatically be ARCHIVED.\nIf an ARCHIVED plan's implementation time has passed, it must be updated before it can be set back to another state.");
	}

	private void AddPlanStates()
	{
		stateGroup.Add(Plan.PlanState.DESIGN, designStatePlans);
		stateGroup.Add(Plan.PlanState.CONSULTATION, consultationStatePlans);
		stateGroup.Add(Plan.PlanState.APPROVAL, needApprovalStatePlans);
		stateGroup.Add(Plan.PlanState.APPROVED, approvedStatePlans);
		stateGroup.Add(Plan.PlanState.IMPLEMENTED, implementedStatePlans);
		stateGroup.Add(Plan.PlanState.DELETED, deletedPlans);
	}

	public PlanBar AddPlanToList(Plan plan)
	{
		// Create the plan and put it in the correct group
		PlanBar planBar = CreatePlanBar(TeamManager.GetTeamByTeamID(plan.Country).color, Util.MonthToText(plan.StartTime, true), plan);
		stateGroup[plan.State].AddPlan(planBar);

		if (PlanDetails.GetSelectedPlan() != plan)
		{
			planBar.ToggleChangeIndicator(true);
		}		

		// show all layers within a plan
		foreach (PlanLayer layer in plan.PlanLayers)
			AddPlanLayer(plan, layer, planBar);

		planToPlanBar.Add(plan, planBar);

		SetLockIcon(plan, plan.IsLocked);
		needsSorting = true;

		SetPlanIssues(plan);
		planBar.UpdateActionRequired();

		//Hide new plan if it shouldnt be visible
		if (!plan.ShouldBeVisibleInUI)
			planBar.gameObject.SetActive(false);

		return planBar;
	}

	public void AddPlanLayer(Plan plan, PlanLayer addedLayer, PlanBar planBar = null)
	{
		// Show the layerbar, they always start off
		PlanLayerBar layerBar = CreatePlanLayer(TeamManager.GetTeamByTeamID(plan.Country).color, addedLayer, plan, addedLayer.BaseLayer.GetShortName());
		layerBar.SetIssue(ERestrictionIssueType.None);
		if (planBar == null)
			planToPlanBar[plan].AddLayer(layerBar);
		else
			planBar.AddLayer(layerBar);
		planLayerToPlanLayerBar.Add(addedLayer, layerBar);
	}


	public void RemovePlanLayer(Plan plan, PlanLayer removedLayer)
	{
		PlanLayerBar bar = planLayerToPlanLayerBar[removedLayer];
		GameObject.Destroy(bar.gameObject);
		planToPlanBar[removedLayer.Plan].RemoveLayer(bar);
		planLayerToPlanLayerBar.Remove(removedLayer);
	}

	public void SortByDate()
	{
		int uiPlanIndex = 0;
		for (int planId = 0; planId < PlanManager.GetPlanCount(); ++planId)
		{
			Plan planInstance = PlanManager.GetPlanAtIndex(planId);
			PlanBar planBar;
			if (planToPlanBar.TryGetValue(planInstance, out planBar))
			{
				planToPlanBar[planInstance].transform.SetSiblingIndex(uiPlanIndex);
				++uiPlanIndex;
			}
		}
	}

	public void UpdatePlan(Plan plan, bool nameChanged, bool timeChanged, bool stateChanged)
	{
		PlanBar planBar;
		if (!planToPlanBar.TryGetValue(plan, out planBar))
		{
			return;
		}

		if (nameChanged)
		{
			planBar.title.text = plan.Name;
		}
		if (timeChanged)
		{
			planBar.date.text = Util.MonthToText(plan.StartTime, true);
			needsSorting = true;
		}
		if (stateChanged)
		{
			//Update plan visibility 
			if (planBar.gameObject.activeSelf)
			{		
				if (!plan.ShouldBeVisibleInUI)
					planBar.gameObject.SetActive(false);
			}
			else if (plan.ShouldBeVisibleInUI)
				planBar.gameObject.SetActive(true);

            if (plan.State == Plan.PlanState.DELETED)
				planBar.SetViewEditButtonState(null); //Hides edit/view button
            else
				planBar.SetViewEditButtonState(false);//Sets edit/view button to view

			//Reparents the planbar to the right group
			stateGroup[plan.State].AddPlan(planToPlanBar[plan]);
			needsSorting = true;
		}
		
		planBar.UpdateActionRequired();
		SetPlanIssues(plan);
	}

	public void SetPlanUnseenChanges(Plan plan, bool unseenChanges)
	{
		if (planToPlanBar.ContainsKey(plan))
		{
			PlanBar planBar = planToPlanBar[plan];
			if (planBar != null)
				planBar.ToggleChangeIndicator(unseenChanges);
		}
	}

	public void SetViewPlanFrameState(Plan plan, bool state)
	{
		planToPlanBar[plan].SetViewFrameActivity(state);
	}

	public void SetPlanBarToggleState(Plan plan, bool state)
	{
		planToPlanBar[plan].SetPlanBarToggleValue(state);
	}

	private void SetPlanIssues(Plan plan)
	{
		PlanBar planBar;
		if (planToPlanBar.TryGetValue(plan, out planBar))
		{
			ERestrictionIssueType maximumSeverity = ERestrictionIssueType.None;
			// first set the planlayers icons
			if (plan.energyError)
				maximumSeverity = ERestrictionIssueType.Error;
			foreach (PlanLayer layer in plan.PlanLayers)
			{
				ERestrictionIssueType issueType = IssueManager.instance.GetMaximumSeverity(layer);
				planLayerToPlanLayerBar[layer].SetIssue(issueType);
				if (issueType < maximumSeverity)
				{
					maximumSeverity = issueType;
				}
			}

			planBar.SetIssue(maximumSeverity);

			if (plan.Country == TeamManager.CurrentUserTeamID)
			{
				if (maximumSeverity <= ERestrictionIssueType.Error)
				{
					PlayerNotifications.AddPlanIssueNotification(plan);
				}
				else
				{
					PlayerNotifications.RemovePlanIssueNotification(plan);
				}
			}
		}
	}

	public void SetLockIcon(Plan plan, bool value)
	{
		PlanBar planBar;
		if (planToPlanBar.TryGetValue(plan, out planBar))
		{
			planBar.lockIcon.gameObject.SetActive(value);
		}
	}

	public PlansGroupBar CreatePlansGroup(string title = "New Plans Group", string tooltip = "")
	{
		GameObject go = Instantiate(plansGroupPrefab);
		PlansGroupBar group = go.GetComponent<PlansGroupBar>();
		planGroups.Add(group);
		go.transform.SetParent(contentLocation, false);
		group.title.text = title;
		group.tooltip.text = tooltip;
		return group;
	}

	public void SetAllButtonInteractable(bool value)
	{
		foreach (PlanBar planBar in plans)
		{
			planBar.SetViewEditButtonInteractable(value);
		}
	}

	private Plan GetPlanForPlanBar(PlanBar planBar)
	{
		Plan result = null;
		foreach (var kvp in planToPlanBar)
		{
			if (kvp.Value == planBar)
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

	public PlanBar CreatePlanBar(Color col, string date, Plan planRepresenting)
	{
		GameObject go = Instantiate(planBarPrefab);
		PlanBar planBar = go.GetComponent<PlanBar>();
		plans.Add(planBar);

		planBar.Initialise(planRepresenting);
		planBar.title.text = planRepresenting.Name;
		planBar.countryIcon.color = col;
		planBar.date.text = date;

		RefreshPlanBarInteractablity(planRepresenting, planBar);

		ColorBlock block = planBar.foldButton.colors;
		block.highlightedColor = col;
		planBar.foldButton.colors = block;

		planBar.ToggleChangeIndicator(true);

		return planBar;
	}

	public PlanLayerBar CreatePlanLayer(Color col, PlanLayer planLayer, Plan plan, string title = "New Layer")
	{
		GameObject go = Instantiate(planLayerPrefab);
		PlanLayerBar layer = go.GetComponent<PlanLayerBar>();
		planLayerBars.Add(layer);

		layer.title.text = title;

		return layer;
	}

	public void RefreshPlanBarInteractablityForAllPlans()
	{
		foreach (var kvp in planToPlanBar)
		{
			RefreshPlanBarInteractablity(kvp.Key, kvp.Value);
		}
	}

	private void RefreshPlanBarInteractablity(Plan planRepresenting, PlanBar planBar)
	{
		if (planRepresenting.State == Plan.PlanState.DELETED)
		{
			planBar.SetViewEditButtonState(null);
		}
		else
		{
			//planBar.SetViewEditButtonState(!planRepresenting.InInfluencingState);
			planBar.SetViewEditButtonState(false);
		}
		planBar.SetViewEditButtonInteractable(!Main.InEditMode && !Main.EditingPlanDetailsContent && !Main.PreventPlanAndTabChange);
		planBar.SetPlanBarToggleInteractability(!Main.InEditMode && !Main.EditingPlanDetailsContent && !Main.PreventPlanAndTabChange);
	}
}
