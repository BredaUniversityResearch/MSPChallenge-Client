﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

namespace MSP2050.Scripts
{
	public class GraphAxis : MonoBehaviour
	{
		[SerializeField] GameObject m_entryPrefab;
		[SerializeField] TextMeshProUGUI m_unitText;

		public float m_size;

		List<GraphAxisEntry> m_entries = new List<GraphAxisEntry>();

		public void SetSize(int a_w, int a_h)
		{ 
			//TODO: if needed
		}

		public void SetRectOffset(Vector2 a_anchorMax, Vector2 a_offsetMin, Vector2 a_offsetMax)
		{
			RectTransform rect = GetComponent<RectTransform>();			
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = a_anchorMax;
			rect.offsetMin = a_offsetMin;
			rect.offsetMax = a_offsetMax;
		}

		public void SetDataStepped(GraphDataStepped a_data)
		{
			int i = 0;
			for (; i < a_data.m_stepNames.Count; i++)
			{
				if (i == m_entries.Count)
					m_entries.Add(Instantiate(m_entryPrefab, transform).GetComponent<GraphAxisEntry>());

				m_entries[i].SetValue(a_data.m_stepNames[i],
					i / (float)a_data.m_stepNames.Count,
					(i + 1) / (float)a_data.m_stepNames.Count);
			}

			//Disable unused
			for (; i < m_entries.Count; i++)
			{
				m_entries[i].gameObject.SetActive(false);
			}
		}

		public void SetDataRange(GraphDataStepped a_data, float a_min, float a_max)
		{
			float minT = (float)((double)a_min).FloorToSignificantDigits(1);
			double maxT = ((double)a_max).CeilToSignificantDigits(1);
			float maxScaled = (float)(maxT - minT).CeilToSignificantDigits(1);
			float step = maxScaled / 5f;

			a_data.m_graphMin = minT;
			a_data.m_graphRange = maxScaled;
			a_data.m_unitIndex = a_data.m_unit.GetConversionUnitIndexForSize(maxScaled);
			m_unitText.text = a_data.m_unit.GetUnitStringForUnitIndex(a_data.m_unitIndex);

			int nextEntryIndex = 0;
			//Set all inbetween points
			int i = 0; 
			while(true)
			{
				float v = minT + i * step;
				SetEntry(a_data.m_unit.FormatValueWithUnitIndex(v, a_data.m_unitIndex), (v - minT) / maxScaled, nextEntryIndex++);
				if (v >= a_max - 0.001f)
					break;
				i++;
			}

			//Disable unused
			for (; nextEntryIndex < m_entries.Count; nextEntryIndex++)
			{
				m_entries[nextEntryIndex].gameObject.SetActive(false);
			}

		}

		void SetEntry(string a_value, float a_relativePos, int a_entryIndex)
		{
			if (a_entryIndex >= m_entries.Count)
				m_entries.Add(Instantiate(m_entryPrefab, transform).GetComponent<GraphAxisEntry>());
			m_entries[a_entryIndex].SetValue(a_value, a_relativePos);
		}
	}
}
