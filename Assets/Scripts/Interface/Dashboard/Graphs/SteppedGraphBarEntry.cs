using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class SteppedGraphBarEntry : MonoBehaviour
	{
		[SerializeField] Transform m_barParent;

		List<RectTransform> m_bars = new List<RectTransform>();
		bool m_stacked;

		//public void Initialise(bool a_stacked)
		//{
		//	m_stacked = a_stacked;
		//}

		public void SetStacked(bool a_stacked)
		{
			m_stacked = a_stacked;
		}

		public void SetData(GraphDataStepped a_data, int a_index)
		{ }
	}
}
