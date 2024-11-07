using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class SteppedGraphBars : ASteppedGraph
	{
		[SerializeField] RectTransform m_barParent;
		[SerializeField] GameObject m_barPrefab;

		GraphDataStepped m_data;
		List<SteppedGraphBarGroup> m_entries;
		bool m_stacked;

		public bool Stacked 
		{
			get { return m_stacked; }
			set 
			{ 
				if(m_stacked != value && m_data != null)
				{
					m_stacked = value; 
					if(m_entries != null)
					{
						foreach (var entry in m_entries)
							entry.SetStacked(m_stacked);
					}
					SetData(m_data);
				}
				else
					m_stacked = value; 
			}
		}

		public override void SetData(GraphDataStepped a_data)
		{
			if (m_entries == null)
				m_entries = new List<SteppedGraphBarGroup>();

			m_data = a_data;
			//m_legend.SetData(a_data);
			//m_valueAxis.SetData(a_data);
			//m_stepAxis.SetData(a_data);

			for(int i = 0; i < a_data.m_steps.Count; i++)
			{
				if(i < m_entries.Count)
				{
					m_entries[i].SetData(a_data, i);
				}
				else
				{
					SteppedGraphBarGroup newEntry = Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarGroup>();
					newEntry.SetStacked(m_stacked);
					newEntry.SetData(a_data, i);
					m_entries.Add(newEntry);
				}
			}
		}
	}
}
