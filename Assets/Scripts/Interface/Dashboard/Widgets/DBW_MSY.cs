using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEditor.PlayerSettings;

namespace MSP2050.Scripts
{
    public class DBW_MSY : ADashboardWidget
    {
        [SerializeField] protected GraphAxis m_valueAxis;
        [SerializeField] protected GraphAxis m_stepAxis;
        [SerializeField] protected GraphLegend m_legend;
        [SerializeField] protected SteppedGraphBars m_graph;
        [SerializeField] protected GraphTimeSelect m_timeSelect;
        [SerializeField] protected AGraphContentSelect m_contentSelect;
		[SerializeField] float m_sideSpacing = 16f;
		[SerializeField] float m_spacing = 12f;

        bool m_legendBottom = true;

		public override void UpdateData()
		{
            KPIValueCollection kvc = SimulationLogicMEL.Instance.GetKPIValuesForCountry();
            List<KPIValue> kpiValues = GetKPIValuesByName(kvc, new string[]{ 
                "Demersal Fish",
				"Flatfish",
				"Cod",
				"Sandeel"
			});

			//TODO: get actual data
			GraphDataStepped data = new GraphDataStepped();

            ValueConversionCollection vcc = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
            vcc.TryGetConverter(kpiValues[0].unit, out data.m_unit);
            //if (!vcc.TryGetConverter(kpiValues[0].unit, out data.m_unit))
                //{
                //    data.m_unit =        
                //}

                //TODO: determine data range
            float maxValue = float.NegativeInfinity;
            data.m_stepNames = MONTH_STEP_NAMES;
            data.m_categoryNames = new string[kpiValues.Count];
			data.m_categoryColours = new Color[kpiValues.Count];
            for(int i = 0; i < kpiValues.Count; i++)
            {
                data.m_categoryNames[i] = kpiValues[i].displayName;
				data.m_categoryColours[i] = kpiValues[i].graphColor;
            }
            data.m_steps = new List<float?[]>(12);
            for (int i = 0; i < 12; i++)
            {
                data.m_steps.Add(new float?[kpiValues.Count]);
                for (int j = 0; j < kpiValues.Count; j++)
                {
                    float? v = kpiValues[j].GetKpiValueForMonth(i);
                    data.m_steps[i][j] = v;
                    if(v.HasValue && v > maxValue)
                        maxValue = v.Value;
                }
			}

            if(maxValue < 1f)
                maxValue = 1f;

			float legendSize = m_legend.SetData(data, m_sideSpacing, m_spacing);
            m_valueAxis.SetDataRange(data, 0, maxValue); //Also sets scale
            m_stepAxis.SetDataStepped(data);
            m_graph.SetData(data);
            SetRectPositions(legendSize);           
		}

        protected override void OnSizeChanged(int a_w, int a_h) 
        {
            m_legendBottom = a_w / a_h < 4;
			float legendSize = m_legend.SetSize(a_w, a_h, m_legendBottom, m_sideSpacing, m_spacing);
            m_valueAxis.SetSize(a_w, a_h);
            m_stepAxis.SetSize(a_w, a_h);
			SetRectPositions(legendSize);
		}

        void SetRectPositions(float a_legendSize)
        {
			Vector2 graphCorner, axisCorner;
			if (m_legendBottom)
			{
				graphCorner = new Vector2(m_sideSpacing + m_valueAxis.m_size, a_legendSize + m_spacing + m_stepAxis.m_size);
				axisCorner = new Vector2(m_sideSpacing, a_legendSize + m_spacing);
			}
			else
			{
				graphCorner = new Vector2(a_legendSize + m_spacing + m_valueAxis.m_size, m_sideSpacing + m_stepAxis.m_size);
				axisCorner = new Vector2(a_legendSize + m_spacing, m_sideSpacing);
			}
			m_valueAxis.SetRectOffset(
				new Vector2(0f, 1f),
				new Vector2(axisCorner.x, graphCorner.y),
				new Vector2(graphCorner.x, -m_sideSpacing));
			m_stepAxis.SetRectOffset(
				new Vector2(1f, 0f),
				new Vector2(graphCorner.x, axisCorner.y),
				new Vector2(-m_sideSpacing, graphCorner.y));
			m_graph.SetRectOffset(
				new Vector2(graphCorner.x, graphCorner.y),
				new Vector2(-m_sideSpacing, -m_sideSpacing));
		}

		List<KPIValue> GetKPIValuesByName(KPIValueCollection a_kvc, string[] a_names)
        {
			List<KPIValue> result = new List<KPIValue>();
            foreach (string s in a_names)
            {
				KPIValue value = a_kvc.FindValueByName(s);
                if(value != null) 
                    result.Add(value);
			}
            return result;
		}

	}
}
