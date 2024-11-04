using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class ASteppedGraph : MonoBehaviour
	{
		[SerializeField] protected GraphAxis m_valueAxis;
		[SerializeField] protected GraphAxis m_stepAxis;
		[SerializeField] protected GraphLegend m_legend;

		public abstract void SetData(GraphDataStepped a_data);
	}
}
