using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class GraphLegend : MonoBehaviour
	{
		[SerializeField] float m_spacing;
		//[SerializeField] bool m_horizontalEdge = true;
		[SerializeField] GraphLegendEntry m_entryPrefab;

		GraphDataStepped m_data;
		int m_columns = 1;
		List<GraphLegendEntry> m_entries = new List<GraphLegendEntry>();

		public void SetSize(int a_w, int a_h)
		{
			//Currently assumes it fills a full horizontal edge
			//TODO: set horizontal or vertical alignment
			//TODO: return expected size
			m_columns = System.Math.Max(1, a_w / 2);
			if (m_data != null)
				SetData(m_data);
		}

		public void SetData(GraphDataStepped a_data)
		{
			m_data = a_data;
			//Determine max number of rows
			int rows = Mathf.CeilToInt(a_data.m_categoryNames.Length / m_columns);
			//Start positioning from top row
			int i = 0;
			for(int y = 0; y < rows && i < a_data.m_categoryColours.Length; y++)
			{
				for (int x = 0; x < m_columns && i < a_data.m_categoryColours.Length; x++)
				{
					if (i >= m_entries.Count)
					{
						m_entries.Add(Instantiate(m_entryPrefab, transform).GetComponent<GraphLegendEntry>());
					}
					m_entries[i].SetData(a_data.m_categoryNames[i], a_data.m_categoryColours[i],
						x / m_columns,
						(x + 1) / m_columns,
						m_spacing / 2f,
						y * (m_entryPrefab.m_height + m_spacing));
				}
			}
			for(; i < m_entries.Count; i++)
			{
				m_entries[i].gameObject.SetActive(false);
			}
		}

	}
}
