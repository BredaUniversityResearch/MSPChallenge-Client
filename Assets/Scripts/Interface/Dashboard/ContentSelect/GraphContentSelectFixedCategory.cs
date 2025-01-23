using System;
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
		[SerializeField] protected string[] m_categoryNames;

		bool[] m_valueToggles;
		List<KPICategory> m_categories;
		List<KPIValue> m_values;
		GraphContentSelectFixedCategoryWindow m_detailsWindow;

		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			base.Initialise(a_onSettingsChanged, a_widget);

			KPIValueCollection kvc = SimulationLogicMEL.Instance.GetKPIValuesForCountry();
			m_categories = new List<KPICategory>();
			m_values = new List<KPIValue>();
			foreach(string s in m_categoryNames)
			{
				KPICategory cat = kvc.FindCategoryByName(s);
				if (cat == null)
					continue;
				m_categories.Add(cat);
				cat.OnValueUpdated += OnKPIChanged;
				m_values.AddRange(cat.GetChildValues());
			}
			m_valueToggles = new bool[m_values.Count];
			for (int i = 0; i < m_valueToggles.Length; i++)
				m_valueToggles[i] = true;
			if (m_categories.Count == 0)
			{
				m_noDataEntry.gameObject.SetActive(m_categories.Count == 0);
				m_noDataEntry.text = "NO DATA AVAILABLE";
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

		void OnToggleValueChanged(int a_index, bool a_value)
		{
			m_valueToggles[a_index] = a_value;
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
				if (m_valueToggles[index])
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
			data.m_categoryNames = new string[chosenKPIs.Count];
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);

			for (int i = 0; i < chosenKPIs.Count; i++)
			{
				data.m_categoryNames[i] = chosenKPIs[i].displayName;
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

		protected override void CreateDetailsWindow()
		{
			m_detailsWindow = Instantiate(m_detailsWindowPrefab, m_detailsWindowParent).GetComponent<GraphContentSelectFixedCategoryWindow>();
			m_detailsWindow.Initialise(m_valueToggles, m_values, OnToggleValueChanged);
		}

		protected override void DestroyDetailsWindow()
		{
			Destroy(m_detailsWindow.gameObject);
			m_detailsWindow = null;
		}
	}
}