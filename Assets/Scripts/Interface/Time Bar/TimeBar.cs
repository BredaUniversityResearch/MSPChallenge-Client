using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class TimeBar : MonoBehaviour
{
	private static TimeBar singleton;

	public static TimeBar instance
	{
		get
		{
			if (singleton == null)
				singleton = (TimeBar)FindObjectOfType(typeof(TimeBar));
			return singleton;
		}
	}

	//Timeline
	[Header("Timeline")]
	public TextMeshProUGUI collapsedDate;
	public Image fill;
	public TimeBarEraMarker eraMarkerPrefab;
	public RectTransform eraMarkerLocation;
	public List<TimeBarEraMarker> markers = new List<TimeBarEraMarker>();

	[SerializeField, Header("General")]
	GameObject simulationTimeContentTop;
	[SerializeField]
	TextMeshProUGUI simulationTimeText;
	[SerializeField]
	RectTransform simulationTimeIndicatorTop;
	[SerializeField]
	RectTransform viewingTimeIndicatorBottom;
	//[SerializeField]
	//ToggleGroup viewModeToggleGroup;
	[SerializeField]
	Image expandedBackground;
	[SerializeField]
	VerticalLayoutGroup layoutGroup;

	//View time
	[SerializeField, Header("View Time")]
	Toggle viewTimeToggle;
	[SerializeField]
	GameObject viewTimeContentBottom;
	[SerializeField]
	TMP_Dropdown viewTimeMonthDropdown;
	[SerializeField]
	TMP_Dropdown viewTimeYearDropdown;

	//View difference
	[SerializeField, Header("View Difference")]
	Toggle viewDifferenceToggle;
	[SerializeField]
	GameObject viewDifferenceContentTop;
	[SerializeField]
	GameObject viewDifferenceContentBottom;
	[SerializeField]
	RectTransform viewDifferenceIndicatorTop;
	[SerializeField]
	RectTransform viewDifferenceIndicatorBottom;
	[SerializeField]
	TMP_Dropdown viewDifferenceMonthDropdown0;
	[SerializeField]
	TMP_Dropdown viewDifferenceYearDropdown0;
	[SerializeField]
	TMP_Dropdown viewDifferenceMonthDropdown1;
	[SerializeField]
	TMP_Dropdown viewDifferenceYearDropdown1;

	//View plan
	[SerializeField, Header("View Plan")]
	GameObject planViewingContentBottom;
	[SerializeField]
	TextMeshProUGUI planViewingText;

	//Indices selected in dropdowns
	int selectedMonthView, selectedYearView = 0;
	int selectedMonthDiff0, selectedYearDiff0 = 0;
	int selectedMonthDiff1, selectedYearDiff1 = 0;

	int maxSelectableMonth = 0;
	int maxSelectableYear = 0;
	bool ignoreActivityCallback = false;

	public enum WorldViewMode { Normal, Time, Difference, Plan }
	WorldViewMode viewMode = WorldViewMode.Normal;

	public void Start()
	{
        if (Main.MspGlobalData != null)
        {
            CreateEraMarkers();
        }
        else
        {
            Main.OnGlobalDataLoaded += GlobalDataLoaded;
        }

		viewTimeToggle.onValueChanged.AddListener((b) =>
		{
			if(b)
				SetViewMode(WorldViewMode.Time, true);
			else
				SetViewMode(WorldViewMode.Normal, true);
		});
		viewDifferenceToggle.onValueChanged.AddListener((b) =>
		{
			if (b)
				SetViewMode(WorldViewMode.Difference, true);
			else
				SetViewMode(WorldViewMode.Normal, true);
		});

		viewTimeMonthDropdown.onValueChanged.AddListener(ViewingMonthDropdownChanged);
		viewTimeYearDropdown.onValueChanged.AddListener(ViewingYearDropdownChanged);

		viewDifferenceMonthDropdown0.onValueChanged.AddListener((v) => DifferenceMonthDropdownChanged(v, 0));
		viewDifferenceYearDropdown0.onValueChanged.AddListener((v) => DifferencedYearDropdownChanged(v, 0));

		viewDifferenceMonthDropdown1.onValueChanged.AddListener((v) => DifferenceMonthDropdownChanged(v, 1));
		viewDifferenceYearDropdown1.onValueChanged.AddListener((v) => DifferencedYearDropdownChanged(v, 1));
	}

    void GlobalDataLoaded()
    {
        Main.OnGlobalDataLoaded -= GlobalDataLoaded;
        CreateEraMarkers();
    }

    void CreateEraMarkers()
    {
        for (int i = 0; i < MspGlobalData.num_eras; i++)
        {
            TimeBarEraMarker marker = (TimeBarEraMarker)Instantiate(eraMarkerPrefab, eraMarkerLocation, false);
            markers.Add(marker);

            // Set position based on month
            //float posX = ((month + 120) / (float)GameState.EndMonth) * eraMarkerLocation.rect.width;
            //marker.thisRectTrans.anchoredPosition = new Vector2(posX, marker.thisRectTrans.anchoredPosition.y);
        }
    }

	/// <summary>
	/// Set the date
	/// </summary>
	public void SetDate(int month)
	{
		fill.fillAmount = (float)month / (float)Main.MspGlobalData.session_end_month;
		collapsedDate.text = Util.MonthToText(month);
		simulationTimeText.text = Util.MonthToText(month);
		UpdateIndicator(simulationTimeIndicatorTop, month);
	}

	public void UpdatePlanViewing()
	{
		if(viewMode == WorldViewMode.Plan && PlanManager.planViewing != null)
		{
			planViewingText.text = Util.MonthToText(PlanManager.planViewing.StartTime, false);
			UpdateIndicator(viewingTimeIndicatorBottom, PlanManager.planViewing.StartTime);
		}
	}

	public void SetViewMode(WorldViewMode mode, bool updateWorldView)
	{
		if (mode == viewMode || ignoreActivityCallback)
			return;

        bool openingViewMode = viewMode == WorldViewMode.Plan || viewMode == WorldViewMode.Normal && (mode == WorldViewMode.Difference || mode == WorldViewMode.Time);

        if (openingViewMode)
        {
            if(Main.InEditMode || Main.EditingPlanDetailsContent)
            {
                DialogBoxManager.instance.NotificationWindow("In edit mode", "View settings are unavailable while editing a plan. Please confirm or cancel your changes before trying again.", null);
				viewTimeToggle.isOn = false;
				return;
            }
            if (PlanManager.planViewing != null)
            {
                ignoreActivityCallback = true;
                PlanManager.HideCurrentPlan(false);
                ignoreActivityCallback = false;
            }
            ignoreActivityCallback = true;
		    UpdateDropdowns();
		    ignoreActivityCallback = false;
        }

		SetViewModeElementsActive(viewMode, false, updateWorldView);
		viewMode = mode;
		SetViewModeElementsActive(viewMode, true, updateWorldView);

        ignoreActivityCallback = true;
		viewTimeToggle.isOn = mode == WorldViewMode.Time;
		viewDifferenceToggle.isOn = mode == WorldViewMode.Difference;
		ignoreActivityCallback = false;
	}

	void SetViewModeElementsActive(WorldViewMode mode, bool active, bool updateWorldView)
	{
		switch(mode)
		{
			case WorldViewMode.Plan:
				simulationTimeContentTop.SetActive(active);
				planViewingContentBottom.SetActive(active);
				simulationTimeIndicatorTop.gameObject.SetActive(active);
				viewingTimeIndicatorBottom.gameObject.SetActive(active);
				if (active)
					UpdatePlanViewing();
				break;
			case WorldViewMode.Time:
				simulationTimeContentTop.SetActive(active);
				viewTimeContentBottom.SetActive(active);
				simulationTimeIndicatorTop.gameObject.SetActive(active);
				viewingTimeIndicatorBottom.gameObject.SetActive(active);
                if (active)
                {
                    PlanManager.SetPlanViewState(PlanManager.PlanViewState.Time, false);
                    if (updateWorldView)
                    {
                        UpdateWorldViewingTime();
                    }
                }
                else
                    PlanManager.SetPlanViewState(PlanManager.PlanViewState.All, false);
				break;
			case WorldViewMode.Difference:
				viewDifferenceContentBottom.SetActive(active);
				viewDifferenceContentTop.SetActive(active);
				viewDifferenceIndicatorBottom.gameObject.SetActive(active);
				viewDifferenceIndicatorTop.gameObject.SetActive(active);
				if (active)
				{
					if (updateWorldView)
					{
						//TODO
						UpdateWorldViewingDifference();
					}
				}
                else
                {
                    //TODO
                }
                break;
			case WorldViewMode.Normal:
				collapsedDate.gameObject.SetActive(active);
				layoutGroup.spacing = active ? 0 : 20f;
				expandedBackground.gameObject.SetActive(!active);
				if (active && updateWorldView)
				{
					PlanManager.ShowWorldAt(-1);
				}
				break;
		}
	}

	public TimeBarEraMarker CreateEraMarker(int month)
	{
		TimeBarEraMarker marker = (TimeBarEraMarker)Instantiate(eraMarkerPrefab, eraMarkerLocation, false);
		markers.Add(marker);

		// Set position based on month
		float posX = ((month + 120) / (float)Main.MspGlobalData.session_end_month) * eraMarkerLocation.rect.width;
		marker.thisRectTrans.anchoredPosition = new Vector2(posX, marker.thisRectTrans.anchoredPosition.y);

		return marker;
	}

	public void ViewCurrentTime()
	{
		PlanManager.HideCurrentPlan();
	}


	public void UpdateDropdowns()
	{
		//Update options in dropdowns
		int currentMonth = GameState.GetCurrentMonth();
		maxSelectableMonth = currentMonth % 12;
		maxSelectableYear = (currentMonth - maxSelectableMonth) / 12;

		//Set selectable years
		SetYearDropdownOptions(viewTimeYearDropdown, maxSelectableYear, selectedYearView);
		SetYearDropdownOptions(viewDifferenceYearDropdown0, maxSelectableYear, selectedYearDiff0);
		SetYearDropdownOptions(viewDifferenceYearDropdown1, maxSelectableYear, selectedYearDiff1);

		//Set selectable month to all or maxSelectable month depending on selected year
		if (selectedYearView == maxSelectableYear)
			SetMonthDropdownOptions(viewTimeMonthDropdown, maxSelectableMonth, selectedMonthView);
		else
			SetMonthDropdownOptions(viewTimeMonthDropdown, 11, selectedMonthView);

		if (selectedYearDiff0 == maxSelectableYear)
			SetMonthDropdownOptions(viewDifferenceMonthDropdown0, maxSelectableMonth, selectedMonthDiff0);
		else
			SetMonthDropdownOptions(viewDifferenceMonthDropdown0, 11, selectedMonthDiff0);

		if (selectedYearDiff1 == maxSelectableYear)
			SetMonthDropdownOptions(viewDifferenceMonthDropdown1, maxSelectableMonth, selectedMonthDiff1);
		else
			SetMonthDropdownOptions(viewDifferenceMonthDropdown1, 11, selectedMonthDiff1);
	}

	void SetYearDropdownOptions(TMP_Dropdown dropdown, int year, int selectedIndex)
	{
		dropdown.ClearOptions();
		List<string> options = new List<string>();
		for (int i = 0; i <= year; i++)
			options.Add((Main.MspGlobalData.start + i).ToString());
		dropdown.AddOptions(options);
		dropdown.value = selectedIndex;
	}

	void SetMonthDropdownOptions(TMP_Dropdown dropdown, int month, int selectedIndex)
	{
		dropdown.ClearOptions();
		List<string> options = new List<string>();
		for (int i = 0; i <= month; i++)
			options.Add(Util.MonthToMonthText(i));
		dropdown.AddOptions(options);
		dropdown.value = selectedIndex;
	}

	void ViewingMonthDropdownChanged(int newValue)
	{
		selectedMonthView = newValue;
		if (!ignoreActivityCallback)
			UpdateWorldViewingTime();
	}

	void DifferenceMonthDropdownChanged(int newValue, int dropdownID)
	{
		if (dropdownID == 0)
		{
			selectedMonthDiff0 = newValue;
		}
		else
		{
			selectedMonthDiff1 = newValue;
		}
		if (!ignoreActivityCallback)
			UpdateWorldViewingDifference();
	}

	void ViewingYearDropdownChanged(int newValue)
	{
		UpdateYearDropdown(newValue, ref selectedMonthView, ref selectedYearView, viewTimeMonthDropdown, UpdateWorldViewingTime);
	}

	void DifferencedYearDropdownChanged(int newValue, int dropdownID)
	{
		if (dropdownID == 0)
			UpdateYearDropdown(newValue, ref selectedMonthDiff0, ref selectedYearDiff0, viewDifferenceMonthDropdown0, UpdateWorldViewingDifference);
		else
			UpdateYearDropdown(newValue, ref selectedMonthDiff1, ref selectedYearDiff1, viewDifferenceMonthDropdown1, UpdateWorldViewingDifference);
	}

	void UpdateYearDropdown(int newValue, ref int selectedMonth, ref int selectedYear, TMP_Dropdown monthDropdown, Action changeCallback)
	{
		if (newValue == maxSelectableYear)
		{
			//Max selectable year selected, limit month selection
			if (selectedMonth > maxSelectableMonth)
			{
				monthDropdown.value = maxSelectableMonth; //TODO: maybe block callback here?
			}
			SetMonthDropdownOptions(monthDropdown, maxSelectableMonth, selectedMonth);
		}
		else if (selectedYear == maxSelectableYear)
		{
			//Max selectable year deselected, unlimit month selection
			SetMonthDropdownOptions(monthDropdown, 11, selectedMonth);
		}
		selectedYear = newValue;
		if (!ignoreActivityCallback)
			changeCallback();
	}

	void UpdateWorldViewingTime()
	{
		int time = selectedMonthView + selectedYearView * 12;
		UpdateIndicator(viewingTimeIndicatorBottom, time);
		PlanManager.ShowWorldAt(time);
	}

	void UpdateWorldViewingDifference()
	{
		int time0 = selectedMonthDiff0 + selectedYearDiff0 * 12;
		int time1 = selectedMonthDiff1 + selectedYearDiff1 * 12;

		if (time0 == time1)
			Debug.LogError("Trying to view the difference between 2 identiocal times.");
		else if (time0 < time1)
		{
			UpdateIndicator(viewDifferenceIndicatorTop, time0);
			UpdateIndicator(viewDifferenceIndicatorBottom, time1);
			//TODO: set world to state
		}
		else
		{
			UpdateIndicator(viewDifferenceIndicatorTop, time1);
			UpdateIndicator(viewDifferenceIndicatorBottom, time0);
			//TODO: set world to state
		}
	}

	void UpdateIndicator(RectTransform indicator, int month)
	{
		float timePercent = (float)month / (float)Main.MspGlobalData.session_end_month;
		indicator.anchorMin = new Vector2(timePercent, 0);
		indicator.anchorMax = new Vector2(timePercent, 0);
		indicator.anchoredPosition = Vector2.zero;
	}
}