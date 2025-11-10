using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class GenericTimeField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_nameField;
        [SerializeField] RectTransform m_contentContainer;
        [SerializeField] TMP_Dropdown m_monthDropdown;
        [SerializeField] TMP_Dropdown m_yearDropdown;
        [SerializeField] float m_spacePerStep;

        bool m_ignoreCallback;
        Action<int> m_changeCallback;
		int m_selectedTime;

		public int CurrentValue
        {
            get 
            {
                return m_selectedTime;
            }
        } 

		public void Initialise(string a_name, int a_nameSizeSteps, Action<int> a_changeCallback)
        {
            m_nameField.text = a_name;
            RectTransform nameRect = m_nameField.GetComponent<RectTransform>();
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a_nameSizeSteps * m_spacePerStep);
            m_contentContainer.offsetMin = new Vector2((a_nameSizeSteps + 2) * m_spacePerStep, 0f);

			UpdateDropdowns();
			m_monthDropdown.onValueChanged.AddListener(OnMonthDropdownChanged);
			m_yearDropdown.onValueChanged.AddListener(OnYearDropdownChanged);
            m_changeCallback = a_changeCallback;
			TimeManager.Instance.OnCurrentMonthChanged += OnSimulationMonthChanged;
		}

		private void OnDestroy()
		{
			TimeManager.Instance.OnCurrentMonthChanged -= OnSimulationMonthChanged;
		}

		private void OnSimulationMonthChanged(int a_oldMonth, int a_newMonth)
		{
			m_ignoreCallback = true;
			UpdateDropdowns();
			m_ignoreCallback = false;
		}

		public void SetContent(int a_month)
        {
            m_ignoreCallback = true;

			int currentMonth = TimeManager.Instance.GetCurrentMonth();
			int maxSelectableMonth = currentMonth % 12;
			int maxSelectableYear = (currentMonth - maxSelectableMonth) / 12;

			m_selectedTime = a_month;
			UpdateDropdowns();

			m_ignoreCallback = false;
		}

		public void SetInteractable(bool a_interactable)
		{
			m_monthDropdown.interactable = a_interactable;
			m_yearDropdown.interactable = a_interactable;
		}

        void OnMonthDropdownChanged(int a_value)
        {
            if (m_ignoreCallback || m_changeCallback == null)
                return;

			m_selectedTime = m_yearDropdown.value * 12 + m_monthDropdown.value;
			m_changeCallback.Invoke(CurrentValue);

		}

		void OnYearDropdownChanged(int a_value)
		{
			if (m_ignoreCallback || m_changeCallback == null)
				return;

			m_selectedTime = Math.Min(m_yearDropdown.value * 12 + m_monthDropdown.value, TimeManager.Instance.GetCurrentMonth());
			UpdateDropdowns();
			m_changeCallback.Invoke(CurrentValue);
		}

		public void UpdateDropdowns()
		{
			//Update options in dropdowns
			int currentMonth = TimeManager.Instance.GetCurrentMonth();
			if (currentMonth < 0)
				currentMonth = 0;
			int maxSelectableMonth = currentMonth % 12;
			int maxSelectableYear = (currentMonth - maxSelectableMonth) / 12;
			int targetMonth = m_selectedTime % 12;
			int targetYear = (m_selectedTime - targetMonth) / 12;

			//Set selectable years
			SetYearDropdownOptions(maxSelectableYear, targetYear);

			//Set selectable month to all or maxSelectable month depending on selected year
			if (m_yearDropdown.value == maxSelectableYear)
				SetMonthDropdownOptions(maxSelectableMonth, Math.Min(maxSelectableMonth, targetMonth));
			else
				SetMonthDropdownOptions(11, targetMonth);
		}

		void SetYearDropdownOptions(int a_year, int a_selectedIndex)
		{
			m_yearDropdown.ClearOptions();
			List<string> options = new List<string>();
			for (int i = 0; i <= a_year; i++)
				options.Add((SessionManager.Instance.MspGlobalData.start + i).ToString());
			m_yearDropdown.AddOptions(options);
			m_yearDropdown.value = a_selectedIndex;
		}

		void SetMonthDropdownOptions(int a_month, int a_selectedIndex)
		{
			m_monthDropdown.ClearOptions();
			List<string> options = new List<string>();
			for (int i = 0; i <= a_month; i++)
				options.Add(Util.MonthToMonthText(i));
			m_monthDropdown.AddOptions(options);
			m_monthDropdown.value = a_selectedIndex;
		}
	}
}
