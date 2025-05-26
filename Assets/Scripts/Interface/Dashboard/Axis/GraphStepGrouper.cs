using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class GraphStepGrouper : MonoBehaviour
	{
		[SerializeField] GameObject[] m_groupPrefabs;
		[SerializeField] Transform m_groupParent;

		List<GraphStepGroup> m_groups = new List<GraphStepGroup>();

		public void Initialise()
		{ 
			if(m_groupParent.childCount > 0)
			{
				foreach(Transform child in m_groupParent)
				{
					Destroy(child.gameObject);
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

		public void CreateGroups(GraphTimeSettings a_timeSettings)
		{
			int nextGroupIndex = 0;
			if(a_timeSettings.m_aggregationFunction != null)
			{
				//Each step is a group
				for(; nextGroupIndex < a_timeSettings.m_months.Count; nextGroupIndex++)
				{
					SetGroup(nextGroupIndex, "",//a_timeSettings.m_stepNames[nextGroupIndex],
						nextGroupIndex / (float)a_timeSettings.m_months.Count,
						(nextGroupIndex + 1) / (float)a_timeSettings.m_months.Count);
				}
			}
			else
			{
				//Group by years
				if (a_timeSettings.m_months.Count == 0)
				{ }
				else if (a_timeSettings.m_months.Count == 1)
				{
					SetGroup(nextGroupIndex, Util.MonthToYearText(a_timeSettings.m_months[0][0]), 0f, 1f);
					nextGroupIndex++;
				}
				else
				{
					int groupStartIndex = 0;
					bool shorten = a_timeSettings.m_months.Count > 10;
					for (int i = 1; i < a_timeSettings.m_months.Count; i++)
					{
						if(a_timeSettings.m_months[i][0] % 12 == 0)
						{
							SetGroup(nextGroupIndex, shorten ? Util.MonthToYearText(a_timeSettings.m_months[groupStartIndex][0]).Substring(2, 2) : Util.MonthToYearText(a_timeSettings.m_months[groupStartIndex][0]),
								groupStartIndex / (float)a_timeSettings.m_months.Count,
								i / (float)a_timeSettings.m_months.Count);
							nextGroupIndex++;
							groupStartIndex = i;
						}
					}
					SetGroup(nextGroupIndex, shorten ? Util.MonthToYearText(a_timeSettings.m_months[groupStartIndex][0]).Substring(2, 2) : Util.MonthToYearText(a_timeSettings.m_months[groupStartIndex][0]),
								groupStartIndex / (float)a_timeSettings.m_months.Count,
								1f);
					nextGroupIndex++;
				}
			}

			//Clear unused entries
			int clearFrom = nextGroupIndex;
			for (; nextGroupIndex < m_groups.Count; nextGroupIndex++)
				Destroy(m_groups[nextGroupIndex].gameObject);
			if (clearFrom != m_groups.Count)
				m_groups.RemoveRange(clearFrom, m_groups.Count - clearFrom);
		}

		void SetGroup(int a_index, string a_name, float a_anchorMin, float a_anchorMax)
		{
			if(a_index >= m_groups.Count)
			{
				m_groups.Add(Instantiate(m_groupPrefabs[a_index % m_groupPrefabs.Length], m_groupParent).GetComponent<GraphStepGroup>());
			}
			m_groups[a_index].SetContent(a_name, a_anchorMin, a_anchorMax);
		}
	}
}
