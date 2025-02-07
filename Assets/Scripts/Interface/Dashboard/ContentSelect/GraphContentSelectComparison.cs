using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectComparison : AGraphContentSelect
	{
		public enum KPISource { Ecology, Energy, Shipping, Geometry, Other }
		[SerializeField] protected string[] m_categoryNames;
		[SerializeField] protected string[] m_categoryDisplayNames;
		[SerializeField] protected int[] m_kpiNameCutLength;
		[SerializeField] protected KPISource m_kpiSource;
		[SerializeField] bool m_overLapPatternSet;

		HashSet<int> m_selectedCountries;
		HashSet<string> m_selectedIDs;

		List<int> m_allCountries;
		List<string> m_allIDs;
		List<string> m_displayIDs;
		List<KPICategory> m_categories;
		Dictionary<string, List<KPIValue>> m_valuesPerId;

		GraphContentSelectMultiSelectWindow[] m_detailsWindows;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);
			m_detailsWindows = new GraphContentSelectMultiSelectWindow[m_contentToggles.Length];

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
			m_valuesPerId = new Dictionary<string, List<KPIValue>>();
			m_displayIDs = new List<string>();
			m_allIDs = new List<string>();
			m_selectedIDs = new HashSet<string>();
			foreach (KPIValueCollection valueColl in kvcs)
			{
				for(int i = 0; i < m_categoryNames.Length; i++)
				{
					KPICategory cat = valueColl.FindCategoryByName(m_categoryNames[i]);
					if (cat == null)
						continue;
					m_categories.Add(cat);
					cat.OnValueUpdated += OnKPIChanged;
					foreach (var value in cat.GetChildValues())
					{
						string cutName = value.name.Substring(0, value.name.Length - m_kpiNameCutLength[i]);
						if (m_valuesPerId.TryGetValue(cutName, out var list))
						{ 
							list.Add(value);
						}
						else
						{
							m_valuesPerId.Add(cutName, new List<KPIValue>() { value });
							m_selectedIDs.Add(cutName);
							m_allIDs.Add(cutName);
							m_displayIDs.Add(value.displayName); //TODO: also cut displaynames?
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
				m_allCountries = new List<int>();
				foreach(Team team in SessionManager.Instance.GetTeams())
				{
					if (!team.IsManager)
						m_allCountries.Add(team.ID);
				}
				m_selectedCountries = new HashSet<int>();
				foreach(int country in m_allCountries)
				{
					m_selectedCountries.Add(country);
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
				m_selectedCountries.Add(m_allCountries[a_index]);
			else
				m_selectedCountries.Remove(m_allCountries[a_index]);
			m_onSettingsChanged.Invoke();
		}

		void OnAllCountriesToggleChanged(bool a_value)
		{
			if (a_value)
			{
				m_selectedCountries = new HashSet<int>();
				for (int i = 0; i < m_allCountries.Count; i++)
				{
					m_selectedCountries.Add(m_allCountries[i]);
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
			data.m_overLapPatternSet = m_overLapPatternSet;
			data.m_patternIndices = new List<int>();
			data.m_selectedDisplayIDs = new List<string>(m_selectedIDs.Count);
			List<KPIValue> chosenKPIs = new List<KPIValue>();
			int index = 0;
			foreach (var kvp in m_valuesPerId)
			{
				if (m_selectedIDs.Contains(kvp.Key))
				{
					for (int i = 0; i < kvp.Value.Count; i++)
					{
						if (m_selectedCountries == null || m_selectedCountries.Contains(kvp.Value[i].targetCountryId))
						{
							chosenKPIs.Add(kvp.Value[i]);
							data.m_patternIndices.Add(i);
							data.m_selectedDisplayIDs.Add(kvp.Value[i].displayName);
							data.m_absoluteCategoryIndices.Add(index);
						}
						index++;

					}
				}
				else
					index += kvp.Value.Count;
			}

			data.m_patternSetsPerStep = m_selectedIDs.Count;
			data.m_patternNames = new List<string>();
			foreach(string s in m_categoryDisplayNames)
				data.m_patternNames.Add(s);

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
				if(m_valuesPerId.Count > 0)
				{
					m_noDataEntry.gameObject.SetActive(true);
					m_noDataEntry.text = "NO CONTENT SELECTED";
				}
			}

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

			FetchDataInternal(chosenKPIs, data, a_timeSettings, a_stacked, out a_maxValue, out a_minValue);
			return data;
		}

		protected override void CreateDetailsWindow(int a_index)
		{
			m_detailsWindows[a_index] = Instantiate(m_detailsWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectMultiSelectWindow>();
			if(a_index == 0)
			{
				m_detailsWindows[0].SetContent(m_selectedIDs, m_allIDs, m_displayIDs, OnIDToggleChanged, OnAllIDTogglesChanged);
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
	}
}