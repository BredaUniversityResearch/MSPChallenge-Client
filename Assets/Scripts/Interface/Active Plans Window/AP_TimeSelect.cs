using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Reactive.Joins;
using System.Text;

namespace MSP2050.Scripts
{
	public class AP_TimeSelect : AP_PopoutWindow
	{
		public enum SelectedTimeUpdateReach { NoWhere = 0, MinYear = 1, Year = 2, MinMonth = 3, Month = 4 }

		[SerializeField] CustomDropdown m_monthDropdown;
		[SerializeField] CustomDropdown m_yearDropdown;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;
		[SerializeField] Toggle m_startPlanToggle;
		[SerializeField] GameObject m_startPlanToggleContainer;
		[SerializeField] TextMeshProUGUI m_constructionText;

		bool m_initialised;
		int m_finishTime; //In game time (0-479)
		int m_minTimeSelectable = 10000;
		int m_finishMonth;//0-11
		int m_minMonthSelectable;
		int m_finishYear; //0-39
		int m_minYearSelectable;
		bool m_ignoreTimeUICallback;
		bool m_dropDownsFilled;

		void Initialise()
		{
			m_initialised = true;

			m_monthDropdown.onValueChanged.AddListener(MonthDropdownChanged);
			m_yearDropdown.onValueChanged.AddListener(YearDropdownChanged);
			m_confirmButton.onClick.AddListener(OnAccept);
			m_cancelButton.onClick.AddListener(TryClose);
			m_startPlanToggle.onValueChanged.AddListener(OnStartingPlanToggled);
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			if (!m_initialised)
				Initialise();
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			UpdateMinTime();
			SetImplementationTime(a_content.StartTime);
			if (SessionManager.Instance.AreWeGameMaster && !TimeManager.Instance.GameStarted)
			{
				m_startPlanToggleContainer.SetActive(true);
				m_startPlanToggle.isOn = a_content.StartTime < 0;
			}
			else
			{
				m_startPlanToggleContainer.SetActive(false);
				m_startPlanToggle.isOn = false;
			}

			if(a_APWindow.InteractionMode == ActivePlanWindow.EInteractionMode.SetupNew)
			{
				m_constructionText.gameObject.SetActive(false);
			}
			else
			{
				m_constructionText.gameObject.SetActive(false);
				int constructionTime = a_content.StartTime - a_content.ConstructionStartTime;
				if (constructionTime == 0)
					m_constructionText.text = "No construction time required";
				else if (constructionTime == 1)
					m_constructionText.text = "After 1 month construction";
				else
					m_constructionText.text = $"After {constructionTime} months construction";
			}
		}

		void OnAccept()
		{
			m_contentToggle.ForceClose(true); //applies content
		}

		public override void ApplyContent()
		{
			if (GetNewPlanStartDate() == m_plan.StartTime || !m_APWindow.Editing)
				return;

			int constructionTime = m_plan.StartTime - m_plan.ConstructionStartTime;
			m_plan.StartTime = GetNewPlanStartDate();
			m_plan.ConstructionStartTime = m_plan.StartTime - constructionTime;
			PlanManager.Instance.UpdatePlanTime(m_plan);
			if (m_plan.State != Plan.PlanState.DELETED)
				foreach (PlanLayer planLayer in m_plan.PlanLayers)
					planLayer.BaseLayer.UpdatePlanLayerTime(planLayer);

			ConstraintManager.Instance.CheckConstraints(m_plan, out var unavailableTypeNames);
			if (unavailableTypeNames.Count > 0)
			{
				StringBuilder sb = new StringBuilder("This plan contains the following entity types that are not yet available at the new implementation time: ");
				for (int i = 0; i < unavailableTypeNames.Count -1; i++)
				{
					sb.Append(unavailableTypeNames[i]);
					sb.Append(", ");
				}
				sb.Append(unavailableTypeNames[unavailableTypeNames.Count - 1]);
				DialogBoxManager.instance.NotificationWindow("Unavailable types", sb.ToString(), null, "Confirm");
			}

			m_APWindow.OnTimeChange();
		}

		public override bool MayClose()
		{
			if (m_finishTime != GetNewPlanStartDate())
			{
				DialogBoxManager.instance.ConfirmationWindow("Discard time change", "Are you sure you want to return to the plan's previous implementation time?", null, m_contentToggle.ForceClose);
				return false;
			}
			return true;
		}

		void OnStartingPlanToggled(bool a_value)
		{
			m_monthDropdown.interactable = !a_value;
			m_yearDropdown.interactable = !a_value;
		}

