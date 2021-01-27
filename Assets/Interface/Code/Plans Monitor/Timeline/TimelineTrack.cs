using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TimelineTrack : MonoBehaviour
{
    public Image[] trackColor;
    public GameObject buttonPrefab, inspectButtonPrefab;
    public Dictionary<int, TimelineButton> timelineButtons = new Dictionary<int, TimelineButton>(); 
    public RectTransform buttonLocation;
    public Animator anim;
    public  PlansTimeline timeline;

	private RectTransform rect;
	private bool inspectingGroup;
	private RectTransform Rect
	{
		get
		{
			if (rect == null)
				rect = GetComponent<RectTransform>();
			return rect;
		}
	}

	private void Start()
	{
		timeline.trackCoverButton.onClick.AddListener(() => ClosePlans());
		//timeline.trackCoverButton.gameObject.SetActive(false);
	}

	public void SetTrackColor(Color col)
    {
        for (int i = 0; i < trackColor.Length; i++)
        {
            trackColor[i].color = col;
        }
    }

    // Register the plan in a dictionairy
    public void RegisterEvent(Plan plan)
    {
		int year = plan.StartTime - (plan.StartTime % 12);
        if(!timelineButtons.ContainsKey(year))
        {
            TimelineButton button = CreateEventButton(year);
            button.AddPlan(plan);
        }
        else
        {
            TimelineButton button = timelineButtons[year];
            button.AddPlan(plan);
        }
    }

    /// <summary>
    /// Removes the given plan from the timeline. If the plans time has changed since insertion, the old time must also be passed.
    /// </summary>
    public void RemoveTrackEvent(Plan plan, int originalTime = -1)
	{
		int time = (originalTime != -1) ? originalTime : plan.StartTime;
		time = time - (time % 12);
		TimelineButton button;
		if (timelineButtons.TryGetValue(time, out button))
		{
			if (button.RemovePlan(plan))
			{
				timelineButtons.Remove(time);
			}
		}
    }

    public void UpdatetrackEventFor(Plan plan, int oldTime)
    {
		int year = oldTime - (oldTime % 12);
		if (timelineButtons[year].RemovePlan(plan))
            timelineButtons.Remove(year);
		if(plan.ShouldBeVisibleInUI)
			RegisterEvent(plan);
    }

    /// <summary>
    /// Creates a new timeline button at the given month. Add its to the timeline.
    /// </summary>
    public TimelineButton CreateEventButton(int month)
    {
        GameObject go = Instantiate(buttonPrefab);
		int year = Mathf.Max(0, month / 12);

        TimelineButton eventButton = go.GetComponent<TimelineButton>();

        go.transform.SetParent(buttonLocation, false);
        
        eventButton.SetButtonColor(trackColor[0].color);
		eventButton.SetMonth(month, timeline.timeLineUtil.yearMarkers[year]);
        eventButton.button.onClick.AddListener(() => CallEventButton(eventButton));

        timelineButtons[month] = eventButton;

        return eventButton;
    }

    /// <summary>
    /// Creates a new timeline button. Does not add it to the timeline and immediately adds the given plan.
    /// Used to create the temporary buttons for plan group inspecting.
    /// </summary>
    private TimelineButton CreateInspectButton(Plan plan)
    {
        GameObject go = Instantiate(inspectButtonPrefab);

        TimelineButton eventButton = go.GetComponent<TimelineButton>();

		eventButton.transform.SetParent(timeline.groupButtonLocation, false);
		eventButton.SetButtonColor(trackColor[0].color);
        eventButton.button.onClick.AddListener(() => CallEventButton(eventButton));
        eventButton.AddPlan(plan);
		eventButton.monthNameText.text = Util.MonthToMonthText(plan.StartTime, true);

		return eventButton;
    }

    // Determine what event to call based on the number of events in that month
    public void CallEventButton(TimelineButton button)
    {
        if (button.plans.Count == 1)
        {
            if (inspectingGroup)
                ClosePlans();
            InspectPlan(button.plans[0]);
        }
        else
        {
            InspectPlans(button.plans, button);
        }
    }

    // Click on a single plan
    public void InspectPlan(Plan plan)
    {
        InterfaceCanvas.Instance.menuBarPlansMonitor.toggle.isOn = true;       
        //if(!PlanDetails.IsOpen)
        PlansMonitor.instance.plansMinMax.Maximize();
        PlanDetails.SelectPlan(plan);
    }

    // Click on a group of plans
    public void InspectPlans(List<Plan> plansToInspect, TimelineButton button)
    {
		timeline.trackCoverButton.gameObject.SetActive(true);

		timeline.IsolateButtonGroup(true);
        anim.SetBool("Show", true);
        inspectingGroup = true;

		plansToInspect.Sort();
		timeline.inspectingYearText.text = Util.MonthToYearText(plansToInspect[plansToInspect.Count - 1].StartTime);

        for (int i = 0; i < plansToInspect.Count; i++)
        {
            CreateInspectButton(plansToInspect[i]);
        }
    }

    // Close a group of plans when inspecting
    public void ClosePlans()
    {
		timeline.trackCoverButton.gameObject.SetActive(false);
		timeline.inspectingYearText.text = "";
		inspectingGroup = false;
        timeline.IsolateButtonGroup(false);
        anim.SetBool("Show", false);
		//Ignores the first child as thats the text
        for (int i = 1; i < timeline.groupButtonLocation.childCount; i++)
        {
            Destroy(timeline.groupButtonLocation.GetChild(i).gameObject);
        }
    }

    #region Button Sorting

    public void BringToFront(RectTransform button)
    {
        button.SetAsLastSibling();
    }

    public void ResetOrder()
    {
        //for (int i = 0; i < timelineButtons.Count; i++)
        //{
        //    timelineButtons[i].rect.SetSiblingIndex(i);
        //}
    }

    #endregion
}