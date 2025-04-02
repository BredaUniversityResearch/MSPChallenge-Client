using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEngine.ParticleSystem;

namespace MSP2050.Scripts
{
	public class GraphTimeSelect : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_summaryText;
		[SerializeField] Toggle m_detailsToggle;
		[SerializeField] GameObject m_detailsWindowPrefab;
		[SerializeField] Transform m_detailsWindowParent;

		[SerializeField] bool m_rangeToggleValue;
		[SerializeField] bool m_yearToggleValue;
		[SerializeField] int m_rangeMin;
		[SerializeField] int m_rangeMax;
		[SerializeField] int m_latestAmount;
		[SerializeField] int m_aggregationOption;
		[SerializeField] bool m_showYearToggleSection = true;

		GraphTimeSelectWindow m_windowInstance;
		bool m_ignoreCallback;
		Action m_onSettingsChanged;
		GraphTimeSettings m_currentSettings;
		public GraphTimeSettings CurrentSettings => m_currentSettings;

		public void Initialise(Action a_onSettingsChanged)
		{
			//Called by widget
			TimeManager.Instance.OnCurrentMonthChanged += OnMonthChanged;
			m_detailsToggle.onValueChanged.AddListener(ToggleDetails);
			SetSettingsToDisplay();
			m_onSettingsChanged = a_onSettingsChanged;
		}

		void ToggleDetails(bool a_value)
		{
			if (a_value)
				CreateDetailsWindow();
			else
			{
				Destroy(m_windowInstance.gameObject);
				m_windowInstance = null;
			}
		}

		void CreateDetailsWindow()
		{
			m_windowInstance = Instantiate(m_detailsWindowPrefab, m_detailsWindowParent).GetComponent<GraphTimeSelectWindow>();

			//Set current values
			m_windowInstance.m_aggregationDropdown.ClearOptions();
			m_windowInstance.m_aggregationDropdown.options = new List<TMP_Dropdown.OptionData>() {
				new TMP_Dropdown.OptionData("Yearly Average"),
				new TMP_Dropdown.OptionData("Yearly Minimum"),
				new TMP_Dropdown.OptionData("Yearly Maximum"),
				new TMP_Dropdown.OptionData("Yearly Sum"),
				new TMP_Dropdown.OptionData("Full Year Sum")
			};
			m_windowInstance.m_aggregationDropdown.value = m_aggregationOption;
			m_windowInstance.m_yearToggle.isOn = m_yearToggleValue;
			m_windowInstance.m_aggregationDropdown.gameObject.SetActive(m_yearToggleValue);
			m_windowInstance.m_rangeToggle.isOn = m_rangeToggleValue;
			m_windowInstance.m_latestAmountSection.SetActive(!m_rangeToggleValue);
			m_windowInstance.m_rangeSection.SetActive(m_rangeToggleValue);
			m_windowInstance.m_latestAmountInput.text = m_latestAmount.ToString();
			UpdateSliderRanges();
			m_windowInstance.m_rangeMinSlider.value = m_rangeMin;
			m_windowInstance.m_rangeMaxSlider.value = m_rangeMax;
			UpdateSliderContext();

			//Set callbacks
			m_windowInstance.m_aggregationDropdown.onValueChanged.AddListener(OnAggregationOptionChanged);
			m_windowInstance.m_latestAmountInput.onEndEdit.AddListener(OnLatestAmountInputChanged);
			m_windowInstance.m_rangeMinSlider.onValueChanged.AddListener(OnSliderMinChanged);
			m_windowInstance.m_rangeMaxSlider.onValueChanged.AddListener(OnSliderMaxChanged);
			m_windowInstance.m_rangeToggle.onValueChanged.AddListener(OnRangeToggleChanged);
			m_windowInstance.m_yearToggle.onValueChanged.AddListener(OnYearToggleChanged);

			if(!m_showYearToggleSection)
			{
				m_windowInstance.m_aggregationDropdown.gameObject.SetActive(false);
				m_windowInstance.m_sectionSeparator.gameObject.SetActive(false);
				m_windowInstance.m_yearToggle.gameObject.SetActive(false);
			}
		}

