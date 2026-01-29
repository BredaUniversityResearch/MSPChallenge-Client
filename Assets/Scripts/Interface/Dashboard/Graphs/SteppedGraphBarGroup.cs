using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class SteppedGraphBarGroup : MonoBehaviour
	{
		[SerializeField] Transform m_barParent;
		[SerializeField] GameObject m_barPrefab;

		List<SteppedGraphBarSingle> m_bars = new List<SteppedGraphBarSingle>();
		bool m_stacked;

		public void SetStacked(bool a_stacked)
		{
			m_stacked = a_stacked;
		}

		public void SetData(GraphDataStepped a_data, int a_step, float a_min, float a_max, float a_offsetL, float a_offsetR)
		{
			gameObject.SetActive(true);
			
			RectTransform rect = GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(a_min, 0f);
			rect.anchorMax = new Vector2(a_max, 1f);	
			rect.offsetMin= new Vector2(a_offsetL, 0f);
			rect.offsetMax = new Vector2(-a_offsetR, 0f);

			int entriesPerStep = a_data.m_steps[a_step].Length;
			//int entriesPerStep = a_data.m_selectedDisplayIDs.Count;
			//if (a_data.m_selectedCountries != null)
			//	entriesPerStep *= a_data.m_selectedCountries.Count;
			//int nextEntryIndex = 0;
			//if(a_data.OverLapPatternSet)
			//{
			//	if(m_stacked) //Stacked and overlapping sets
			//	{
			//		int setIndex = -1;
			//		float ymin = 0f;
			//      float nextMin = 0f; //Moved here 29/01
			//		for (int i = 0; i < entriesPerStep; i++)
			//		{
			//			
			//			if (a_data.GetPatternIndex(i) == 0)
			//			{
			//				setIndex++;
			//				ymin = nextMin;
			//			}
			//			if (!a_data.m_steps[a_step][i].HasValue)
			//				continue;
			//			if (nextEntryIndex == m_bars.Count)
			//				m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());

			//			float ymax = ymin + (a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange;
			//			if (ymax > nextMin)
			//				nextMin = ymax;
			//			m_bars[nextEntryIndex].SetData(a_data, a_step, i, 0f, 1f, ymin, ymax);
			//			ymin = ymax;
			//			nextEntryIndex++;
			//		}
			//	}
			//	else //Only overlapping sets
			//	{
			//		int setIndex = -1;
			//		for (int i = 0; i < entriesPerStep; i++)
			//		{
			//			if (a_data.GetPatternIndex(i) == 0)
			//				setIndex++;
			//			if (!a_data.m_steps[a_step][i].HasValue)
			//				continue;
			//			if (nextEntryIndex == m_bars.Count)
			//				m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());

			//			m_bars[nextEntryIndex].SetData(a_data, a_step, i,
			//				setIndex / (float)a_data.PatternSetsPerStep,
			//				(setIndex + 1) / (float)a_data.PatternSetsPerStep,
			//				0f,
			//				(a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange);
			//			nextEntryIndex++;
			//		}
			//	}
			//}
			//else if(m_stacked) //Only stacked
			//{
			//	float ymin = 0f;
			//	for(int i = 0; i < entriesPerStep; i++)
			//	{
			//		if (!a_data.m_steps[a_step][i].HasValue)
			//			continue;
			//		if(nextEntryIndex == m_bars.Count)
			//			m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());
			//		float ymax = ymin + (a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange;
			//		m_bars[nextEntryIndex].SetData(a_data, a_step, i, 0f, 1f, ymin, ymax);
			//		ymin = ymax;
			//		nextEntryIndex++;
			//	}
			//}
			//else //Regular
			//{
			//	for (int i = 0; i < entriesPerStep; i++)
			//	{
			//		if (!a_data.m_steps[a_step][i].HasValue)
			//			continue;
			//		if (nextEntryIndex == m_bars.Count)
			//			m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());
			//		m_bars[nextEntryIndex].SetData(a_data, a_step, i, 
			//			i / (float)entriesPerStep,  //xmin
			//			(i+1) / (float)entriesPerStep,  //xmax
			//			0f, //ymin
			//			(a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange); //ymax
			//		nextEntryIndex++;
			//	}
			//}

			int nextEntryIndex = 0;
			int setIndex = -1;
			float ymin = 0f;
			float ymax = 0f;
			float xmin = 0f;
			float xmax = 1f;
			float nextMin = 0f;
			for (int i = 0; i < entriesPerStep; i++)
			{
				if (a_data.GetPatternIndex(i) == 0) //Only really used for pattern sets
				{
					setIndex++;
					ymax = nextMin;
				}
				if (!a_data.m_steps[a_step][i].HasValue)
					continue;
				if (nextEntryIndex == m_bars.Count)
					m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());

				if (a_data.OverLapPatternSet)
				{
					if (m_stacked) //Stacked and overlapping sets
					{
						ymin = ymax;
						ymax += (a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange;
						if (ymax > nextMin)
							nextMin = ymax;
					}
					else //Only overlapping sets
					{
						xmin = setIndex / (float)a_data.PatternSetsPerStep;
						xmax = (setIndex + 1) / (float)a_data.PatternSetsPerStep;
						ymin = 0f;
						ymax = (a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange;
					}
				}
				else if (m_stacked) //Only stacked
				{
					ymin = ymax;
					ymax += (a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange;
				}
				else //Regular
				{
					xmin = i / (float)entriesPerStep;
					xmax = (i + 1) / (float)entriesPerStep;
					ymax = (a_data.m_steps[a_step][i].Value - a_data.m_graphMin) / a_data.m_graphRange;

				}
				m_bars[nextEntryIndex].SetData(a_data, a_step, i, xmin, xmax, ymin, ymax);
				nextEntryIndex++;
			}

			//Remove unused
			for (int i = nextEntryIndex; i < m_bars.Count; i++)
				Destroy(m_bars[i].gameObject);
			if (nextEntryIndex != m_bars.Count)
				m_bars.RemoveRange(nextEntryIndex, m_bars.Count - nextEntryIndex);
		}
	}
}
