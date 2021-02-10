using System.Collections.Generic;
using UnityEngine;
using TMPro;

class PlanDetailsTabIssues : PlanDetailsTab
{
	[SerializeField]
	private PlanIssuesEntry issueEntryPrefab = null;
	[SerializeField]
	private Transform issueEntryLocation = null;
	[SerializeField]
	private TextMeshProUGUI issueIndicator;

	private List<KeyValuePair<PlanIssueInstance, PlanIssuesEntry>> issueEntries = new List<KeyValuePair<PlanIssueInstance, PlanIssuesEntry>>();

	protected override PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Issues;

	public override void UpdateTabContent()
	{
		UpdateIssueStatus();

		if (!isActive)
			return;

		ClearIssues();
		PopulateIssuesList(PlanDetails.GetSelectedPlan());
		emptyContentOverlay.SetActive(issueEntries != null && issueEntries.Count == 0);
	}

	protected override void OnTabActivate()
	{
		base.OnTabActivate();
		IssueManager.instance.SubscribeToIssueChangedEvent(OnIssuesChanged);
		planDetails.editTabContentBox.SetActive(false);
	}

	protected override void OnTabDeactivate()
	{
		base.OnTabDeactivate();
		IssueManager.instance.UnsubscribeFromIssueChangedEvent(OnIssuesChanged);
		ClearIssues();
	}

	private void PopulateIssuesList(Plan selectedPlan)
	{
		foreach (PlanIssueInstance issue in IssueManager.instance.FindIssuesForPlan(selectedPlan))
		{
			CreateIssueInstanceMessage(issue);
		}
		if (selectedPlan.energyError)
		{
			CreateEnergyErrorInstanceMessage();
		}
	}

	public void ClearIssues()
	{
		for (int i = 0; i < issueEntries.Count; ++i)
		{
			Destroy(issueEntries[i].Value.gameObject);
		}

		issueEntries.Clear();
	}

	private void CreateEnergyErrorInstanceMessage()
	{
		PlanIssuesEntry go = Instantiate(issueEntryPrefab);
		go.transform.SetParent(issueEntryLocation, false);
		go.SetText("<color=#FF5454>[Error]</color> The energy distribution has been invalidated and must be recalculated. To do this move the plan to the design state, start editing and accept. Note that editing might change energy cables and distributions to repair the plan, make sure to check these.");
		go.DisableViewOnMapButton();
		issueEntries.Add(new KeyValuePair<PlanIssueInstance, PlanIssuesEntry>(null, go));
	}

	private void CreateIssueInstanceMessage(PlanIssueInstance planIssueInstance)
	{
		PlanIssuesEntry go = Instantiate(issueEntryPrefab);
		go.transform.SetParent(issueEntryLocation, false);
		go.SetViewOnMapClickedAction(() =>
		{
			OnViewOnMapClicked(planIssueInstance);
		});

		string issueText;
		switch (planIssueInstance.PlanIssueData.type)
		{
		case ERestrictionIssueType.Info:
			issueText = "<color=#7bd7f6>[Info]</color> " + planIssueInstance.Text;
			break;
		case ERestrictionIssueType.Warning:
			issueText = "<color=#FFFA31>[Warning]</color> " + planIssueInstance.Text;
			break;
		case ERestrictionIssueType.Error:
			issueText = "<color=#FF5454>[Error]</color> " + planIssueInstance.Text;
			break;
		default:
			issueText = "Unknow issue type " + planIssueInstance.PlanIssueData.type;
			break;
		}

		go.SetText(issueText);

		issueEntries.Add(new KeyValuePair<PlanIssueInstance, PlanIssuesEntry>(planIssueInstance, go));
	}

	private void OnIssuesChanged(PlanLayer changedissuelayer)
	{
		if (changedissuelayer.Plan == PlanDetails.GetSelectedPlan())
		{
			ClearIssues();
			PopulateIssuesList(PlanDetails.GetSelectedPlan());
		}
	}

	private void OnViewOnMapClicked(PlanIssueInstance planIssueInstance)
	{
		PlanManager.ShowPlan(PlanDetails.GetSelectedPlan());
		IssueManager.instance.ShowRelevantPlanLayersForIssue(planIssueInstance);
		PlansMonitor.instance.plansMinMax.Minimize();

		Rect viewBounds = new Rect(planIssueInstance.PlanIssueData.x, planIssueInstance.PlanIssueData.y, 1.0f, 1.0f);
		CameraManager.Instance.ZoomToBounds(viewBounds);
	}

	public void UpdateIssueStatus()
	{
		ERestrictionIssueType severity = planDetails.SelectedPlan.energyError ? ERestrictionIssueType.Error : IssueManager.instance.GetMaximumSeverity(planDetails.SelectedPlan);
		switch (severity)
		{
			case ERestrictionIssueType.None:
			case ERestrictionIssueType.Info:
				issueIndicator.enabled = false;
				break;
			case ERestrictionIssueType.Warning:
				issueIndicator.enabled = true;
				issueIndicator.color = new Color(1f, 250f / 255, 49f / 255f);
				break;
			case ERestrictionIssueType.Error:
				issueIndicator.enabled = true;
				issueIndicator.color = new Color(1f, 84f / 255, 84f / 255f);
				break;
		}
	}
}

