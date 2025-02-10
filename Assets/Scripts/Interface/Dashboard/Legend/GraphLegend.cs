using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

namespace MSP2050.Scripts
{
	public class GraphLegend : MonoBehaviour
	{
		[SerializeField] GraphLegendEntry m_entryPrefab;
		[SerializeField] RectTransform m_entryParent;
		[SerializeField] float m_maxOuterSize;

		GraphDataStepped m_data;
		int m_columns = 1;
		List<GraphLegendEntry> m_entries = new List<GraphLegendEntry>();
		bool m_horizontal = true;
		int m_w, m_h;

		public void Initialise()
		{
			if (m_entryParent.childCount > 0)
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
			m_w = a_w;
			m_h = a_h;
			m_columns = m_horizontal ? a_w : System.Math.Max(1, a_w / 4);
			if (m_data != null)
				return SetData(m_data, a_sideSpacing, a_spacing, a_topSpacing);
			return 0f;
		}

		public float SetData(GraphDataStepped a_data, float a_sideSpacing, float a_spacing, float a_topSpacing)
		{
			m_data = a_data;
			//Start positioning from top row
			int i = 0;
			int rows;
			int startingRow = 0;

			if (a_data.UsesPattern)
			{
				startingRow = Mathf.CeilToInt(a_data.PatternNames.Count / (float)m_columns);
			}

			//Value entries
			if (a_data.m_valueCountries == null)
			{
				//Set entries to fixed colours
				rows = Mathf.CeilToInt(a_data.m_selectedDisplayIDs.Count / (float)m_columns) + startingRow;
				for (int y = startingRow; y < rows; y++)
				{
					for (int x = 0; x < m_columns && i < a_data.m_selectedDisplayIDs.Count; x++)
					{
						if (i >= m_entries.Count)
						{
							m_entries.Add(Instantiate(m_entryPrefab, m_entryParent).GetComponent<GraphLegendEntry>());
						}
						m_entries[i].SetData(a_data.m_selectedDisplayIDs[i], a_data.GetLegendDisplayColor(i),
							x / (float)m_columns,
							(x + 1) / (float)m_columns,
							a_spacing / 2f,
							y * (m_entryPrefab.m_height + a_spacing));
						
						i++;
					}
				}
			}
			else
			{
				//Set entries to country colours
				rows = Mathf.CeilToInt(a_data.m_valueCountries.Count / (float)m_columns) + startingRow;
				for (int y = startingRow; y < rows; y++)
				{
					for (int x = 0; x < m_columns && i < a_data.m_valueCountries.Count; x++)
					{
						if (i >= m_entries.Count)
						{
							m_entries.Add(Instantiate(m_entryPrefab, m_entryParent).GetComponent<GraphLegendEntry>());
						}
						Color color;
						if (a_data.m_selectedDisplayIDs.Count > 1)
						{
							//Focused per country, offset colour darkness
							float t = (float)(i + 1) / (a_data.m_selectedDisplayIDs.Count + 1);
							Team team = SessionManager.Instance.FindTeamByID(a_data.m_valueCountries[i]);
							if (team == null)
								color = new Color(t, t, t, 1f);
							else
							{
								color = DashboardManager.GetLerpedCountryColour(team.color, t);
							}
						}
						else
						{
							//Focused per type, show pure country colours
							Team team = SessionManager.Instance.FindTeamByID(a_data.m_valueCountries[i]);
							if (team == null)
								color = new Color(0.5f, 0.5f, 0.5f, 1f);
							else
								color = team.color;
						}
						m_entries[i].SetData(a_data.m_selectedDisplayIDs[i % a_data.m_selectedDisplayIDs.Count], color,
													x / (float)m_columns,
													(x + 1) / (float)m_columns,
													a_spacing / 2f,
													y * (m_entryPrefab.m_height + a_spacing));

						i++;
					}
				}
			}

			//Pattern entries
			if (a_data.UsesPattern)
			{
				for (int p = 0; p < a_data.PatternNames.Count; p++)
				{
					if (i >= m_entries.Count)
					{
						m_entries.Add(Instantiate(m_entryPrefab, m_entryParent).GetComponent<GraphLegendEntry>());
					}
					int x = p % m_columns;
					m_entries[i].SetData(a_data.PatternNames[p], p == 0 ? Color.white : Color.black,
							x / (float)m_columns,
							(x + 1) / (float)m_columns,
							a_spacing / 2f,
							p / m_columns * (m_entryPrefab.m_height + a_spacing), p);
					i++;
				}
			}

			//Disable unused
			for (; i < m_entries.Count; i++)
			{
				m_entries[i].gameObject.SetActive(false);
			}

			RectTransform rect = GetComponent<RectTransform>();
			float size = 0f;
			m_entryParent.sizeDelta = new Vector2(0f, rows * m_entryPrefab.m_height + (rows - 1) * a_spacing);
			int minSizeEntries = m_horizontal ? m_h * 2 : m_w * 2;
			if (m_horizontal)
			{
				size = Mathf.Min(rows * m_entryPrefab.m_height + (rows - 1) * a_spacing + a_sideSpacing, m_maxOuterSize, minSizeEntries * m_entryPrefab.m_height + (minSizeEntries - 1) * a_spacing);
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(1f, 0f);
				rect.offsetMin = new Vector2(a_sideSpacing, a_sideSpacing);
				rect.offsetMax = new Vector2(-a_sideSpacing, size);
			}
			else
			{
				size = Mathf.Min(m_columns * m_entryPrefab.m_preferredWidth + (m_columns - 1) * a_spacing + a_sideSpacing, m_maxOuterSize, minSizeEntries * m_entryPrefab.m_preferredWidth + (minSizeEntries - 1) * a_spacing + a_sideSpacing);
				rect.anchorMin = new Vector2(0f, 0f);
				rect.anchorMax = new Vector2(0f, 1f);
				rect.offsetMin = new Vector2(a_sideSpacing, a_sideSpacing);
				rect.offsetMax = new Vector2(size, -a_topSpacing);
			}
			return size;
		}
	}
}
