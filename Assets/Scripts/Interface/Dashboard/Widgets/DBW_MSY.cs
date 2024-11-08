using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
    public class DBW_MSY : ADashboardWidget
    {
        readonly string[] MONTH_STEP_NAMES = {
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sept",
            "Oct",
            "Nov",
            "Dec"
        };

        [SerializeField] protected GraphAxis m_valueAxis;
        [SerializeField] protected GraphAxis m_stepAxis;
        [SerializeField] protected GraphLegend m_legend;
        [SerializeField] protected SteppedGraphBars m_graph;



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


			m_legend.SetData(data);
            m_valueAxis.SetDataRange(data, 0, maxValue);
            m_stepAxis.SetDataStepped(data);
            m_graph.SetData(data);
        }

        protected override void OnSizeChanged(int a_w, int a_h) 
        {
            m_legend.SetSize(a_w, a_h);
            m_valueAxis.SetSize(a_w, a_h);
            m_stepAxis.SetSize(a_w, a_h);
           //TODO: gather/set offsets and set graph region
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
