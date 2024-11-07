using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
    public class DBW_MSY : ADashboardWidget
    {
        [SerializeField] protected GraphAxis m_valueAxis;
        [SerializeField] protected GraphAxis m_stepAxis;
        [SerializeField] protected GraphLegend m_legend;
        [SerializeField] protected SteppedGraphBars m_graph;

        public void UpdateData()
		{
            //TODO: get actual data
            GraphDataStepped data = null;

            //TODO: determine data range

            m_legend.SetData(data);
            m_valueAxis.SetDataRange(data);
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
    }
}