		private void OnDestroy()
		{
			TimeManager.Instance.OnCurrentMonthChanged -= OnMonthChanged;
		}

		void OnLatestAmountInputChanged(string a_value)
		{
			if (m_ignoreCallback)
				return;
			if(int.TryParse(a_value, out int result) && result > 0)
			{
				m_latestAmount = result;
				SetSettingsToDisplay();
				return;
			}
			m_ignoreCallback = true;
			m_latestAmount = 1;
			m_windowInstance.m_latestAmountInput.text = "1";
			m_ignoreCallback = false;
			SetSettingsToDisplay();
		}

		void OnSliderMinChanged(float a_value)
		{
			if (m_ignoreCallback)
				return;

			m_rangeMin = (int)a_value;
			if(m_rangeMax < m_rangeMin)
			{
				m_ignoreCallback = true;
				m_rangeMax = m_rangeMin;
				m_windowInstance.m_rangeMaxSlider.value = m_rangeMax;
				m_ignoreCallback = false;
			}
			UpdateSliderContext();
			SetSettingsToDisplay();
		}

		void OnSliderMaxChanged(float a_value)
		{
			if (m_ignoreCallback)
				return;

			m_rangeMax = (int)a_value;
			if (m_rangeMax < m_rangeMin)
			{
				m_ignoreCallback = true;
				m_rangeMin = m_rangeMax;
				m_windowInstance.m_rangeMinSlider.value = m_rangeMin;
				m_ignoreCallback = false;
			}
			UpdateSliderContext();
			SetSettingsToDisplay();
		}

		void UpdateSliderContext()
		{
			if (m_windowInstance == null)
				return;

			if(m_windowInstance.m_rangeMaxSlider.maxValue < 0.01f)
			{
				m_windowInstance.m_rangeSliderFill.anchorMin = new Vector2(0f, 0f);
				m_windowInstance.m_rangeSliderFill.anchorMax = new Vector2(0f, 1f);
			}
			else
			{
				m_windowInstance.m_rangeSliderFill.anchorMin = new Vector2(m_rangeMin / m_windowInstance.m_rangeMaxSlider.maxValue, 0f);
				m_windowInstance.m_rangeSliderFill.anchorMax = new Vector2(m_rangeMax / m_windowInstance.m_rangeMaxSlider.maxValue, 1f);
			}
			m_windowInstance.m_rangeSliderFill.offsetMin = Vector2.zero;
			m_windowInstance.m_rangeSliderFill.offsetMax = Vector2.zero;
			m_windowInstance.m_rangeMinText.text = m_rangeMin.ToString();
			m_windowInstance.m_rangeMaxText.text = m_rangeMax.ToString();
			if (m_yearToggleValue)
			{
				m_windowInstance.m_rangeMinText.text = Util.MonthToYearText(m_rangeMin * 12);
				m_windowInstance.m_rangeMaxText.text = Util.MonthToYearText(m_rangeMax * 12);
			}
			else
			{
				m_windowInstance.m_rangeMinText.text = Util.MonthToText(m_rangeMin, true);
				m_windowInstance.m_rangeMaxText.text = Util.MonthToText(m_rangeMax, true);
			}
		}

		void OnMonthChanged(int a_oldCurrentMonth, int a_newCurrentMonth)
		{
			UpdateSliderRanges();
			UpdateSliderContext();
			SetSettingsToDisplay();
		}

		void OnAggregationOptionChanged(int a_option)
		{
			if (m_ignoreCallback)
				return;
			m_aggregationOption = a_option;
			SetSettingsToDisplay();
		}

		void OnRangeToggleChanged(bool a_newValue)
		{
			m_windowInstance.m_latestAmountSection.SetActive(!a_newValue);
			m_windowInstance.m_rangeSection.SetActive(a_newValue);
			m_rangeToggleValue = a_newValue;
			if (m_ignoreCallback)
				return;

			SetSettingsToDisplay();
		}

