using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using System.Text;

public class PlanDetails : SerializedMonoBehaviour
{
	public enum EPlanDetailsTab
	{
		Feedback = 0,
		Layers,
		Description,
		Shipping,
		Energy,
		Ecology,
		Issues,
	};
    
	private bool isOwner;

	[Header("Plan Information Bar")]
	public Image countryBall;
	public TextMeshProUGUI planTitle;
	public TextMeshProUGUI planDateText;
	public CustomDropdown statusDropdown;
	[SerializeField]
	private Button changeDetailsButton = null;
	[SerializeField]
	private Button forceUnlockButton = null;

    private bool ignoreStatusDropdownChanged = false;
    List<Plan.PlanState> stateDropdownOptions;
		
	[Header("Editing tab content")]
	public GameObject editTabContentBox;
	public TextMeshProUGUI editTabContentText;
	public Button editTabContentButton;

	[Header("Editing confirmation")]
	public GameObject changesConfirmCancelBox;
	public TextMeshProUGUI changesConfirmCancelText;
	public Button changesConfirmButton, changesCancelButton;

	[Header("Tabs")]
	[SerializeField]
	private Dictionary<EPlanDetailsTab, PlanDetailsTab> tabs;
	[SerializeField]
	PlanDetailsTabLayers layersTab; //Specifically linked because it is referenced often
	private PlanDetailsTab currentTab;

	[Header("Cover")]
	[SerializeField]
	private GameObject detailsCover = null;

	[HideInInspector]
	public static PlanDetails instance;
	private Plan selectedPlan = null;
	public Plan SelectedPlan { get { return selectedPlan; } }
	public static PlanDetailsTabLayers LayersTab => instance.layersTab;

	protected void Awake()
	{
		instance = this;
	}

	protected void Start()
	{
		changeDetailsButton.onClick.AddListener(() =>
		{
			if (selectedPlan == null) return;

			selectedPlan.AttemptLock((plan) =>
			{
				//If lock is succesful, open planwizard and set to selected plan
				PlanWizard planWizard = UIManager.GetInterfaceCanvas().planWizard;
				planWizard.gameObject.SetActive(true);
				planWizard.SetToPlan(plan);

			}, null);
		});
				
        statusDropdown.onValueChanged.AddListener(OnStatusDropdownValueChanged);

		IssueManager.instance.SubscribeToIssueChangedEvent(OnPlanLayerIssuesChanged);

		forceUnlockButton.gameObject.SetActive(false);
		if (TeamManager.IsGameMaster)
		{
			forceUnlockButton.onClick.AddListener(() =>
			{
                if (selectedPlan != null)
                {
                    UnityEngine.Events.UnityAction lb = () => { };
                    UnityEngine.Events.UnityAction rb = () => PlanManager.RequestForceUnlockPlan(selectedPlan);
                    DialogBoxManager.instance.ConfirmationWindow("Force unlock plan", "Are you sure you want to force unlock this plan?", lb, rb);
                }
            });
		}

		TabSelect(EPlanDetailsTab.Feedback);
	}

	public static void LockStateChanged(Plan plan, bool lockState)
	{
		if (instance == null || instance.selectedPlan == null) return; //This window cant even be open so no point in updating anyways
		if (lockState)
		{
			//Locked
			if (plan == instance.selectedPlan)
				instance.DisableStateChangeAndPlanWizard();
		}
		else
			//Unlocked
			UpdateStatusIfActivePlan(plan);
	}

	public static void UpdateNameAndDescription(Plan plan)
	{
		if (instance == null || instance.selectedPlan == null) return; //This window cant even be open so no point in updating anyways
		if (plan == instance.selectedPlan)
		{
			instance.planTitle.text = plan.Name;
			instance.tabs[EPlanDetailsTab.Description].UpdateTabContent();
		}
	}

	public static Plan GetSelectedPlan()
	{
		return instance.selectedPlan;
	}

	public static bool IsOpen
	{
		get { return instance.gameObject.activeInHierarchy; }
		set
		{
			instance.gameObject.SetActive(value);
			if (value && instance.selectedPlan != null)
				PlanManager.SetPlanUnseenChanges(instance.selectedPlan, false);
		}
	}

	public static void ChangeDate(Plan plan)
	{
		ConstraintManager.CheckConstraints(plan, null, false);

		if (instance == null || instance.selectedPlan == null) return; //This window cant even be open so no point in updating anyways
		if (plan == instance.selectedPlan)
		{
			instance.SetRealisationDate(plan.ConstructionStartTime, plan.StartTime);
		}
	}

