﻿using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MSP2050.Scripts.TimeManager;

namespace MSP2050.Scripts
{
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

		[Header("Layout")]
		[SerializeField] LayoutElement windowLayout;
		[SerializeField] float collapsedHeight;
		[SerializeField] float expandedHeight;

		//Timeline
		[Header("Timeline")]
		public TextMeshProUGUI currentDateText;
		public Image fill;
		public TimeBarEraMarker eraMarkerPrefab;
		public RectTransform eraMarkerParent;
		[HideInInspector] public List<TimeBarEraMarker> eraMarkers = new List<TimeBarEraMarker>();
		[SerializeField] RectTransform viewingTimeIndicatorBottom;

		//View time
		[Header("View Time")]
		[SerializeField] GameObject viewTimeSection;
		[SerializeField] Toggle viewTimeToggle;
		[SerializeField] TMP_Dropdown viewTimeMonthDropdown;
		[SerializeField] TMP_Dropdown viewTimeYearDropdown; 

		//View plan
		[Header("View Plan")]
		[SerializeField] GameObject viewPlanSection;
		[SerializeField] TextMeshProUGUI planViewingText;

		//Planning time
		[Header("Planning State and Time")]
		[SerializeField] CustomButton timeManagerButton;
		[SerializeField] TextMeshProUGUI stateText;
		[SerializeField] TextMeshProUGUI timeText;
		[SerializeField] AddTooltip toolTip;
		PlanningState planningState;

		int selectedMonthView, selectedYearView = 0; 
		int maxSelectableMonth = 0;
		int maxSelectableYear = 0;
		bool ignoreActivityCallback = false;

		public enum WorldViewMode { Normal, Time, Difference, Plan }
		WorldViewMode viewMode = WorldViewMode.Normal;

		public void Start()
		{
			CreateEraMarkers();
			viewTimeToggle.onValueChanged.AddListener((b) =>
			{
				if(b)
					SetViewMode(WorldViewMode.Time, true);
				else
					SetViewMode(WorldViewMode.Normal, true);
			}); 

			viewTimeMonthDropdown.onValueChanged.AddListener(ViewingMonthDropdownChanged);
			viewTimeYearDropdown.onValueChanged.AddListener(ViewingYearDropdownChanged); 
			TimeManager.Instance.OnCurrentMonthChanged += OnMonthChanged;
			timeManagerButton.interactable = SessionManager.Instance.AreWeGameMaster;
			toolTip.enabled = SessionManager.Instance.AreWeGameMaster;
		}
		
		private void OnMonthChanged(int oldCurrentMonth, int newCurrentMonth)
		{
			if (viewMode != WorldViewMode.Normal)
			{
				ignoreActivityCallback = true;
				UpdateDropdowns();
				ignoreActivityCallback = false;
			}
		}

		void CreateEraMarkers()
		{
			for (int i = 0; i < MspGlobalData.num_eras; i++)
			{
				TimeBarEraMarker marker = (TimeBarEraMarker)Instantiate(eraMarkerPrefab, eraMarkerParent, false);
				eraMarkers.Add(marker);
			}
		}

		public void SetDate(int month)
		{
			if (!TimeManager.Instance.GameStarted)
			{
				planViewingText.text = "";
				currentDateText.text = "";
				return;
			}

			fill.fillAmount = (float)month / (float)SessionManager.Instance.MspGlobalData.session_end_month;
			currentDateText.text = Util.MonthToText(month);
			UpdateIndicator(viewingTimeIndicatorBottom, month);

			if (isViewingPlan)
			{
				UpdatePlanViewing();
			}
		}

		public void UpdatePlanViewing()
		{
			if (isViewingPlan)
			{
				planViewingText.text = Util.MonthToText(PlanManager.Instance.planViewing.StartTime, false);
				UpdateIndicator(viewingTimeIndicatorBottom, PlanManager.Instance.planViewing.StartTime);
			}
		}