		void OnYearToggleChanged(bool a_newValue)
		{
			m_windowInstance.m_aggregationDropdown.gameObject.SetActive(a_newValue);
			m_yearToggleValue = a_newValue;
			if (m_ignoreCallback)
				return;

			m_ignoreCallback = true;
			int currentTime = TimeManager.Instance.GetCurrentMonth();
			if (a_newValue)
			{
				m_windowInstance.m_rangeMinSlider.maxValue = TimeManager.Instance.GetCurrentMonth() / 12;
				m_windowInstance.m_rangeMaxSlider.maxValue = m_windowInstance.m_rangeMinSlider.maxValue;
				m_rangeMin = m_rangeMin / 12;
				m_rangeMax = m_rangeMax / 12;
				m_windowInstance.m_rangeMinSlider.value = m_rangeMin;
				m_windowInstance.m_rangeMaxSlider.value = m_rangeMax;
			}
			else
			{
				m_windowInstance.m_rangeMinSlider.maxValue = TimeManager.Instance.GetCurrentMonth();
				m_windowInstance.m_rangeMaxSlider.maxValue = m_windowInstance.m_rangeMinSlider.maxValue;
				if (m_rangeMax == TimeManager.Instance.GetCurrentMonth() / 12)
					m_rangeMax = TimeManager.Instance.GetCurrentMonth();
				else 
					m_rangeMax = m_rangeMax * 12;
				m_rangeMin = m_rangeMin * 12;
				m_windowInstance.m_rangeMinSlider.value = m_rangeMin;
				m_windowInstance.m_rangeMaxSlider.value = m_rangeMax;
			}
			m_ignoreCallback = false;

			UpdateSliderContext();
			SetSettingsToDisplay();
		}

		void UpdateSliderRanges()
		{
			m_ignoreCallback = true;
			int max = TimeManager.Instance.GetCurrentMonth();
			if (m_yearToggleValue)
			{
				max /= 12;
			}
			if (m_windowInstance != null)
			{
				m_windowInstance.m_rangeMinSlider.maxValue = max;
				m_windowInstance.m_rangeMaxSlider.maxValue = max;
			}
			m_ignoreCallback = false;
		}

		void UpdateSummaryText()
		{
			if (m_rangeToggleValue)
			{
				if (m_yearToggleValue)
					m_summaryText.text = $"{Util.MonthToYearText(m_rangeMin * 12)} - {Util.MonthToYearText(m_rangeMax * 12)} ({GetAggregationText()})";
				else
					m_summaryText.text = $"{Util.MonthToText(m_rangeMin, true)} - {Util.MonthToText(m_rangeMax, true)}";
			}
			else if (m_yearToggleValue)
				m_summaryText.text = $"Last {m_latestAmount} Years ({GetAggregationText()})";
			else
				m_summaryText.text = $"Last {m_latestAmount} Months";
		}

		string GetAggregationText()
		{
			switch (m_aggregationOption)
			{
				case 1:
					return "Min";
				case 2:
					return "Max";
				case 3:
					return "Sum";
				case 4:
					return "FSum";
				default:
					return "Avg";
			}
		}