		/// <summary>
		/// Updates the minimum implementation date and sets a target implementatio date right afterwards.
		/// Avoids double updates that would occur if UpdateMin and SetImplementation time were called seperately.
		/// </summary>
		public void UpdateMinTime()
		{
			if (m_plan != null)
			{
				m_minTimeSelectable = TimeManager.Instance.GetCurrentMonth() + 1 + (m_plan.StartTime - m_plan.ConstructionStartTime);
				if (m_finishTime < m_minTimeSelectable)
					m_finishTime = m_minTimeSelectable;
				UpdateMinSelectableYear(false);
			}
		}

		private void SetImplementationTime(int time)
		{
			if (time < m_minTimeSelectable)
				m_finishTime = m_minTimeSelectable;
			else
				m_finishTime = time;

			if (SetSelectedYear((int)((float)m_finishTime / 12f)) < SelectedTimeUpdateReach.Month)
				SetSelectedMonth(m_finishTime % 12);
		}

		private SelectedTimeUpdateReach SetSelectedYear(int newYear, bool forceUpdated = false)
		{
			m_ignoreTimeUICallback = true;
			m_yearDropdown.value = newYear - m_minYearSelectable;
			m_finishYear = newYear;
			SelectedTimeUpdateReach reach = UpdateMinSelectableMonth(forceUpdated);
			m_ignoreTimeUICallback = false;
			return reach > SelectedTimeUpdateReach.Year ? reach : SelectedTimeUpdateReach.Year;
		}

		private SelectedTimeUpdateReach SetSelectedMonth(int newMonth)
		{
			m_ignoreTimeUICallback = true;
			m_monthDropdown.value = newMonth - m_minMonthSelectable;
			m_finishMonth = newMonth;
			m_ignoreTimeUICallback = false;
			return SelectedTimeUpdateReach.Month;
		}

		/// <summary>
		/// Returns wether the month value was updated.
		/// </summary>
		private SelectedTimeUpdateReach UpdateMinSelectableYear(bool setValues = true)
		{
			int newMinimum = (int)((float)m_minTimeSelectable / 12f);
			if (newMinimum == m_minYearSelectable && m_dropDownsFilled)
				return SelectedTimeUpdateReach.NoWhere;

			//Adds new year options
			m_minYearSelectable = newMinimum;
			m_yearDropdown.ClearOptions();
			List<string> options = new List<string>();
			for (int i = m_minYearSelectable; i < SessionManager.Instance.MspGlobalData.session_num_years; i++)
				options.Add((SessionManager.Instance.MspGlobalData.start + i).ToString());
			m_yearDropdown.AddOptions(options);

			//Checks if the set dropdown value needs to be updated
			if (setValues)
			{
				SelectedTimeUpdateReach reach;
				if (m_finishYear < m_minYearSelectable)
				{
					reach = SetSelectedYear(m_minYearSelectable, true);
				}
				else
					reach = SetSelectedYear(m_finishYear);
				return reach > SelectedTimeUpdateReach.MinYear ? reach : SelectedTimeUpdateReach.MinYear;
			}
			else
				return SelectedTimeUpdateReach.MinYear;
		}

		/// <summary>
		/// Returns wether the month value was updated.
		/// </summary>
		private SelectedTimeUpdateReach UpdateMinSelectableMonth(bool forceUpdated = false)
		{
			int newMinimum = m_finishYear == m_minYearSelectable ? m_minTimeSelectable % 12 : 0;
			if (newMinimum == m_minMonthSelectable && m_dropDownsFilled)
				return SelectedTimeUpdateReach.NoWhere;

			//Adds new month options
			m_minMonthSelectable = newMinimum;
			m_monthDropdown.ClearOptions();
			List<string> options = new List<string>();
			for (int i = m_minMonthSelectable; i < 12; i++)
				options.Add(Util.MonthToMonthText(i));
			m_monthDropdown.AddOptions(options);

			//Checks if the set dropdown value needs to be updated
			SelectedTimeUpdateReach reach;
			if (m_finishMonth < m_minMonthSelectable || forceUpdated)
			{
				reach = SetSelectedMonth(m_minMonthSelectable);
				m_finishTime = m_finishYear * 12 + m_finishMonth;
			}
			else
				reach = SetSelectedMonth(m_finishMonth);
			m_dropDownsFilled = true;
			return reach > SelectedTimeUpdateReach.MinMonth ? reach : SelectedTimeUpdateReach.MinMonth;
		}

		public void YearDropdownChanged(int value)
		{
			if (!m_ignoreTimeUICallback)
			{
				m_finishYear = value + m_minYearSelectable;
				m_finishTime = m_finishYear * 12 + m_finishMonth;
				UpdateMinSelectableMonth();
			}
		}

		public void MonthDropdownChanged(int value)
		{
			if (!m_ignoreTimeUICallback)
			{
				m_finishMonth = value + m_minMonthSelectable;
				m_finishTime = m_finishYear * 12 + m_finishMonth;
			}
		}
		private int GetNewPlanStartDate()
		{
			return m_startPlanToggle.isOn ? -1 : m_finishTime;
		}
	}
}