	void SetRealisationDate(int constructionStartTime, int implementationTime)
	{
		int constuctionTime = implementationTime - constructionStartTime;
		if (constuctionTime == 0)
			planDateText.text = $"Due {Util.MonthToText(implementationTime)}. No construction time required.";
		else if (constuctionTime == 1)
			planDateText.text = $"Due {Util.MonthToText(implementationTime)}, after 1 month construction.";
		else
			planDateText.text = $"Due {Util.MonthToText(implementationTime)}, after {constuctionTime} months construction.";
	}

	public void DisableStateChangeAndPlanWizard()
	{
		changeDetailsButton.interactable = false;
		statusDropdown.interactable = false;
	}

	public static void SelectPlan(Plan plan)
	{
		instance.SetPlan(plan);
	}

	public static void UpdateStatusIfActivePlan(Plan plan)
	{
		if (instance.selectedPlan != null && instance.selectedPlan.ID == plan.ID)
			instance.StatusUpdate();
	}

	public static void UpdateStatus()
	{
		if (instance.selectedPlan != null)
			instance.StatusUpdate();
	}

	public static void UpdateButtonInteractability()
	{
		if (instance.selectedPlan != null)
		{
			instance.changeDetailsButton.interactable = (instance.isOwner || TeamManager.IsManager) && !instance.selectedPlan.InInfluencingState && !Main.InEditMode && !Main.EditingPlanDetailsContent;
			UpdateTabAvailability();
		}
	}

	public bool CanStartEditingContent
	{
		get
		{
			return (isOwner || TeamManager.IsManager) && selectedPlan.State == Plan.PlanState.DESIGN && !Main.InEditMode && !Main.EditingPlanDetailsContent;
		}
	}

	public void CancelEditingContent()
	{
		((LockablePlanDetailsTab)currentTab).CancelChangesAndUnlock();
	}

	public static void AddFeedbackFromServer(List<PlanMessageObject> planMessages)
	{
		PlanDetailsTabFeedback feedbacktab = ((PlanDetailsTabFeedback)instance.tabs[EPlanDetailsTab.Feedback]);
		foreach (PlanMessageObject message in planMessages)
		{
			feedbacktab.AddFeedback(message);
		}
		feedbacktab.UpdateTabContent();
	}

	void SetPlan(Plan plan)
	{
		if (Main.InEditMode && plan.ID != Main.CurrentlyEditingPlan.ID || plan == selectedPlan || Main.EditingPlanDetailsContent)
			return;

		if (selectedPlan != null)
			PlansMonitor.SetPlanBarToggleState(selectedPlan, false);
		selectedPlan = plan;

		if (plan != null)
		{
			detailsCover.SetActive(false);
			forceUnlockButton.gameObject.SetActive(TeamManager.IsGameMaster);	
			PlansMonitor.SetPlanBarToggleState(plan, true);
		}
		else
		{
			detailsCover.SetActive(true);
			forceUnlockButton.gameObject.SetActive(false);
			return;
		}


		planTitle.text = plan.Name;
		countryBall.color = TeamManager.GetTeamByTeamID(plan.Country).color;
		SetRealisationDate(plan.ConstructionStartTime, plan.StartTime);

		isOwner = plan.Country == TeamManager.CurrentUserTeamID;
        StatusUpdate();

		if (plan.IsLocked)
		{
			DisableStateChangeAndPlanWizard();
		}

		Plan tmpPlan = plan;
		
		updateTabContent();

		
		TabSelect(EPlanDetailsTab.Feedback);
		updateTabAvailability();
	}

    void OnStatusDropdownValueChanged(int newDropDownValue)
    {
        if (ignoreStatusDropdownChanged || !statusDropdown.interactable)
            return;
        if (newDropDownValue >= stateDropdownOptions.Count)
            Debug.LogError("State of higher index than available states selected");

        Plan.PlanState newState = stateDropdownOptions[newDropDownValue];
            
        if (newState == selectedPlan.State)
            return;
        if (selectedPlan.RequiresTimeChange)
        {
			DialogBoxManager.instance.NotificationWindow("Cannot change state", "The plan's construction start time has passed. To restore the plan, change its implementation date.", () => { }, "Dismiss");
			ignoreStatusDropdownChanged = true;
			for (int i = 0; i < stateDropdownOptions.Count; i++)
			{
				if (stateDropdownOptions[i] == selectedPlan.State)
				{
					statusDropdown.value = i;
					statusDropdown.captionText.text = "State: " + selectedPlan.State.GetDisplayName();

					break;
				}
			}
			ignoreStatusDropdownChanged = false;
        }
        else
        {
            AcceptStatus(selectedPlan, newState);
        }
    }
	
    public static void UpdateTabAvailability()
    {
        instance.updateTabAvailability();
    }

    void updateTabAvailability()
    {
		foreach (var kvp in tabs)
			kvp.Value.UpdateTabAvailability();
	}