		public void SetViewMode(WorldViewMode mode, bool updateWorldView)
		{
			if (mode == viewMode || ignoreActivityCallback)
				return;

			bool openingViewMode = viewMode == WorldViewMode.Plan || viewMode == WorldViewMode.Normal && mode == WorldViewMode.Time;

			if (openingViewMode)
			{
				if(Main.InEditMode)
				{
					DialogBoxManager.instance.NotificationWindow("Editing plan content", "View settings are unavailable while editing a plan's content. Please confirm or cancel your changes before trying again.", null);
					ignoreActivityCallback = true;
					viewTimeToggle.isOn = false;
					ignoreActivityCallback = false;
					return;
				}
				if (PlanManager.Instance.planViewing != null)
				{
					ignoreActivityCallback = true;
					PlanManager.Instance.HideCurrentPlan(false);
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
			ignoreActivityCallback = false;
		}

		void SetViewModeElementsActive(WorldViewMode mode, bool active, bool updateWorldView)
		{
			switch(mode)
			{
				case WorldViewMode.Plan:
					viewPlanSection.SetActive(active);
					if (active)
						UpdatePlanViewing();
					break;
				case WorldViewMode.Time:
					viewTimeSection.SetActive(active);
					if (active)
					{
						PlanManager.Instance.SetPlanViewState(PlanManager.PlanViewState.Time, false);
						if (updateWorldView)
						{
							UpdateWorldViewingTime();
						}
					}
					else
						PlanManager.Instance.SetPlanViewState(PlanManager.PlanViewState.All, false);
					break;
				case WorldViewMode.Normal:
					windowLayout.preferredHeight = active ? collapsedHeight : expandedHeight;
					viewingTimeIndicatorBottom.gameObject.SetActive(!active);
					if (active && updateWorldView)
					{
						PlanManager.Instance.ShowWorldAt(-1);
					}
					break;
			}
		}

		public TimeBarEraMarker CreateEraMarker(int month)
		{
			TimeBarEraMarker marker = (TimeBarEraMarker)Instantiate(eraMarkerPrefab, eraMarkerParent, false);
			eraMarkers.Add(marker);

			// Set position based on month
			float posX = ((month + 120) / (float)SessionManager.Instance.MspGlobalData.session_end_month) * eraMarkerParent.rect.width;
			marker.thisRectTrans.anchoredPosition = new Vector2(posX, marker.thisRectTrans.anchoredPosition.y);

			return marker;
		}

		public void ViewCurrentTime()
		{
			PlanManager.Instance.HideCurrentPlan();
		}


		public void UpdateDropdowns()
		{
			//Update options in dropdowns
			int currentMonth = TimeManager.Instance.GetCurrentMonth();
			maxSelectableMonth = currentMonth % 12;
			maxSelectableYear = (currentMonth - maxSelectableMonth) / 12;

			//Set selectable years
			SetYearDropdownOptions(viewTimeYearDropdown, maxSelectableYear, selectedYearView); 

			//Set selectable month to all or maxSelectable month depending on selected year
			if (selectedYearView == maxSelectableYear)
				SetMonthDropdownOptions(viewTimeMonthDropdown, maxSelectableMonth, selectedMonthView);
			else
				SetMonthDropdownOptions(viewTimeMonthDropdown, 11, selectedMonthView); 
		}

		void SetYearDropdownOptions(TMP_Dropdown dropdown, int year, int selectedIndex)
		{
			dropdown.ClearOptions();
			List<string> options = new List<string>();
			for (int i = 0; i <= year; i++)
				options.Add((SessionManager.Instance.MspGlobalData.start + i).ToString());
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
		void ViewingYearDropdownChanged(int newValue)
		{
			UpdateYearDropdown(newValue, ref selectedMonthView, ref selectedYearView, viewTimeMonthDropdown, UpdateWorldViewingTime);
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
			PlanManager.Instance.ShowWorldAt(time);
		} 

		void UpdateIndicator(RectTransform indicator, int month)
		{
			float timePercent = (float)month / (float)SessionManager.Instance.MspGlobalData.session_end_month;
			indicator.anchorMin = new Vector2(timePercent, 0);
			indicator.anchorMax = new Vector2(timePercent, 0);
			indicator.anchoredPosition = Vector2.zero;
		}

		bool isViewingPlan
		{
			get { return viewMode == WorldViewMode.Plan && PlanManager.Instance.planViewing != null; }
		}

		public void SetState(PlanningState a_newState)
		{
			planningState = a_newState;
			switch (a_newState)
			{
				case TimeManager.PlanningState.Setup:
					stateText.text = "Setup";
					break;
				case TimeManager.PlanningState.Play:
					stateText.text = "Planning";
					break;
				case TimeManager.PlanningState.FastForward:
					stateText.text = "Fast Forward";
					break;
				case TimeManager.PlanningState.Simulation:
					stateText.text = "Simulating";
					break;
				case TimeManager.PlanningState.Pause:
					stateText.text = "Paused";
					break;
				case TimeManager.PlanningState.End:
					stateText.text = "End";
					break;
			}

		}

		public void SetCatchingUp(bool a_value)
		{
			if (a_value && planningState == TimeManager.PlanningState.Play)
			{
				stateText.text = "Calculating";
			}
			else
			{
				SetState(planningState);
			}
		}

		public void SetTimeRemaining(TimeSpan a_newTime)
		{

			if (a_newTime.Ticks > TimeSpan.TicksPerDay)
			{
				timeText.text = string.Format("{0:D1}:{1:D2}:{2:D2}:{3:D2}", a_newTime.Days, a_newTime.Hours, a_newTime.Minutes, a_newTime.Seconds);
			}
			else if (a_newTime.Ticks < TimeSpan.TicksPerHour)
			{
				timeText.text = string.Format("{0:D1}:{1:D2}", a_newTime.Minutes, a_newTime.Seconds);
			}
			else if (a_newTime.Ticks < TimeSpan.TicksPerDay)
			{
				timeText.text = string.Format("{0:D1}:{1:D2}:{2:D2}", a_newTime.Hours, a_newTime.Minutes, a_newTime.Seconds);
			}
		}
	}
}