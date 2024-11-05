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

		//public void Initialise(bool a_stacked)
		//{
		//	m_stacked = a_stacked;
		//}

		public void SetStacked(bool a_stacked)
		{
			m_stacked = a_stacked;
		}

		public void SetData(GraphDataStepped a_data, int a_step)
		{
			gameObject.SetActive(true);
			
			if(m_stacked)
			{
				float ymin = 0f;
				for(int i = 0; i < a_data.m_categoryNames.Length; i++)
				{
					if(i < m_bars.Count)
						m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());
					float value = a_data.m_steps[a_step][i];
					float ymax = ymin + value / a_data.m_scale;
					m_bars[i].SetData(a_data, a_step, i, 0f, 1f, ymin, ymax);
					ymin = ymax;
				}
			}
			else
			{
				for (int i = 0; i < a_data.m_categoryNames.Length; i++)
				{
					if (i < m_bars.Count)
						m_bars.Add(Instantiate(m_barPrefab, m_barParent).GetComponent<SteppedGraphBarSingle>());
					float value = a_data.m_steps[a_step][i];
					float ymax = ymin + value / a_data.m_scale;
					m_bars[i].SetData(a_data, a_step, i, 
						i / a_data.m_categoryNames.Length, 
						(i+1) / a_data.m_categoryNames.Length, 
						0f, ymin, ymax);
					ymin = ymax;
				}
			}
			//TODO: set actual data
		}
	}
}