	public static void UpdateTabContent()
	{
		instance.updateTabContent();
	}

	void updateTabContent()
	{
		if (selectedPlan == null)
			return;

		foreach (var kvp in tabs)
			kvp.Value.UpdateTabContent();
	}

	void OnPlanLayerIssuesChanged(PlanLayer changedIssueLayer)
	{
		if (selectedPlan != null)
		{
			if (selectedPlan.PlanLayers.Contains(changedIssueLayer))
			{
				((PlanDetailsTabIssues)tabs[EPlanDetailsTab.Issues]).UpdateIssueStatus();
			}
		}
	}
	
	// Call when plan status has changed
	void StatusUpdate()
	{
		Plan.PlanState planState = selectedPlan.State;
		statusDropdown.interactable = false;
		changeDetailsButton.interactable = false;
		UpdateTabAvailability();

		//If simulation, disable all changeable UI elements
		if (GameState.CurrentState == GameState.PlanningState.Simulation)
		{
			return;
		} 

		//If state is implemented, nothing needs to be updated
		if (planState != Plan.PlanState.IMPLEMENTED)
		{
            //If owner/manager
            if (isOwner || TeamManager.IsManager)
            {
                statusDropdown.interactable = true;
                SetStatusDropdownOptions();
                if (!Main.InEditMode && !Main.EditingPlanDetailsContent)
                {
                    changeDetailsButton.interactable = !selectedPlan.InInfluencingState;
                }
            }
            else
                statusDropdown.captionText.text = "State: " + planState.GetDisplayName();
        }
        else
            statusDropdown.captionText.text = "State: Implemented";
	}
		
    void SetStatusDropdownOptions()
    {
        List<Plan.PlanState> availableStates = null;

        //Set the right available states
        if (selectedPlan.HasErrors() || selectedPlan.State == Plan.PlanState.DELETED)
            availableStates = new List<Plan.PlanState>() { Plan.PlanState.DELETED, Plan.PlanState.DESIGN };        
        else if (selectedPlan.NeedsApproval())
            availableStates = TeamManager.IsManager ?
              new List<Plan.PlanState>() { Plan.PlanState.DELETED, Plan.PlanState.DESIGN, Plan.PlanState.CONSULTATION, Plan.PlanState.APPROVAL, Plan.PlanState.APPROVED }
            : new List<Plan.PlanState>() { Plan.PlanState.DELETED, Plan.PlanState.DESIGN, Plan.PlanState.CONSULTATION, Plan.PlanState.APPROVAL };
        else
            availableStates = new List<Plan.PlanState>() { Plan.PlanState.DELETED, Plan.PlanState.DESIGN, Plan.PlanState.CONSULTATION, Plan.PlanState.APPROVED };

        //Add the current state if it wasnt already available
        if (!availableStates.Contains(selectedPlan.State))
            availableStates.Add(selectedPlan.State);

        SetStatusDropdownOptions(availableStates, selectedPlan.State);
    }

    void SetStatusDropdownOptions(List<Plan.PlanState> availableStates, Plan.PlanState selectedState)
    {
        stateDropdownOptions = availableStates;

        //Recreate dropdown options
        statusDropdown.ClearOptions();
        List<string> options = new List<string>(availableStates.Count);
        foreach (Plan.PlanState state in availableStates)
            options.Add(state.GetDisplayName());
        statusDropdown.AddOptions(options);

        //Set the dropdown value to the right state
        ignoreStatusDropdownChanged = true;
        for (int i = 0; i < availableStates.Count; i++)
            if (availableStates[i] == selectedState)
            {
                statusDropdown.value = i;
				statusDropdown.captionText.text = "State: " + selectedState.GetDisplayName();
				break;
            }
        ignoreStatusDropdownChanged = false;
    }

