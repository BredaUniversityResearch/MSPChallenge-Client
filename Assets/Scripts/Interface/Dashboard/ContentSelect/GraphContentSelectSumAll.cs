using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectSumAll : AGraphContentSelect
	{
		public enum KPISource { Ecology, Energy, Shipping, Geometry, Other }
		[SerializeField] protected string[] m_categoryNames;
		[SerializeField] protected KPISource m_kpiSource;
		[SerializeField] protected string m_entryName;
		[SerializeField] protected bool m_onlyCompleteSets = true;

		List<KPIValue> m_values;
		List<KPICategory> m_categories;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);

			List<KPIValueCollection> kvcs = null;
			m_categories = new List<KPICategory>();
			m_values = new List<KPIValue>();

			switch (m_kpiSource)
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
			int valueIndex = 0;
			foreach (KPIValueCollection valueColl in kvcs)
			{
				foreach (string s in m_categoryNames)
				{
					KPICategory cat = valueColl.FindCategoryByName(s);
					if (cat == null)
						continue;
					cat.OnValueUpdated += OnKPIChanged;
					m_categories.Add(cat);
					m_values.AddRange(cat.GetChildValues());
				}
				valueIndex++;
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

		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			GraphDataStepped data = new GraphDataStepped();
			data.m_absoluteCategoryIndices = new List<int>();
			data.m_unit = m_unit;
			data.m_undefinedUnit = m_undefinedUnit;

			for (int i = 0; i < m_values.Count; i++ )
			{
				data.m_absoluteCategoryIndices.Add(i);
			}

			if (m_values.Count == 0)
			{
				m_noDataEntry.gameObject.SetActive(true);
				m_noDataEntry.text = "NO CONTENT SELECTED";
			}
			else
				m_noDataEntry.gameObject.SetActive(false);

			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);
			if(m_values.Count > 0)
				data.m_selectedDisplayIDs = new List<string>(1) { m_entryName };
			else
				data.m_selectedDisplayIDs = new List<string>();

			a_minValue = 0f;
			a_maxValue = float.NegativeInfinity;

			if (a_timeSettings.m_aggregationFunction != null)
			{
				//Aggregated
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					float? sum = 0f;
					if (m_values.Count == 0 && m_onlyCompleteSets)
						sum = null;
					for (int j = 0; j < m_values.Count; j++)
					{
						List<float?> values = new List<float?>(a_timeSettings.m_months[i].Count);
						foreach (int month in a_timeSettings.m_months[i])
						{
							values.Add(m_values[j].GetKpiValueForMonth(month));
						}
						float? aggregatedV = a_timeSettings.m_aggregationFunction(values);
						if (aggregatedV.HasValue && sum.HasValue)
						{
							sum += aggregatedV.Value;
						}
						else if(m_onlyCompleteSets)
						{
							sum = null;
						}
					}

					data.m_steps.Add(new float?[1] {sum});
					if (sum.HasValue)
					{
						a_maxValue = Mathf.Max(a_maxValue, sum.Value);
						a_minValue = Mathf.Min(a_minValue, sum.Value);
					}
					
				}
			}
			else
			{
				//Get data directly
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					float? sum = 0f;
					if (m_values.Count == 0 && m_onlyCompleteSets)
						sum = null;
					for (int j = 0; j < m_values.Count; j++)
					{
						float? v = m_values[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						if (v.HasValue && sum.HasValue)
						{
							sum += v.Value;
						}
						else if (m_onlyCompleteSets)
						{
							sum = null;
						}
					}
					data.m_steps.Add(new float?[1] { sum });
					if (sum.HasValue)
					{
						a_maxValue = Mathf.Max(a_maxValue, sum.Value);
						a_minValue = Mathf.Min(a_minValue, sum.Value);
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
		{ }

		protected override void DestroyDetailsWindow(int a_index)
		{ }
	}
}