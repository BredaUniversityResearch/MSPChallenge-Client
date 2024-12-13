using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class GraphLegend : MonoBehaviour
	{
		[SerializeField] GraphLegendEntry m_entryPrefab;

		GraphDataStepped m_data;
		int m_columns = 1;
		List<GraphLegendEntry> m_entries = new List<GraphLegendEntry>();
		bool m_horizontal = true;

		public void Initialise()
		{
			if (transform.childCount > 0)
			{
				foreach (Transform child in transform)
				{
					Destroy(child.gameObject);
				}
			}
		}

		public float SetSize(int a_w, int a_h, bool a_horizontal, float a_sideSpacing, float a_spacing, float a_topSpacing)
		{
			//Currently assumes it fills a full horizontal edge
			//set horizontal or vertical alignment
			//return expected size
			m_horizontal = a_horizontal;
			m_columns = m_horizontal ? a_w : System.Math.Max(1, a_w / 4);
			if (m_data != null)
				return SetData(m_data, a_sideSpacing, a_spacing, a_topSpacing);
			return 0f;
		}

		public float SetData(GraphDataStepped a_data, float a_sideSpacing, float a_spacing, float a_topSpacing)
		{
			m_data = a_data;
			//Determine max number of rows
			int rows = Mathf.CeilToInt(a_data.m_categoryNames.Length / (float)m_columns);
			//Start positioning from top row
			int i = 0;
			for(int y = 0; y < rows && i < a_data.m_categoryNames.Length; y++)
			{
				for (int x = 0; x < m_columns && i < a_data.m_categoryNames.Length; x++)
				{
					if (i >= m_entries.Count)
					{
						m_entries.Add(Instantiate(m_entryPrefab, transform).GetComponent<GraphLegendEntry>());
					}
					m_entries[i].SetData(a_data.m_categoryNames[i], DashboardManager.Instance.ColourList.GetColour(a_data.m_absoluteCategoryIndices[i]),
						x / (float)m_columns,
						(x + 1) / (float)m_columns,
						a_spacing / 2f,
						y * (m_entryPrefab.m_height + a_spacing));
					i++;
				}
			}
			for(; i < m_entries.Count; i++)
			{
				m_entries[i].gameObject.SetActive(false);
			}

			RectTransform rect = GetComponent<RectTransform>();
			if(m_horizontal)
			{
				float size = rows * m_entryPrefab.m_height + (rows - 1) * a_spacing + a_sideSpacing;
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(1f, 0f);
				rect.offsetMin = new Vector2(a_sideSpacing, a_sideSpacing);
				rect.offsetMax = new Vector2(-a_sideSpacing, size);
				return size;
			}
			else
			{
				float size = m_columns * m_entryPrefab.m_preferredWidth + (m_columns - 1) * a_spacing + a_sideSpacing;
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(0f, 1f);
				rect.offsetMin = new Vector2(a_sideSpacing, a_sideSpacing);
				rect.offsetMax = new Vector2(size, -a_topSpacing);
				return size;
			}
		}

	}
}
