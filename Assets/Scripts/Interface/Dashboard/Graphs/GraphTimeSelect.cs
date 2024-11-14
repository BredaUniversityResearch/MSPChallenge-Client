using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class GraphTimeSelect : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_summaryText;
		[SerializeField] Button m_detailsButton;
		[SerializeField] GameObject m_detailsWindow;

		//Year/Month section
		[SerializeField] Toggle m_yearToggle; //per month / per year
		[SerializeField] CustomDropdown m_aggregationDropdown;

		//Range section
		[SerializeField] Toggle m_rangeToggle; // Latest X / range
		[SerializeField] CustomInputField m_latestAmountInput; 
		[SerializeField] GameObject m_rangeSection;
		[SerializeField] Slider m_rangeMinSlider;
		[SerializeField] Slider m_rangeMaxSlider;
		[SerializeField] TextMeshProUGUI m_rangeMinText;
		[SerializeField] TextMeshProUGUI m_rangeMaxText;

		bool m_ignoreCallback;
		Action m_onSettingsChanged;
		GraphTimeSettings m_currentSettings;
		public GraphTimeSettings CurrentSettings => m_currentSettings;

		public void Initialise(Action a_onSettingsChanged)
		{
			//Called by widget
			m_onSettingsChanged = a_onSettingsChanged;
			TimeManager.Instance.OnCurrentMonthChanged += OnMonthChanged;

			m_aggregationDropdown.ClearOptions();
			m_aggregationDropdown.options = new List<TMP_Dropdown.OptionData>() {
				new TMP_Dropdown.OptionData("Average"),
				new TMP_Dropdown.OptionData("Minimum"),
				new TMP_Dropdown.OptionData("Maximum")};
			m_aggregationDropdown.value = 0;
		}

		private void OnDestroy()
		{
			TimeManager.Instance.OnCurrentMonthChanged -= OnMonthChanged;
		}

		void OnMonthChanged(int a_oldCurrentMonth, int a_newCurrentMonth)
		{
			if (!m_rangeToggle.isOn)
				SetSettingsToDisplay();
			else if (m_detailsWindow.activeSelf)
			{ 
				//TODO: update settings and display date when showing latest
			}
		}

		void SetSettingsToDisplay()
		{
			m_currentSettings = new GraphTimeSettings();
			m_currentSettings.m_months = new List<List<int>>();
			if (m_yearToggle.isOn)
			{
				switch(m_aggregationDropdown.value)
				{
					case 1:
						m_currentSettings.m_aggregationFunction = AggregateYearsMin;
						break;
					case 2:
						m_currentSettings.m_aggregationFunction = AggregateYearsMax;
						break;
					default:
						m_currentSettings.m_aggregationFunction = AggregateYearsAvg;
						break;
				}
			}

			int currentMonth = TimeManager.Instance.GetCurrentMonth();
			if(m_rangeToggle.isOn)
			{
				int min = (int)m_rangeMinSlider.value;
				int max = (int)m_rangeMaxSlider.value;
				if (m_yearToggle.isOn)
				{
					for (int i = min; i <= max && i <= currentMonth; i+=12)
					{
						List<int> newSet = new List<int>(12);
						for (int j = 0; j < 12 && j + i <= currentMonth && j + i <= max; j++)
						{
							newSet.Add(j + i);
						}
						m_currentSettings.m_months.Add(newSet);
					}
				}
				else
				{
					for (int i = min; i <= max; i++)
					{
						m_currentSettings.m_months.Add(new List<int>() { i });
					}
				}
			}
			else
			{
				int latestX = int.Parse(m_latestAmountInput.text);
				if(m_yearToggle.isOn)
				{
					int first = Math.Max(0, currentMonth % 12 - 12 * (latestX - 1));
					for(int i = first; i <= currentMonth; i+= 12)
					{
						List<int> newSet = new List<int>(12);
						for(int j = 0; j < 12 && j+i <= currentMonth; j++)
						{
							newSet.Add(j + i);
						}
						m_currentSettings.m_months.Add(newSet);
					}
				}
				else
				{
					int first = Math.Max(0, currentMonth - latestX - 1);
					for (int i = first; i <= currentMonth; i++)
					{
						m_currentSettings.m_months.Add(new List<int>() { i });
					}
				}
			}
			m_onSettingsChanged?.Invoke();
		}

		float AggregateYearsMin(List<float> a_monthData)
		{
			if(a_monthData.Count == 0)
				return 0f;

			float result = Mathf.Infinity;
			foreach(float data in a_monthData)
				if(data < result)
					result = data;
			return result;
		}

		float AggregateYearsMax(List<float> a_monthData)
		{
			if (a_monthData.Count == 0)
				return 0f;

			float result = Mathf.NegativeInfinity;
			foreach (float data in a_monthData)
				if (data > result)
					result = data;
			return result;
		}

		float AggregateYearsAvg(List<float> a_monthData)
		{
			if (a_monthData.Count == 0)
				return 0f;

			float result = 0f;
			foreach (float data in a_monthData)
				result += data;
			return result / a_monthData.Count;
		}
	}

	public class GraphTimeSettings
	{
		public List<List<int>> m_months;
		public delegate float AggregationFunction(List<float> a_monthData);
		public AggregationFunction m_aggregationFunction;
	}
}
