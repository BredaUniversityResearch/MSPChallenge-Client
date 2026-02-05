using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public abstract class AGraphContentSelect : MonoBehaviour
	{
		[SerializeField] protected GraphContentSelectToggle[] m_contentToggles;
		[SerializeField] protected string[] m_contentToggleNames = new string[2] { "Type", "Country" };
		[SerializeField] protected GameObject m_detailsWindowPrefab;
		[SerializeField] protected TextMeshProUGUI m_noDataEntry;

		protected ADashboardWidget m_widget;
		protected Action m_onSettingsChanged;
		protected ValueConversionUnit m_unit;
		protected string m_undefinedUnit;
		//Fixed category, toggles for content
		//Fixed category, selectable country (or: all)
		//2 fixed categories, grouped by content (different name)

		public virtual void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			m_widget = a_widget;
			m_onSettingsChanged = a_onSettingsChanged;
			for (int i = 0; i < m_contentToggles.Length; i++)
			{
				int index = i;
				m_contentToggles[i].m_detailsToggle.onValueChanged.AddListener((b) => ToggleDetails(b, index));
				m_contentToggles[i].m_summaryText.text = m_contentToggleNames[i];
			}
		}

		protected void DetermineUnit(KPICategory a_cat, KPIValue a_val = null)
		{
			ValueConversionCollection vcc = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
			if (a_cat != null && !string.IsNullOrEmpty(a_cat.unit))
			{
				vcc.TryGetConverter(a_cat.unit, out m_unit);
				if (m_unit == null)
					m_undefinedUnit = string.IsNullOrEmpty(a_cat.unit) ? "N/A" : a_cat.unit;
				m_noDataEntry.gameObject.SetActive(false);
			}
			else if(a_val != null && !string.IsNullOrEmpty(a_val.unit))
			{
				vcc.TryGetConverter(a_val.unit, out m_unit);
				if (m_unit == null)
					m_undefinedUnit = string.IsNullOrEmpty(a_val.unit) ? "N/A" : a_val.unit;
				m_noDataEntry.gameObject.SetActive(false);
			}
			else
			{
				m_undefinedUnit = "N/A";
			}
		}

		void ToggleDetails(bool a_value, int a_index)
		{
			if (a_value)
				CreateDetailsWindow(a_index);
			else
				DestroyDetailsWindow(a_index);
		}

		protected abstract void CreateDetailsWindow(int a_index);
		protected abstract void DestroyDetailsWindow(int a_index);
		public abstract GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue);

		protected void FetchDataInternal(List<KPIValue> a_chosenKPIs, GraphDataStepped a_data, GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			a_minValue = 0f;
			a_maxValue = 0f;

			if (a_timeSettings.m_aggregationFunction != null)
			{
				if (a_data.OverLapPatternSet && a_stacked)
				{
					//Aggregated with max per set
					//!!! Assumes no negative values
					for (int i = 0; i < a_timeSettings.m_months.Count; i++)
					{
						a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
						float stackedV = 0f;
						float maxInSet = 0f;
						for (int j = 0; j < a_chosenKPIs.Count; j++)
						{
							List<float?> values = new List<float?>(a_timeSettings.m_months[i].Count);
							foreach (int month in a_timeSettings.m_months[i])
							{
								values.Add(a_chosenKPIs[j].GetKpiValueForMonth(month));
							}
							float? aggregatedV = a_timeSettings.m_aggregationFunction(values);
							a_data.m_steps[i][j] = aggregatedV;
							if (aggregatedV.HasValue)
							{
								maxInSet = Mathf.Max(maxInSet, aggregatedV.Value);
							}
							if (j == a_chosenKPIs.Count - 1 || a_data.GetPatternIndex(j + 1) == 0)
							{
								stackedV += aggregatedV.Value;
								maxInSet = 0f;
							}
						}
						a_maxValue = Mathf.Max(a_maxValue, stackedV);
						a_minValue = Mathf.Min(a_minValue, stackedV);
						
					}
				}
				else
				{
					//Aggregated
					for (int i = 0; i < a_timeSettings.m_months.Count; i++)
					{
						a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
						float stackedPositive = 0f;
						float stackedNegative = 0f;
						for (int j = 0; j < a_chosenKPIs.Count; j++)
						{
							List<float?> values = new List<float?>(a_timeSettings.m_months[i].Count);
							foreach (int month in a_timeSettings.m_months[i])
							{
								values.Add(a_chosenKPIs[j].GetKpiValueForMonth(month));
							}
							float? aggregatedV = a_timeSettings.m_aggregationFunction(values);
							a_data.m_steps[i][j] = aggregatedV;
							if (aggregatedV.HasValue)
							{
								if (!a_stacked)
								{
									a_maxValue = Mathf.Max(a_maxValue, aggregatedV.Value);
									a_minValue = Mathf.Min(a_minValue, aggregatedV.Value);
								}
								if (aggregatedV.Value >= 0)
									stackedPositive += aggregatedV.Value;
								else
									stackedNegative += aggregatedV.Value;
							}
						}
						if (a_stacked)
						{
							a_maxValue = Mathf.Max(a_maxValue, stackedPositive);
							a_minValue = Mathf.Min(a_minValue, stackedNegative);
						}
					}
				}
			}
			else if(a_data.OverLapPatternSet && a_stacked)
			{
				//Non-aggregated, but using max per set
				//!!! Assumes no negative values
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
					float stackedV = 0f;
					float maxInSet = 0f;
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						a_data.m_steps[i][j] = v;
						if (v.HasValue)
						{
							maxInSet = Mathf.Max(maxInSet, v.Value);
						}
						if(j == a_chosenKPIs.Count-1 || a_data.GetPatternIndex(j+1) == 0)
						{
							stackedV += v.Value;
							maxInSet = 0f;
						}
					}
					a_maxValue = Mathf.Max(a_maxValue, stackedV);
					a_minValue = Mathf.Min(a_minValue, stackedV);

				}
			}
			else
			{
				//Get data directly
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
					float stackedPositive = 0f;
					float stackedNegative = 0f;
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						a_data.m_steps[i][j] = v;
						if (v.HasValue)
						{
							if (!a_stacked)
							{
								a_maxValue = Mathf.Max(a_maxValue, v.Value);
								a_minValue = Mathf.Min(a_minValue, v.Value);
							}
							if(v.Value >= 0)
								stackedPositive += v.Value;
							else
								stackedNegative += v.Value;
						}
					}
					if (a_stacked)
					{
						a_maxValue = Mathf.Max(a_maxValue, stackedPositive);
						a_minValue = Mathf.Min(a_minValue, stackedNegative);
					}
				}
			}

			if (Mathf.Abs(a_maxValue - a_minValue) < 0.001f)
			{
				if (Mathf.Abs(a_maxValue) < 0.001f)
					a_maxValue = 1f;
				else
					a_maxValue = a_minValue + 0.001f;
			}
		}

		protected void SetContentToggleNames(string[] a_names)
		{
			for (int i = 0; i < a_names.Length; i++)
			{
				m_contentToggles[i].m_summaryText.text = a_names[i] == null ? m_contentToggleNames[i] : a_names[i];
			}
		}

		protected List<KPIValueCollection> GetKVCs(KPISource a_source)
		{
			switch (a_source)
			{
				case KPISource.Ecology:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.MEL_SIM_NAME);
				case KPISource.Energy:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.CEL_SIM_NAME);
				case KPISource.Shipping:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.SEL_SIM_NAME);
				case KPISource.Geometry:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.Geometry_KPI_NAME);
				case KPISource.MultiUse:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.MultiUse_KPI_NAME);
				case KPISource.SandExtraction:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.SE_SIM_NAME);
				default:
					return SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.OTHER_SIM_NAME);
			}
		}
	}
}