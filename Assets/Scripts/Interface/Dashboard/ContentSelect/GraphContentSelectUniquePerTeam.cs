using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectUniquePerTeam : AGraphContentSelect
	{
		[SerializeField] protected string[] m_categoryNames;
		[SerializeField] protected KPISource m_kpiSource;
		[SerializeField] protected GameObject m_singleSelectWindowPrefab;

		int m_selectedCountry;
		HashSet<KPIValue> m_selectedValues;

		Dictionary<int, List<KPIValue>> m_valuesPerCountry;
		List<int> m_allCountries;
		List<KPIValue> m_currentValueOptions;
		List<KPICategory> m_categories;
		GraphContentSelectWindow[] m_detailsWindows;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);
			m_detailsWindows = new GraphContentSelectWindow[m_contentToggles.Length];
			List<KPIValueCollection> kvcs = GetKVCs(m_kpiSource);

			if (kvcs == null || kvcs.Count == 0)
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
					if (m_valuesPerCountry.TryGetValue(cat.targetCountryId, out var list))
					{
						list.AddRange(cat.GetChildValues());
					}
					else
					{
						m_valuesPerCountry.Add(cat.targetCountryId, cat.GetChildValues());
					}

				}
			}
			if (m_categories.Count == 0)
			{
				m_noDataEntry.gameObject.SetActive(m_categories.Count == 0);
				m_noDataEntry.text = "NO DATA AVAILABLE";
				DetermineUnit(null);
			}
			else
			{
				DetermineUnit(m_categories[0]);
			}

			//Setup toggle values
			if (kvcs.Count > 1)
			{
				m_selectedCountry = m_valuesPerCountry.First().Key;
				foreach (int country in m_valuesPerCountry.Keys)
				{
					m_allCountries.Add(country);
				}
				foreach (KPIValue value in m_valuesPerCountry[m_allCountries[0]])
				{
					m_currentValueOptions.Add(value);
					m_selectedValues.Add(value);
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
				m_selectedCountry = a_index;
				m_currentValueOptions.Clear();
				m_selectedValues.Clear();
				foreach (KPIValue kpi in m_valuesPerCountry[m_allCountries[a_index]])
				{
					m_selectedValues.Add(kpi);
					m_currentValueOptions.Add(kpi);
				}

				RefreshValueSelectIfOpen();
				m_onSettingsChanged.Invoke();
			}
		}

		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			GraphDataStepped data = new GraphDataStepped();
			data.m_absoluteCategoryIndices = new List<int>();
			data.m_unit = m_unit;
			data.m_undefinedUnit = m_undefinedUnit;

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

			if (chosenKPIs.Count == 0 && m_allCountries.Count > 0)
			{
				m_noDataEntry.gameObject.SetActive(true);
				m_noDataEntry.text = "NO CONTENT SELECTED";
			}
			else
				m_noDataEntry.gameObject.SetActive(false);

			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);
			data.m_valueCountries = new List<int>(chosenKPIs.Count);
			foreach (KPIValue kpi in chosenKPIs)
			{
				data.m_valueCountries.Add(kpi.targetCountryId);

			}

			FetchDataInternal(chosenKPIs, data, a_timeSettings, a_stacked, out a_maxValue, out a_minValue);
			return data;
		}

		protected override void CreateDetailsWindow(int a_index)
		{
			if(a_index == 0)
			{
				m_detailsWindows[0] = Instantiate(m_detailsWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectMultiSelectWindow>();
				m_detailsWindows[0].SetContent(m_selectedValues, m_currentValueOptions, OnIDToggleChanged, OnAllIDTogglesChanged);
			}
			else
			{
				m_detailsWindows[1] = Instantiate(m_singleSelectWindowPrefab, m_contentToggles[a_index].m_detailsWindowParent).GetComponent<GraphContentSelectSingleSelectWindow>();
				m_detailsWindows[1].SetContent(new HashSet<int>() { m_selectedCountry }, m_allCountries, OnCountryToggleChanged, null);
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