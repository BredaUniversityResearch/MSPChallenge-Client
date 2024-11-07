using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class ASteppedGraph : MonoBehaviour
	{
		//[SerializeField] protected GraphAxis m_valueAxis;
		//[SerializeField] protected GraphAxis m_stepAxis;
		//[SerializeField] protected GraphLegend m_legend;

		//protected int m_w;
		//protected int m_h;

		public abstract void SetData(GraphDataStepped a_data);
		//public virtual void SetSize(int a_w, int a_h)
		//{
		//	m_w = a_w;
		//	m_h = a_h;
		//}
	}
}
