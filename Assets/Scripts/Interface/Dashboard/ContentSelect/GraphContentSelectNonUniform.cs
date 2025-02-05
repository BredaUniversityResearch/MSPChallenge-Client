using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectNonUniform : AGraphContentSelect
	{
		[SerializeField] protected string[] m_categoryNames;
		[SerializeField] protected GraphContentSelectFixedCategory.KPISource m_kpiSource;

		HashSet<int> m_selectedCountries;
		HashSet<KPIValue> m_selectedValues;

		Dictionary<int, List<KPIValue>> m_valuesPerCountry;
		List<int> m_allCountries;
		List<KPIValue> m_currentValueOptions;
		List<KPICategory> m_categories;
		GraphContentSelectMultiSelectWindow[] m_detailsWindows;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);
			m_detailsWindows = new GraphContentSelectMultiSelectWindow[m_contentToggles.Length];

			List<KPIValueCollection> kvcs = null;
			switch(m_kpiSource)
			{
				case GraphContentSelectFixedCategory.KPISource.Ecology:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.MEL_SIM_NAME); 
					break;
				case GraphContentSelectFixedCategory.KPISource.Energy:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.CEL_SIM_NAME);
					break;
				case GraphContentSelectFixedCategory.KPISource.Shipping:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(SimulationManager.SEL_SIM_NAME);
					break;
				case GraphContentSelectFixedCategory.KPISource.Geometry:
					kvcs = SimulationManager.Instance.GetKPIValuesForAllCountriesSimulation(null);
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

			//Fetch values and their names
			m_categories = new List<KPICategory>();
			m_currentValueOptions = new List<KPIValue>();
			m_allCountries = new List<int>();
			m_selectedValues = new HashSet<KPIValue>();
			m_valuesPerCountry = new Dictionary<int, List<KPIValue>>();

			foreach (KPIValueCollection valueColl in kvcs)
			{
				foreach (string s in m_categoryNames)
				{
					KPICategory cat = valueColl.FindCategoryByName(s);
					if (cat == null)
						continue;
					m_categories.Add(cat);
					cat.OnValueUpdated += OnKPIChanged;
					foreach (var value in cat.GetChildValues())
					{
						m_currentValueOptions.Add(value);
						m_selectedValues.Add(value);
						if (m_valuesPerCountry.TryGetValue(cat.targetCountryId, out var list))
						{
							list.Add(value);
						}
						else
						{ 
							m_valuesPerCountry.Add(cat.targetCountryId, new List<KPIValue>() { value });
						}
					}			
				}
			}
			if (m_categories.Count == 0)
			{
				m_noDataEntry.gameObject.SetActive(m_categories.Count == 0);
				m_noDataEntry.text = "NO DATA AVAILABLE";
			}

			//Setup toggle values
			if(kvcs.Count > 1)
			{
				m_selectedCountries = new HashSet<int>();
				foreach(int country in m_valuesPerCountry.Keys)
				{
					m_selectedCountries.Add(country);
					m_allCountries.Add(country);
				}
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
			m_onSettingsChanged.Invoke();
		}

		void OnIDToggleChanged(int a_index, bool a_value)
		{
			if (a_value)
				m_selectedValues.Add(m_currentValueOptions[a_index]);
			else
				m_selectedValues.Remove(m_currentValueOptions[a_index]);
			m_onSettingsChanged.Invoke();
		}

		void OnAllIDTogglesChanged(bool a_value)
		{
			if (a_value)
			{
				m_selectedValues = new HashSet<KPIValue>();
				for (int i = 0; i < m_currentValueOptions.Count; i++)
				{
					m_selectedValues.Add(m_currentValueOptions[i]);
				}
			}
			else
				m_selectedValues = new HashSet<KPIValue>();
			m_onSettingsChanged.Invoke();
		}

		void OnCountryToggleChanged(int a_index, bool a_value)
		{
			if (a_value)
			{
				m_selectedCountries.Add(m_allCountries[a_index]);
				foreach(KPIValue kpi in m_valuesPerCountry[m_allCountries[a_index]])
				{
					m_selectedValues.Add(kpi);
					m_currentValueOptions.Add(kpi);
				}
			}
			else
			{
				m_selectedCountries.Remove(m_allCountries[a_index]);
				foreach (KPIValue kpi in m_valuesPerCountry[m_allCountries[a_index]])
				{
					m_selectedValues.Remove(kpi);
					m_currentValueOptions.Remove(kpi);
				}
			}
			RefreshValueSelectIfOpen();
			m_onSettingsChanged.Invoke();
		}

		void OnAllCountriesToggleChanged(bool a_value)
		{
			if (a_value)
			{
				m_selectedCountries = new HashSet<int>();
				for (int i = 0; i < m_allCountries.Count; i++)
				{
					if(!m_selectedCountries.Contains(m_allCountries[i]))
					{
						foreach (KPIValue kpi in m_valuesPerCountry[m_allCountries[i]])
						{
							m_selectedValues.Add(kpi);
							m_currentValueOptions.Add(kpi);
						}
					}
					m_selectedCountries.Add(m_allCountries[i]);
				}

			}
			else
			{
				m_selectedCountries = new HashSet<int>();
				m_selectedValues = new HashSet<KPIValue>();
				m_currentValueOptions = new List<KPIValue>();
			}
			RefreshValueSelectIfOpen();
			m_onSettingsChanged.Invoke();
		}

		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			GraphDataStepped data = new GraphDataStepped();
			data.m_absoluteCategoryIndices = new List<int>();
			List<KPIValue> chosenKPIs = new List<KPIValue>();
			int index = 0;
			data.m_selectedDisplayIDs = new List<string>(m_selectedValues.Count);
			foreach (KPIValue v in m_selectedValues)
			{
				chosenKPIs.Add(v);
				data.m_absoluteCategoryIndices.Add(index);
				data.m_selectedDisplayIDs.Add(v.displayName);
				index++;
			}

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
				if(m_allCountries.Count > 0)
				{
					m_noDataEntry.gameObject.SetActive(true);
					m_noDataEntry.text = "NO CONTENT SELECTED";
				}
			}

			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);
			if (m_selectedCountries != null)
			{
				data.m_valueCountries = new List<int>(chosenKPIs.Count);
				foreach (KPIValue kpi in chosenKPIs)
				{
					data.m_valueCountries.Add(kpi.targetCountryId);
				}
			}

			FetchDataInternal(chosenKPIs, data, a_timeSettings, a_stacked, out a_maxValue, out a_minValue);
			return data;
		}

		protected override void CreateDetailsWindow(int a_index)
		{
			m_detailsWindows[a_index] = Instantiate(m_detailsWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectMultiSelectWindow>();
			if(a_index == 0)
			{
				m_detailsWindows[0].SetContent(m_selectedValues, m_currentValueOptions, OnIDToggleChanged, OnAllIDTogglesChanged);
			}
			else
			{
				m_detailsWindows[1].SetContent(m_selectedCountries, m_allCountries, OnCountryToggleChanged, OnAllCountriesToggleChanged);
			}
		}

		protected override void DestroyDetailsWindow(int a_index)
		{
			Destroy(m_detailsWindows[a_index].gameObject);
			m_detailsWindows[a_index] = null;
		}

		void RefreshValueSelectIfOpen()
		{
			if(m_detailsWindows[0] != null)
			{
				m_detailsWindows[0].SetContent(m_selectedValues, m_currentValueOptions, OnIDToggleChanged, OnAllIDTogglesChanged);
			}
		}
	}
}