		void SetSettingsToDisplay()
		{
			m_currentSettings = new GraphTimeSettings();
			m_currentSettings.m_months = new List<List<int>>();
			m_currentSettings.m_stepNames = new List<string>();
			if (m_yearToggleValue)
			{
				switch(m_aggregationOption)
				{
					case 1:
						m_currentSettings.m_aggregationFunction = AggregateYearsMin;
						break;
					case 2:
						m_currentSettings.m_aggregationFunction = AggregateYearsMax;
						break;
					case 3:
						m_currentSettings.m_aggregationFunction = AggregateYearsSum;
						break;
					case 4:
						m_currentSettings.m_aggregationFunction = AggregateFullYearsSum;
						break;
					default:
						m_currentSettings.m_aggregationFunction = AggregateYearsAvg;
						break;
				}
			}

			int currentMonth = TimeManager.Instance.GetCurrentMonth();
			if(m_rangeToggleValue)
			{
				bool shorten = m_rangeMax - m_rangeMin >= 20;
				if (m_yearToggleValue)
				{
					for (int i = m_rangeMin; i <= m_rangeMax; i++)
					{
						List<int> newSet = new List<int>(12);
						for (int j = 0; j < 12 && j + i*12 <= currentMonth; j++)
						{
							newSet.Add(j + i*12);
						}
						m_currentSettings.m_months.Add(newSet);
						m_currentSettings.m_stepNames.Add(Util.MonthToYearText(i*12, shorten));
					}
				}
				else
				{
					for (int i = m_rangeMin; i <= m_rangeMax; i++)
					{
						m_currentSettings.m_months.Add(new List<int>() { i });

						if (shorten)
							m_currentSettings.m_stepNames.Add(Util.MonthToMonthLetter(i));
						else
							m_currentSettings.m_stepNames.Add(Util.MonthToMonthText(i, true));
					}
				}
			}
			else
			{
				if(m_yearToggleValue)
				{
					int first = Math.Max(0, currentMonth % 12 - 12 * (m_latestAmount - 1));
					bool shorten = (currentMonth - first) / 12 >= 20;
					for (int i = first; i <= currentMonth; i+= 12)
					{
						List<int> newSet = new List<int>(12);
						for(int j = 0; j < 12 && j+i <= currentMonth; j++)
						{
							newSet.Add(j + i);
						}
						m_currentSettings.m_months.Add(newSet);
						m_currentSettings.m_stepNames.Add(Util.MonthToYearText(i, shorten));
					}
				}
				else
				{
					int first = Math.Max(0, currentMonth - m_latestAmount + 1);
					bool shorten = currentMonth - first >= 20;
					for (int i = first; i <= currentMonth; i++)
					{
						m_currentSettings.m_months.Add(new List<int>() { i });
					
						if(shorten)
							m_currentSettings.m_stepNames.Add(Util.MonthToMonthLetter(i));
						else
							m_currentSettings.m_stepNames.Add(Util.MonthToMonthText(i, true));
					}
				}
			}

			UpdateSummaryText();
			m_onSettingsChanged?.Invoke();
		}

		float? AggregateYearsMin(List<float?> a_monthData)
		{
			if(a_monthData.Count == 0)
				return null;

			float result = Mathf.Infinity;
			foreach(float? data in a_monthData)
			{
				if (!data.HasValue)
					continue;
				if(data.Value < result)
					result = data.Value;
			}
			return result == Mathf.Infinity ? null : result;
		}

		float? AggregateYearsMax(List<float?> a_monthData)
		{
			if (a_monthData.Count == 0)
				return null;

			float result = Mathf.NegativeInfinity;
			foreach (float? data in a_monthData)
			{
				if (!data.HasValue)
					continue;
				if (data.Value > result)
					result = data.Value;
			}
			return result == Mathf.NegativeInfinity ? null : result;
		}

		float? AggregateYearsAvg(List<float?> a_monthData)
		{
			if (a_monthData.Count == 0)
				return null;

			float result = 0f;
			int count = 0;
			foreach (float? data in a_monthData)
			{
				if (!data.HasValue)
					continue;
				result += data.Value;
				count++;
			}
			if (count == 0)
				return null;
			return result / count;
		}

		float? AggregateYearsSum(List<float?> a_monthData)
		{
			if (a_monthData.Count == 0)
				return null;

			float result = 0f;
			foreach (float? data in a_monthData)
			{
				if (!data.HasValue)
					continue;
				result += data.Value;
			}
			return result;
		}

		float? AggregateFullYearsSum(List<float?> a_monthData)
		{
			if (a_monthData.Count < 12)
				return null;

			float result = 0f;
			foreach (float? data in a_monthData)
			{
				if (!data.HasValue)
					continue;
				result += data.Value;
			}
			return result;
		}
	}

	public class GraphTimeSettings
	{
		public List<List<int>> m_months;
		public delegate float? AggregationFunction(List<float?> a_monthData);
		public AggregationFunction m_aggregationFunction;
		public List<string> m_stepNames; 
	}
}