    public void AcceptStatus(Plan plan, Plan.PlanState newState)
	{
		//Lock plan, if successful, change state
		plan.AttemptLock((changedPlan) =>
		{
            bool submitDelayed = false;
			BatchRequest batch = new BatchRequest();
			plan.AttemptUnlock(batch);

			if (changedPlan.energyPlan)
            {
                if (changedPlan.InInfluencingState && !newState.IsInfluencingState())
                {
                    //Check for later plans overlapping with this one
                    submitDelayed = true;
                    NetworkForm form = new NetworkForm();
                    form.AddField("plan_id", changedPlan.ID);
                    ServerCommunication.DoRequest<int[]>(Server.GetDependentEnergyPlans(), form, (planErrorsIDs) => CreatePlanChangeConfirmPopup(planErrorsIDs, changedPlan, newState, batch));

                }
                else if (!changedPlan.InInfluencingState && newState.IsInfluencingState())
                {
                    submitDelayed = true;

                    //Check if all cables are still valid
                    if (changedPlan.CheckForInvalidCables())
                    {
                        //Create notification window
                        DialogBoxManager.instance.NotificationWindow("Invalid energy plan", "Another plan changed state while this plan was being edited, invalidating this plan. Edit this plan to fix the error.", null);
                        changedPlan.SubmitEnergyError(true, false, batch);
						batch.ExecuteBatch(null, null);
						return;
                    }

                    //Check if the plan wasnt invalidated while being edited
                    Debug.Log("Request prev overlap for plan: " + changedPlan.ID);
                    NetworkForm form = new NetworkForm();
                    form.AddField("plan_id", changedPlan.ID);
                    ServerCommunication.DoRequest<int>(Server.OverlapsWithPreviousEnergyPlans(), form, (i) =>
                    {
						if (i == 0)
						{
							//Check for later plans overlapping with this one
							NetworkForm form2 = new NetworkForm();
							form2.AddField("plan_id", changedPlan.ID);
							ServerCommunication.DoRequest<int[]>(Server.GetOverlappingEnergyPlans(), form2, (planErrorsIDs2) => CreatePlanChangeConfirmPopup(planErrorsIDs2, changedPlan, newState, batch));
						}
						else
						{
							DialogBoxManager.instance.NotificationWindow("Invalid energy plan","Another plan changed state while this plan was being edited, invalidating this plan. Edit this plan to fix the error.",null);
							batch.ExecuteBatch(null, null);
						}
                    });
                }
            }

            if (!submitDelayed)
            {
                SendMessage(changedPlan.ID, "Changed the plans status to: " + newState.GetDisplayName(), batch);
				plan.SetState(newState, batch);
				batch.ExecuteBatch(null, null);
			}
		}, null);
	}

    private void CreatePlanChangeConfirmPopup(int[] planErrorsIDs, Plan plan, Plan.PlanState targetState, BatchRequest batch)
    {
        if (planErrorsIDs == null || planErrorsIDs.Length == 0)
        {
            //No plans with errors, set new state
            SendMessage(plan.ID, "Changed the plans status to: " + targetState.GetDisplayName(), batch);
			plan.SetState(targetState, batch);
			batch.ExecuteBatch(null, null);
		}
        else
        {
            //Will cause errors in other plans, ask for confirmation
            Plan errorPlan = PlanManager.GetPlanWithID(planErrorsIDs[0]);

            //Create text for warning message, with names of affected plans
			StringBuilder notificationText = new StringBuilder(256);
			notificationText.Append("Changing this plan's state will cause errors for other plans, they will be moved to design and need to have their energy distribution confirmed.\n\nThe affected plans are:\n\n");
			for (int i = 0; i < planErrorsIDs.Length && i < 4; i++)
			{
				errorPlan = PlanManager.GetPlanWithID(planErrorsIDs[i]);
				notificationText.Append("<color=#").Append(Util.ColorToHex(TeamManager.GetTeamByTeamID(errorPlan.Country).color)).Append(">");
				notificationText.Append(" - ").Append(errorPlan.Name).Append("\n");
				notificationText.Append("</color>");
			}
			if (planErrorsIDs.Length > 4)
				notificationText.Append("and " + (planErrorsIDs.Length - 4).ToString() + " others.");

			//Create confirmation window
			BatchRequest batchRequest = batch; //Create local variable for batch
            string description = notificationText.ToString();
            UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() =>
			{
				batchRequest.ExecuteBatch(null, null);
			});
            UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() =>
            {
                SendMessage(plan.ID, "Changed the plans status to: " + targetState.GetDisplayName(), batch);
                plan.SetState(targetState, batchRequest);
				batchRequest.ExecuteBatch(null, null);
            });
            DialogBoxManager.instance.ConfirmationWindow("Energy error warning", description, lb, rb);
        }
    }

	public void SendMessage(int planID, string text)
	{
		NetworkForm form = new NetworkForm();
		form.AddField("plan", planID);
		form.AddField("team_id", TeamManager.CurrentUserTeamID);
		form.AddField("user_name", TeamManager.CurrentUserName);
		form.AddField("text", text);
		ServerCommunication.DoRequest(Server.PostPlanFeedback(), form);
	}

	public void SendMessage(int planID, string text, BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("plan", planID);
		dataObject.Add("team_id", TeamManager.CurrentUserTeamID);
		dataObject.Add("user_name", TeamManager.CurrentUserName);
		dataObject.Add("text", text);
		batch.AddRequest(Server.PostPlanFeedback(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void TabSelect(EPlanDetailsTab tab)
	{
        currentTab = tabs[tab];
		currentTab.SetTabActive(true);
	}

	public PlanDetailsTab GetTab(EPlanDetailsTab tab)
	{
		return tabs[tab];
	}

}