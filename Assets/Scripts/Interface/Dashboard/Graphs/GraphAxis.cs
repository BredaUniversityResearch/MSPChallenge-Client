using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class GraphAxis : MonoBehaviour
	{
		[SerializeField] bool m_horizontal = true;
		[SerializeField] GameObject m_entryPrefab;

		List<GraphAxisEntry> m_entries = new List<GraphAxisEntry>();

		public void SetSize(int a_w, int a_h)
		{ 
			//TODO: if needed
		}

		public void SetDataStepped(GraphDataStepped a_data)
		{
			int i = 0;
			for (; i < a_data.m_stepNames.Length; i++)
			{
				if (i >= m_entries.Count)
					m_entries.Add(Instantiate(m_entryPrefab, transform).GetComponent<GraphAxisEntry>());

				m_entries[i].SetValue(a_data.m_stepNames[i],
					i / (float)a_data.m_stepNames.Length,
					(i + 1) / (float)a_data.m_stepNames.Length);
			}

			//Disable unused
			for (; i < m_entries.Count; i++)
			{
				m_entries[i].gameObject.SetActive(false);
			}
		}

		public void SetDataRange(GraphDataStepped a_data, float a_min, float a_max)
		{
			float step = (float)RoundToSignificantDigits(a_max - a_min, 1) / 5f;
			float truncMin = (float)RoundToSignificantDigits(a_min + step, 1);

			int nextEntryIndex = 0;

			//Set first 1/2 manually
			SetEntry(a_data.m_unit.ConvertUnit(a_min).FormatAsString(), 0f, nextEntryIndex++);
			if(truncMin - a_min > step)
			{
				SetEntry(a_data.m_unit.ConvertUnit(truncMin).FormatAsString(), (truncMin-a_min)/a_max, nextEntryIndex++);
			}

			//Set all inbetween points
			int i = 1; 
			while(a_min + i * step <= a_max-0.001f)
			{
				SetEntry(a_data.m_unit.ConvertUnit(truncMin + i*step).FormatAsString(), (truncMin + i * step - a_min) / a_max, nextEntryIndex++);
				i++;
			}

			//Set last manually
			SetEntry(a_data.m_unit.ConvertUnit(a_min).FormatAsString(), 0f, nextEntryIndex++);

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

		double TruncateToSignificantDigits(double a_value, int a_digits)
		{
			if (a_value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(a_value))) + 1 - a_digits);
			return scale * Math.Truncate(a_value / scale);
		}

		double RoundToSignificantDigits(double a_value, int a_digits)
		{
			if (a_value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(a_value))) + 1);
			return scale * Math.Round(a_value / scale, a_digits);
		}
	}
}
