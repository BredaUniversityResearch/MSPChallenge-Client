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
		[SerializeField] int m_totalSpacing = 128;

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

		public override void Initialise()
		{
			if (m_barParent.childCount > 1)
			{
				for(int i = 1; i < m_barParent.childCount; i++)
				{
					Destroy(transform.GetChild(i).gameObject);
				}
			}
		}

		public void SetRectOffset(Vector2 a_offsetMin, Vector2 a_offsetMax)
		{
			RectTransform rect = GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = a_offsetMin;
			rect.offsetMax = a_offsetMax;
		}

		public override void SetData(GraphDataStepped a_data)
		{
			if (m_entries == null)
				m_entries = new List<SteppedGraphBarGroup>();

			m_data = a_data;
			float halfSpacing = m_data.m_steps.Count == 0 ? 0f : m_totalSpacing / m_data.m_steps.Count / 2;

			int i = 0;
			for(; i < a_data.m_steps.Count; i++)
			{
				if (i >= m_entries.Count)
				{
					SteppedGraphBarGroup newEntry = Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarGroup>();
					newEntry.SetStacked(m_stacked);
					m_entries.Add(newEntry);
				}
				m_entries[i].SetData(a_data, i,
					i / (float)a_data.m_steps.Count,
					(i + 1) / (float)a_data.m_steps.Count,
					halfSpacing,
					halfSpacing);			
			}

			//Clear unused entries
			int clearFrom = i;
			for(; i < m_entries.Count; i++)
				Destroy(m_entries[i].gameObject);
			if(clearFrom != m_entries.Count)
				m_entries.RemoveRange(clearFrom, m_entries.Count - clearFrom);
		}
	}
}
