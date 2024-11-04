using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MSP2050.Scripts
{
	public class SteppedGraphBars : ASteppedGraph
	{
		[SerializeField] RectTransform m_barParent;
		[SerializeField] GameObject m_barPrefab;
		[SerializeField] RectTransform m_legendParent;
		[SerializeField] GameObject m_legendEntryPrefab;

		public override void SetData(GraphDataStepped a_data)
		{
			
		}
	}
}
