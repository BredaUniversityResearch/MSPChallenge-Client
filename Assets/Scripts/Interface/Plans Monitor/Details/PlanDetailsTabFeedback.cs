using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;

public class PlanDetailsTabFeedback : PlanDetailsTab
{
	[Header("Messages")]
	[SerializeField]
	ScrollRect scrollRect;
	[SerializeField]
	CustomInputField chatInputField;
	[SerializeField]
	GameObject planDetailsEntriesPrefab;
	[SerializeField]
	Transform feedbackEntryLocation;
	[SerializeField]
	private Button sendFeedbackButton = null;

	[Header("Approval")]
	[SerializeField]
	GameObject planApprovalSection;
	[SerializeField]
	Transform planApprovalEntryParent;
	[SerializeField]
	GameObject planApprovalEntryPrefab;
	[SerializeField]
	GameObject approvalRequiredIndicator;

	List<PlanApprovalEntry> planApprovalEntries = new List<PlanApprovalEntry>();
	List<TextMeshProUGUI> feedbackEntries = new List<TextMeshProUGUI>();
	private static HashSet<int> receivedPlanMessages = new HashSet<int>();
	private Dictionary<Plan, List<string>> messageEntriesPerPlan = new Dictionary<Plan, List<string>>();
	int nextAvailableFeedbackIndex = 0;
	int nextAvailableApprovalIndex = 0;
	Plan lockedPlan;


	protected override void Initialise()
	{
		base.Initialise();

		sendFeedbackButton.onClick.AddListener(() =>
		{
			if (planDetails.SelectedPlan == null)
				return;
			PostFeedback(planDetails.SelectedPlan);
		});
	}

	protected override void OnTabActivate()
	{
		base.OnTabActivate();
		planDetails.editTabContentBox.SetActive(false);
	}

	public override void UpdateTabContent()
	{
		if (planDetails.SelectedPlan == null)
			return;

		approvalRequiredIndicator.SetActive(planDetails.SelectedPlan.State == Plan.PlanState.APPROVAL);	

		if (!isActive)
			return;

		UpdateFeedbackEntries();

		if (planDetails.SelectedPlan.State == Plan.PlanState.APPROVAL)
		{
			UpdateApprovalEntries();
			planApprovalSection.SetActive(true);
			emptyContentOverlay.SetActive(false);
		}
		else
		{
			planApprovalSection.SetActive(false);
			emptyContentOverlay.SetActive(true);
		}

		Canvas.ForceUpdateCanvases();
		scrollRect.verticalNormalizedPosition = 0f;
		Canvas.ForceUpdateCanvases();

		emptyContentOverlay.SetActive(nextAvailableFeedbackIndex == 0);
	}

	protected void Update()
	{
		if (isActive && Input.GetKeyDown(KeyCode.Return))
		{
			if (!string.IsNullOrEmpty(chatInputField.text))
			{
				PostFeedback(planDetails.SelectedPlan);
			}
		}
	}

	private void UpdateFeedbackEntries()
	{
		nextAvailableFeedbackIndex = 0;
		//Set entries
		if (messageEntriesPerPlan.TryGetValue(planDetails.SelectedPlan, out var feedback))
		{
			for (; nextAvailableFeedbackIndex < feedback.Count; nextAvailableFeedbackIndex++)
			{
				if (nextAvailableFeedbackIndex < feedbackEntries.Count)
				{
					feedbackEntries[nextAvailableFeedbackIndex].text = feedback[nextAvailableFeedbackIndex];
					feedbackEntries[nextAvailableFeedbackIndex].gameObject.SetActive(true);
				}
				else
				{
					CreateEntry(feedback[nextAvailableFeedbackIndex]);
				}
			}
		}

		//Turn off unused entries
		for (int i = nextAvailableFeedbackIndex; i < feedbackEntries.Count; i++)
		{
			feedbackEntries[i].gameObject.SetActive(false);
		}
	}

	public void AddFeedback(PlanMessageObject planMessages)
	{
		if (receivedPlanMessages.Contains(planMessages.message_id))
			return;
		receivedPlanMessages.Add(planMessages.message_id);

		string feedback = $"<color=#A3A0A2>[{planMessages.time}]</color> [<color=#{ColorUtility.ToHtmlStringRGB(TeamManager.GetTeamByTeamID(planMessages.team_id).color)}>{planMessages.user_name}</color>] {planMessages.message}";

		Plan plan = PlanManager.GetPlanWithID(planMessages.plan_id);

		if (messageEntriesPerPlan.TryGetValue(plan, out var list))
		{
			messageEntriesPerPlan[plan].Add(feedback);
		}
		else
		{
			messageEntriesPerPlan.Add(plan, new List<string>() { feedback });
		}
	}

