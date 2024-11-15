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

		public override void Initialise(Action a_onSettingsChanged)
		{
			base.Initialise(a_onSettingsChanged);

			KPIValueCollection kvc = SimulationLogicMEL.Instance.GetKPIValuesForCountry();
			m_category = kvc.FindCategoryByName(m_categoryName);
			m_valueToggles = new bool[m_category.GetChildValueCount()];
			for (int i = 0; i < m_valueToggles.Length; i++)
				m_valueToggles[i] = true;
		}

		public override GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, out float a_maxValue)
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

			ValueConversionCollection vcc = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
			vcc.TryGetConverter(chosenKPIs[0].unit, out data.m_unit);

			a_maxValue = float.NegativeInfinity;
			data.m_stepNames = a_timeSettings.m_stepNames;
			data.m_categoryNames = new string[chosenKPIs.Count];
			data.m_categoryColours = new Color[chosenKPIs.Count];

			for (int i = 0; i < chosenKPIs.Count; i++)
			{
				data.m_categoryNames[i] = chosenKPIs[i].displayName;
				data.m_categoryColours[i] = chosenKPIs[i].graphColor;
			}

			if(a_timeSettings.m_aggregationFunction != null)
			{
				//TODO: get sets, then aggregate
			}
			else
			{
				//TODO: get data directly, then aggregate
			}
			data.m_steps = new List<float?[]>(12);
			for (int i = 0; i < 12; i++)
			{
				data.m_steps.Add(new float?[chosenKPIs.Count]);
				for (int j = 0; j < chosenKPIs.Count; j++)
				{
					float? v = kpiValues[j].GetKpiValueForMonth(i);
					data.m_steps[i][j] = v;
					if (v.HasValue && v > a_maxValue)
						a_maxValue = v.Value;
				}
			}

			if (a_maxValue < 1f)
				a_maxValue = 1f;
		}

		protected override void CreateDetailsWindow()
		{
			throw new System.NotImplementedException();
		}

		protected override void DestroyDetailsWindow()
		{
			throw new System.NotImplementedException();
		}
	}
}