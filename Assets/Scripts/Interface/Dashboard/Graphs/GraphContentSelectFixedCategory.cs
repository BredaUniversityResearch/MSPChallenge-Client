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
		[SerializeField] string m_categoryName;

		bool[] m_valueToggles;
		KPICategory m_category;
		GraphContentSelectFixedCategoryWindow m_detailsWindow;

		public override void Initialise(Action a_onSettingsChanged)
		{
			base.Initialise(a_onSettingsChanged);

			KPIValueCollection kvc = SimulationLogicMEL.Instance.GetKPIValuesForCountry();
			m_category = kvc.FindCategoryByName(m_categoryName);
			m_valueToggles = new bool[m_category.GetChildValueCount()];
			for (int i = 0; i < m_valueToggles.Length; i++)
				m_valueToggles[i] = true;

			//TODO: subscribe to KPI change
		}

		void OnToggleValueChanged(int a_index, bool a_value)
		{
			m_valueToggles[a_index] = a_value;
			m_onSettingsChanged.Invoke();
		}

		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, out float a_maxValue, out float a_minValue)
		{
			GraphDataStepped data = new GraphDataStepped();
			List<KPIValue> chosenKPIs = new List<KPIValue>();
			int index = 0;
			foreach(KPIValue v in m_category.GetChildValues())
			{
				if (m_valueToggles[index])
					chosenKPIs.Add(v);
				index++;
			}

			a_minValue = 0f;
			a_maxValue = float.NegativeInfinity;

			//if(chosenKPIs.Count == 0)
			//{
			//	data.m_stepNames = a_timeSettings.m_stepNames;
			//	data.m_categoryNames = new string[0];
			//	data.m_categoryColours = new Color[0];
			//	data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);
			//	a_maxValue = 1f;
			//}

			ValueConversionCollection vcc = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
			if (chosenKPIs.Count > 0)
			{
				vcc.TryGetConverter(chosenKPIs[0].unit, out data.m_unit);
			}
			else
			{
				vcc.TryGetConverter("", out data.m_unit);
			}

			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_categoryNames = new string[chosenKPIs.Count];
			data.m_categoryColours = new Color[chosenKPIs.Count];
			data.m_steps = new List<float?[]>(a_timeSettings.m_stepNames.Count);

			for (int i = 0; i < chosenKPIs.Count; i++)
			{
				data.m_categoryNames[i] = chosenKPIs[i].displayName;
				data.m_categoryColours[i] = chosenKPIs[i].graphColor;
			}

			if(a_timeSettings.m_aggregationFunction != null)
			{
				//get sets, then aggregate
				for(int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					data.m_steps.Add(new float?[chosenKPIs.Count]);
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
							if(aggregatedV.Value > a_maxValue)
								a_maxValue = aggregatedV.Value;
							if (aggregatedV.Value < a_minValue)
								a_minValue = aggregatedV.Value;
						}
					}
				}
			}
			else
			{
				//get data directly
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					data.m_steps.Add(new float?[chosenKPIs.Count]);
					for (int j = 0; j < chosenKPIs.Count; j++)
					{
						float? v = chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						data.m_steps[i][j] = v;
						if (v.HasValue)
						{
							if (v.Value > a_maxValue)
								a_maxValue = v.Value;
							if (v.Value < a_minValue)
								a_minValue = v.Value;
						}
					}
				}
			}

			if (a_maxValue == Mathf.NegativeInfinity)
				a_maxValue = 1f;
			return data;
		}

		protected override void CreateDetailsWindow()
		{
			m_detailsWindow = Instantiate(m_detailsWindowPrefab, m_detailsWindowParent).GetComponent<GraphContentSelectFixedCategoryWindow>();
			m_detailsWindow.Initialise(m_valueToggles, m_category.GetChildValues(), OnToggleValueChanged);
		}

		protected override void DestroyDetailsWindow()
		{
			Destroy(m_detailsWindow.gameObject);
			m_detailsWindow = null;
		}
	}
}