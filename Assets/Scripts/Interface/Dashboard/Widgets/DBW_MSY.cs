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
        [SerializeField] protected GraphStepGrouper m_stepGrouper;
        [SerializeField] protected GraphLegend m_legend;
        [SerializeField] protected SteppedGraphBars m_graph;
        [SerializeField] protected GraphTimeSelect m_timeSelect;
        [SerializeField] protected AGraphContentSelect m_contentSelect;
		[SerializeField] float m_sideSpacing = 16f;
		[SerializeField] float m_topSpacing = 44f;
		[SerializeField] float m_spacing = 12f;

        bool m_legendBottom = true;
		bool m_stacked = false;

		public override void InitialiseCatalogue()
		{
			InitialiseContent();
			base.InitialiseCatalogue();
		}

		public override void Initialise(ADashboardWidget a_original)
		{
			InitialiseContent();
			base.Initialise(a_original);
		}

		void InitialiseContent()
		{
			m_timeSelect.Initialise(UpdateData);
			m_contentSelect.Initialise(UpdateData, this);
			m_stepGrouper.Initialise();
			m_valueAxis.Initialise();
			m_stepAxis.Initialise();
			m_graph.Initialise();
			m_graph.Stacked = m_stacked;
			m_legend.Initialise();
		}

		public override void UpdateData()
		{
			float maxValue, minValue;
			GraphDataStepped data = m_contentSelect.FetchData(m_timeSelect.CurrentSettings, m_stacked, out maxValue, out minValue);
			float legendSize = m_legend.SetData(data, m_sideSpacing, m_spacing, m_topSpacing);
			m_stepGrouper.CreateGroups(m_timeSelect.CurrentSettings);
			m_valueAxis.SetDataRange(data, minValue, maxValue); //Also sets scale
            m_stepAxis.SetDataStepped(data);
            m_graph.SetData(data);
            SetRectPositions(legendSize);           
		}

        protected override void OnSizeChanged(int a_w, int a_h) 
        {
            m_legendBottom = a_w / a_h < 4;
			float legendSize = m_legend.SetSize(a_w, a_h, m_legendBottom, m_sideSpacing, m_spacing, m_topSpacing);
            m_valueAxis.SetSize(a_w, a_h);
            m_stepAxis.SetSize(a_w, a_h);
			SetRectPositions(legendSize);
		}

        void SetRectPositions(float a_legendSize)
        {
			Vector2 graphCorner, axisCorner;
			if (m_legendBottom)
			{
				graphCorner = new Vector2(m_sideSpacing + m_valueAxis.m_size, a_legendSize + m_spacing + m_spacing + m_stepAxis.m_size);
				axisCorner = new Vector2(m_sideSpacing, a_legendSize + m_spacing + m_spacing);
			}
			else
			{
				graphCorner = new Vector2(a_legendSize + m_spacing + m_valueAxis.m_size, m_sideSpacing + m_stepAxis.m_size);
				axisCorner = new Vector2(a_legendSize + m_spacing, m_sideSpacing);
			}
			//m_valueAxis.SetRectOffset(
			//	new Vector2(0f, 1f),
			//	new Vector2(axisCorner.x, graphCorner.y),
			//	new Vector2(graphCorner.x, -m_topSpacing));
			m_valueAxis.SetRectOffset(
				new Vector2(1f, 1f),
				new Vector2(axisCorner.x, graphCorner.y),
				new Vector2(-m_sideSpacing, -m_topSpacing));
			m_stepAxis.SetRectOffset(
				new Vector2(1f, 0f),
				new Vector2(graphCorner.x, axisCorner.y),
				new Vector2(-m_sideSpacing, graphCorner.y));
			m_graph.SetRectOffset(
				new Vector2(graphCorner.x, graphCorner.y),
				new Vector2(-m_sideSpacing, -m_topSpacing));
			m_stepGrouper.SetRectOffset(
				new Vector2(graphCorner.x, axisCorner.y),
				new Vector2(-m_sideSpacing, -m_topSpacing));
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
