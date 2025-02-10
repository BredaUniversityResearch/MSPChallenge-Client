using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class GraphContentSelectFixedCategory : AGraphContentSelect
	{
		public enum KPISource { Ecology, Energy, Shipping, Geometry, Other }
		[SerializeField] protected string[] m_categoryNames;
		[SerializeField] protected KPISource m_kpiSource;
		[SerializeField] protected ContentSelectFocusSelection m_focusSelection;
		[SerializeField] protected GameObject m_singleSelectWindowPrefab;

		HashSet<int> m_selectedCountries;
		HashSet<string> m_selectedIDs;

		List<int> m_AllCountries;
		List<string> m_allIDs;
		List<string> m_displayIDs;
		List<KPICategory> m_categories;
		List<KPIValue> m_values;
		GraphContentSelectWindow[] m_detailsWindows;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);
			m_detailsWindows = new GraphContentSelectWindow[m_contentToggles.Length];

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

			if(m_focusSelection != null)
			{
				string[] focusNames = new string[m_contentToggles.Length];
				for (int i = 0; i < m_contentToggleNames.Length; i++) 
				{
					focusNames[i] = "Per " + m_contentToggleNames[i];
				}
				m_focusSelection.Initialise(focusNames, OnSelectedFocusChanged);
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
							if(m_focusSelection == null)
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
			if(m_focusSelection != null && m_allIDs.Count > 0)
			{
				m_selectedIDs.Add(m_allIDs[0]);
			}

			//Setup toggle values
			if (kvcs.Count > 1)
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

		void OnSelectedFocusChanged(int a_index)
		{ 
			foreach(var toggle in m_contentToggles)
			{
				toggle.m_detailsToggle.isOn = false;
			}
			if(a_index == 0)
			{
				if(m_selectedIDs.Count > 1)
				{
					m_selectedIDs.Clear();
					m_selectedIDs.Add(m_allIDs[0]);
				}
			}
			else if (m_selectedCountries.Count > 1)
			{
				m_selectedCountries.Clear();
				m_selectedCountries.Add(m_AllCountries[0]);
			}
			m_onSettingsChanged.Invoke();
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

			FetchDataInternal(chosenKPIs, data, a_timeSettings, a_stacked, out a_maxValue, out a_minValue);
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

		protected override void DestroyDetailsWindow(int a_index)
		{
			Destroy(m_detailsWindows[a_index].gameObject);
			m_detailsWindows[a_index] = null;
		}
	}
}