using System;
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
		[SerializeField] float m_firstAndLastOffset = 4f;
		[SerializeField] int m_expectedEmptyChildren = 0;

		public float m_size;

		List<GraphAxisEntry> m_entries = new List<GraphAxisEntry>();

		public void Initialise()
		{
			if (transform.childCount > 1)
			{
				for (int i = m_expectedEmptyChildren; i < transform.childCount; i++)
				{
					Destroy(transform.GetChild(i).gameObject);
				}
			}
		}

		public void SetSize(int a_w, int a_h)
		{ }

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

				m_entries[i].SetValueStretch(a_data.m_stepNames[i],
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
			float absMax = Mathf.Max(Mathf.Abs(minT), Mathf.Abs((float)maxT));

			a_data.m_graphMin = minT;
			a_data.m_graphMax = (float)maxT;
			a_data.m_graphRange = maxScaled;
			if (a_data.m_unit != null)
			{
				a_data.m_unitIndex = a_data.m_unit.GetConversionUnitIndexForSize(absMax);
				a_data.m_scalePower = FindPower(absMax);
				a_data.m_unitEOffset = a_data.m_unit.GetUnitEntryEOffset(a_data.m_unitIndex);
			}
			else
				a_data.m_scalePower = FindPower(maxScaled);

			m_unitText.text = a_data.GetUnitString();

			//Set all inbetween points
			int i = 0;
			for(; i < 6; i++)
			{
				float v = minT + i * step;
				SetEntry(a_data.FormatValue(v),
					(v - minT) / maxScaled,
					i,
					i == 0 ? m_firstAndLastOffset : i == 5 ? -m_firstAndLastOffset : 0f);
			}

			//Disable unused
			for (; i < m_entries.Count; i++)
			{
				m_entries[i].gameObject.SetActive(false);
			}
		}

		void SetEntry(string a_value, float a_relativePos, int a_entryIndex, float a_textOffset)
		{
			if (a_entryIndex >= m_entries.Count)
				m_entries.Add(Instantiate(m_entryPrefab, transform).GetComponent<GraphAxisEntry>());
			m_entries[a_entryIndex].SetValuePoint(a_value, a_relativePos, m_size, a_textOffset);
		}

		int FindPower(float a_value)
		{
			float formatValue = Mathf.Abs(a_value);
			int power = 0;

			if (formatValue > 1000f)
			{
				formatValue /= 1000f;
				power += 3;
				while (formatValue >= 100f)
				{
					formatValue /= 10f;
					power++;
				}
				if(formatValue > 10f && (power == 2 || power == 5 || power == 8 || power == 11 || power == 14)) //Only move lower than 10 if it round out the scale
				{
					power++;
				}
			}
			else if(formatValue != 0 && formatValue <= 0.1f)
			{
				while (formatValue <= 1f)
				{
					formatValue *= 10f;
					power--;
				}
				if (formatValue > 10f && (power == -2 || power == -5 || power == -8 || power == -11 || power == -14)) //Only move lower than 10 if it round out the scale
				{
					power--;
				}
			}
			return power;
		}
	}
}