	void CreateEntry(string text)
	{
		GameObject go = Instantiate(planDetailsEntriesPrefab, feedbackEntryLocation);
		TextMeshProUGUI textObj = go.GetComponentInChildren<TextMeshProUGUI>();
		feedbackEntries.Add(textObj);
		textObj.text = text;
	}

	public void PostFeedback(Plan plan)
	{
		if (plan == null)
		{
			return;
		}

		if (chatInputField.text != "")
		{
			planDetails.SendMessage(plan.ID, chatInputField.text);
			chatInputField.text = "";
			chatInputField.ActivateInputField();
			chatInputField.Select();
		}
		//Canvas.ForceUpdateCanvases();
		//scrollRect.verticalNormalizedPosition = 0f;
		//Canvas.ForceUpdateCanvases();
	}

	void UpdateApprovalEntries()
	{
		nextAvailableApprovalIndex = 0;

		//Set entries
		foreach (var kvp in planDetails.SelectedPlan.countryApproval)
		{

			if (nextAvailableApprovalIndex < planApprovalEntries.Count)
			{
				planApprovalEntries[nextAvailableApprovalIndex].SetContent(TeamManager.GetTeamByTeamID(kvp.Key), kvp.Value);
				planApprovalEntries[nextAvailableApprovalIndex].gameObject.SetActive(true);
			}
			else
			{
				GameObject go = Instantiate(planApprovalEntryPrefab, planApprovalEntryParent);
				PlanApprovalEntry entry = go.GetComponentInChildren<PlanApprovalEntry>();
				entry.SetCallback(ApprovalChangedCountry);
				planApprovalEntries.Add(entry);
				entry.SetContent(TeamManager.GetTeamByTeamID(kvp.Key), kvp.Value);
			}
			nextAvailableApprovalIndex++;
		}

		//Turn off unused entries
		for (int i = nextAvailableApprovalIndex; i < planApprovalEntries.Count; i++)
		{
			planApprovalEntries[i].gameObject.SetActive(false);
		}
	}

	public void ApprovalChangedCountry(Team team, EPlanApprovalState newApproval)
	{
		planDetails.SelectedPlan.AttemptLock((changedPlan) =>
		{
			lockedPlan = changedPlan;
			BatchRequest batch = new BatchRequest();
			planDetails.SelectedPlan.AttemptUnlock(batch);

			int planId = planDetails.SelectedPlan.ID;
			if (newApproval == EPlanApprovalState.Disapproved)
			{
				if (team.ID == TeamManager.CurrentUserTeamID)
					planDetails.SendMessage(planId, "Disapproved the plan.", batch);
				else
					planDetails.SendMessage(planId, "Disapproved the plan for <color=#" + Util.ColorToHex(team.color) + ">" + team.name + "</color>.", batch);

				SubmitApprovalState(newApproval, team, batch);

			}
			else if (newApproval == EPlanApprovalState.Maybe)
			{
				if (team.ID == TeamManager.CurrentUserTeamID)
					planDetails.SendMessage(planId, "Retracted the previous approval state.", batch);
				else
					planDetails.SendMessage(planId, "Retracted the previous approval state for <color=#" + Util.ColorToHex(team.color) + ">" + team.name + "</color>.", batch);

				SubmitApprovalState(newApproval, team, batch);
			}
			else
			{
				if (team.ID == TeamManager.CurrentUserTeamID)
					planDetails.SendMessage(planId, "Approved the plan.", batch);
				else
					planDetails.SendMessage(planId, "Approved the plan for <color=#" + Util.ColorToHex(team.color) + ">" + team.name + "</color>.", batch);

				//Set approval immediately so we can check for completion
				planDetails.SelectedPlan.countryApproval[team.ID] = EPlanApprovalState.Approved;
				if (planDetails.SelectedPlan.HasApproval())
				{
					planDetails.SelectedPlan.SetState(Plan.PlanState.APPROVED, batch);
				}
				else
					SubmitApprovalState(EPlanApprovalState.Approved, team, batch);
			}
			batch.ExecuteBatch(null, null);
		}, null);
	}

	void SubmitApprovalState(EPlanApprovalState state, Team country, BatchRequest batch)
	{
		JObject dataObject = new JObject();

		dataObject.Add("plan", planDetails.SelectedPlan.ID);
		dataObject.Add("country", country.ID);
		dataObject.Add("vote", (int)state);

		batch.AddRequest(Server.SetApproval(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	void UnlockComplete(string s)
	{
		lockedPlan = null;
		PlanDetails.UpdateTabContent();
	}
}
