﻿using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectFixedCategory : AGraphContentSelect
	{
		public enum KPISource { Ecology, Energy, Shipping, Geometry, Other }
		[SerializeField] protected string[] m_categoryNames;
		[SerializeField] protected KPISource m_kpiSource;

		HashSet<int> m_selectedCountries;
		HashSet<string> m_selectedIDs;

		List<int> m_AllCountries;
		List<string> m_allIDs;
		List<string> m_displayIDs;
		List<KPICategory> m_categories;
		List<KPIValue> m_values;
		GraphContentSelectFixedCategoryWindow[] m_detailsWindows;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);
			m_detailsWindows = new GraphContentSelectFixedCategoryWindow[m_contentToggles.Length];

			List<KPIValueCollection> kvcs = null;
			switch(m_kpiSource)
			{
				case KPISource.Ecology:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.MEL_SIM_NAME); 
					break;
				case KPISource.Energy:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.CEL_SIM_NAME);
					break;
				case KPISource.Shipping:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.SEL_SIM_NAME);
					break;
				case KPISource.Geometry:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(null);
					break;
				default:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.OTHER_SIM_NAME);
					break;
			}
			
			if(kvcs == null || kvcs.Count == 0)
			{
				m_noDataEntry.gameObject.SetActive(m_categories.Count == 0);
				m_noDataEntry.text = "NO DATA AVAILABLE";
				for(int i = 0; i < m_contentToggles.Length; i++)
				{
					m_contentToggles[i].gameObject.SetActive(false);
				}
				return;
			}
			else if (kvcs.Count == 1)
			{
				m_contentToggles[1].gameObject.SetActive(false);
			}

			//Fetch values and their names
			m_categories = new List<KPICategory>();
			m_values = new List<KPIValue>();
			m_displayIDs = new List<string>();
			m_allIDs = new List<string>();
			m_selectedIDs = new HashSet<string>();
			int valueIndex = 0;
			foreach (KPIValueCollection valueColl in kvcs)
			{
				foreach (string s in m_categoryNames)
				{
					KPICategory cat = valueColl.FindCategoryByName(s);
					if (cat == null)
						continue;
					m_categories.Add(cat);
					cat.OnValueUpdated += OnKPIChanged;
					m_values.AddRange(cat.GetChildValues());
					if(valueIndex == 0)
					{ 
						foreach(var value in cat.GetChildValues())
						{
							m_displayIDs.Add(value.displayName);
							m_allIDs.Add(value.name);
							m_selectedIDs.Add(value.name);
						}
					}
				}
				valueIndex++;
			}
			if (m_categories.Count == 0)
			{
				m_noDataEntry.gameObject.SetActive(m_categories.Count == 0);
				m_noDataEntry.text = "NO DATA AVAILABLE";
			}

			//Setup toggle values
			if(kvcs.Count > 1)
			{
				m_AllCountries = new List<int>();
				foreach(Team team in SessionManager.Instance.GetTeams())
				{
					if (!team.IsManager)
						m_AllCountries.Add(team.ID);
				}
				m_selectedCountries = new HashSet<int>();
				foreach(int country in m_AllCountries)
				{
					m_selectedCountries.Add(country);
				}
				//TODO: add all country option if relevant?
			}
		}

		private void OnDestroy()
		{
			foreach(KPICategory cat in m_categories)
			{
				cat.OnValueUpdated -= OnKPIChanged;
			}
		}

		void OnKPIChanged(KPIValue a_newValue)
		{
			m_widget.UpdateData();
		}

		void OnIDToggleChanged(int a_index, bool a_value)
		{
			if (a_value)
				m_selectedIDs.Add(m_allIDs[a_index]);
			else
				m_selectedIDs.Remove(m_allIDs[a_index]);
			m_onSettingsChanged.Invoke();
		}

		void OnAllIDTogglesChanged(bool a_value)
		{
			if (a_value)
			{
				m_selectedIDs = new HashSet<string>();
				for (int i = 0; i < m_allIDs.Count; i++)
				{
					m_selectedIDs.Add(m_allIDs[i]);
				}
			}
			else
				m_selectedIDs = new HashSet<string>();
			m_onSettingsChanged.Invoke();
		}

		void OnCountryToggleChanged(int a_index, bool a_value)
		{
			if (a_value)
				m_selectedCountries.Add(m_AllCountries[a_index]);
			else
				m_selectedCountries.Remove(m_AllCountries[a_index]);
			m_onSettingsChanged.Invoke();
		}

		void OnAllCountriesToggleChanged(bool a_value)
		{
			if (a_value)
			{
				m_selectedCountries = new HashSet<int>();
				for (int i = 0; i < m_AllCountries.Count; i++)
				{
					m_selectedCountries.Add(m_AllCountries[i]);
				}
			}
			else
				m_selectedCountries = new HashSet<int>();
			m_onSettingsChanged.Invoke();
		}

		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			GraphDataStepped data = new GraphDataStepped();
			data.m_absoluteCategoryIndices = new List<int>();
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
			

			a_minValue = 0f;
			a_maxValue = float.NegativeInfinity;

			ValueConversionCollection vcc = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
			if (chosenKPIs.Count > 0)
			{
				vcc.TryGetConverter(chosenKPIs[0].unit, out data.m_unit);
				if (data.m_unit == null)
					data.m_undefinedUnit = string.IsNullOrEmpty(chosenKPIs[0].unit) ? "N/A" : chosenKPIs[0].unit;
				m_noDataEntry.gameObject.SetActive(false);
			}
			else
			{
				vcc.TryGetConverter("", out data.m_unit);
				if(m_values.Count > 0)
				{
					m_noDataEntry.gameObject.SetActive(true);
					m_noDataEntry.text = "NO CONTENT SELECTED";
				}
			}

			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);
			if(m_selectedCountries != null)
			{
				data.m_selectedCountries = new List<int>(m_selectedCountries.Count);
				foreach(int country in m_AllCountries)
				{
					if (m_selectedCountries.Contains(country))
						data.m_selectedCountries.Add(country);
				}
			}
			data.m_selectedDisplayIDs = new List<string>(m_selectedIDs.Count);
			for(int i = 0; i < m_allIDs.Count; i++)
			{
				if (m_selectedIDs.Contains(m_allIDs[i]))
					data.m_selectedDisplayIDs.Add(m_displayIDs[i]);
			}

			if(a_timeSettings.m_aggregationFunction != null)
			{
				//get sets, then aggregate
				for(int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					data.m_steps.Add(new float?[chosenKPIs.Count]);
					float stackedV = 0f;
					for (int j = 0; j < chosenKPIs.Count; j++)
					{
						List<float?> values = new List<float?>(a_timeSettings.m_months[i].Count);
						foreach (int month in a_timeSettings.m_months[i])
						{
							values.Add(chosenKPIs[j].GetKpiValueForMonth(month));
						}
						float? aggregatedV = a_timeSettings.m_aggregationFunction(values);
						data.m_steps[i][j] = aggregatedV;
						if (aggregatedV.HasValue)
						{
							if (!a_stacked)
							{
								if (aggregatedV.Value > a_maxValue)
									a_maxValue = aggregatedV.Value;
								if (aggregatedV.Value < a_minValue)
									a_minValue = aggregatedV.Value;
							}
							stackedV += aggregatedV.Value;
						}
					}
					if(a_stacked)
					{
						if (stackedV > a_maxValue)
							a_maxValue = stackedV;
						if (stackedV < a_minValue)
							a_minValue = stackedV;
					}
				}
			}
			else
			{
				//get data directly
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					data.m_steps.Add(new float?[chosenKPIs.Count]);
					float stackedV = 0f;
					for (int j = 0; j < chosenKPIs.Count; j++)
					{
						float? v = chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						data.m_steps[i][j] = v;
						if (v.HasValue)
						{
							if (!a_stacked)
							{
								if (v.Value > a_maxValue)
									a_maxValue = v.Value;
								if (v.Value < a_minValue)
									a_minValue = v.Value;
							}
							stackedV += v.Value;
						}
					}
					if (a_stacked)
					{
						if (stackedV > a_maxValue)
							a_maxValue = stackedV;
						if (stackedV < a_minValue)
							a_minValue = stackedV;
					}
				}
			}

			if (a_maxValue == Mathf.NegativeInfinity)
				a_maxValue = 1f;
			if (Mathf.Abs(a_maxValue - a_minValue) < 0.001f)
				a_maxValue = a_minValue + 0.001f;
			return data;
		}

		protected override void CreateDetailsWindow(int a_index)
		{
			m_detailsWindows[a_index] = Instantiate(m_detailsWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectFixedCategoryWindow>();
			if(a_index == 0)
			{
				m_detailsWindows[0].SetContent(m_selectedIDs, m_allIDs, m_displayIDs, OnIDToggleChanged, OnAllIDTogglesChanged);
			}
			else
			{
				m_detailsWindows[1].SetContent(m_selectedCountries, m_AllCountries, OnCountryToggleChanged, OnAllCountriesToggleChanged);
			}
		}

		protected override void DestroyDetailsWindow(int a_index)
		{
			Destroy(m_detailsWindows[a_index].gameObject);
			m_detailsWindows[a_index] = null;
		}
	}
}