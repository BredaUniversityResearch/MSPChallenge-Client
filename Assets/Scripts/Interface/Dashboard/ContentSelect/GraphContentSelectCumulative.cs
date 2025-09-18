using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectCumulative : GraphContentSelectFixedCategory
	{
		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			GraphDataStepped data = new GraphDataStepped();
			data.m_absoluteCategoryIndices = new List<int>();
			data.m_unit = m_unit;
			data.m_undefinedUnit = m_undefinedUnit;
			List<KPIValue> chosenKPIs = new List<KPIValue>();
			int index = 0;
			foreach (KPIValue v in m_values)
			{
				if (m_selectedIDs.Contains(v.name) && (m_selectedCountries == null || m_selectedCountries.Contains(v.targetCountryId)))
				{
					chosenKPIs.Add(v);
					data.m_absoluteCategoryIndices.Add(index);
				}
				index++;
			}

			if (chosenKPIs.Count == 0 && m_values.Count > 0)
			{
				m_noDataEntry.gameObject.SetActive(true);
				m_noDataEntry.text = "NO CONTENT SELECTED";
			}
			else
				m_noDataEntry.gameObject.SetActive(false);

			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);
			if(m_selectedCountries != null)
			{
				data.m_valueCountries = new List<int>(chosenKPIs.Count);
				foreach(KPIValue kpi in chosenKPIs)
				{
					data.m_valueCountries.Add(kpi.targetCountryId);
				}
			}
			data.m_selectedDisplayIDs = new List<string>(m_selectedIDs.Count);
			for(int i = 0; i < m_allIDs.Count; i++)
			{
				if (m_selectedIDs.Contains(m_allIDs[i]))
					data.m_selectedDisplayIDs.Add(m_displayIDs[i]);
			}

			FetchDataCumulative(chosenKPIs, data, a_timeSettings, a_stacked, out a_maxValue, out a_minValue);
			return data;
		}

		protected override void CreateDetailsWindow(int a_index)
		{
			if(m_focusSelection != null && a_index == m_focusSelection.CurrentIndex)
				m_detailsWindows[a_index] = Instantiate(m_singleSelectWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectSingleSelectWindow>();
			else
				m_detailsWindows[a_index] = Instantiate(m_detailsWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectMultiSelectWindow>();
			if(a_index == 0)
			{
				m_detailsWindows[0].SetContent(m_selectedIDs, m_allIDs, m_displayIDs, OnIDToggleChanged, OnAllIDTogglesChanged);
			}
			else
			{
				m_detailsWindows[1].SetContent(m_selectedCountries, m_AllCountries, OnCountryToggleChanged, OnAllCountriesToggleChanged);
			}
		}

		protected void FetchDataCumulative(List<KPIValue> a_chosenKPIs, GraphDataStepped a_data, GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			a_minValue = 0f;
			a_maxValue = float.NegativeInfinity;

			if (a_data.OverLapPatternSet && a_stacked)
			{
				//Aggregated with max per set
				//Not supported
			}

			if (a_timeSettings.m_aggregationFunction != null)
			{
				//Aggregated
				//Get data directly
				float[] cumulV = new float[a_chosenKPIs.Count];

				for (int currentMonth = 0; currentMonth < a_timeSettings.m_months[0][0]; currentMonth++)
				{
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(currentMonth);
						if (v.HasValue)
						{
							cumulV[j] += v.Value;
						}
					}
				}

				//Ignore actual aggregation function, just get yearly max
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
					float stackedV = 0f;

					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						foreach (int month in a_timeSettings.m_months[i])
						{
							float? monthValue = a_chosenKPIs[j].GetKpiValueForMonth(month);
							if (monthValue.HasValue)
								cumulV[j] += monthValue.Value;
						}
						a_data.m_steps[i][j] = cumulV[j];

						if (!a_stacked)
						{
							a_maxValue = Mathf.Max(a_maxValue, cumulV[j]);
							a_minValue = Mathf.Min(a_minValue, cumulV[j]);
						}
						else
						{
							stackedV += cumulV[j];
						}
					}
					if (a_stacked)
					{
						a_maxValue = Mathf.Max(a_maxValue, stackedV);
						a_minValue = Mathf.Min(a_minValue, stackedV);
					}

				}
			}
			else
			{
				//Get data directly
				float[] cumulV = new float[a_chosenKPIs.Count];

				for (int currentMonth = 0; currentMonth < a_timeSettings.m_months[0][0]; currentMonth++)
				{
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(currentMonth);
						if (v.HasValue)
						{
							cumulV[j] += v.Value;
						}
					}
				}

				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
					float stackedV = 0f;
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						if (v.HasValue)
						{
							cumulV[j] += v.Value;
							stackedV += cumulV[j];
						}
						a_data.m_steps[i][j] = cumulV[j];
						if (!a_stacked)
						{
							a_maxValue = Mathf.Max(a_maxValue, cumulV[j]);
							a_minValue = Mathf.Min(a_minValue, cumulV[j]);
						}
					}
					if (a_stacked)
					{
						a_maxValue = Mathf.Max(a_maxValue, stackedV);
						a_minValue = Mathf.Min(a_minValue, stackedV);
					}
				}
			}

			if (a_maxValue == Mathf.NegativeInfinity)
				a_maxValue = 1f;
			if (Mathf.Abs(a_maxValue - a_minValue) < 0.001f)
				a_maxValue = a_minValue + 0.001f;

		}
	}
}