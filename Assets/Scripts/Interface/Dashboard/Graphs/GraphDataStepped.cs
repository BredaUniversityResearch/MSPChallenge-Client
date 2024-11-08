using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class GraphDataStepped
	{
		public List<float?[]> m_steps; //Step (time step or distribution), value per category

		public ValueConversionUnit m_unit;
		public float m_scale;
		public string[] m_stepNames;
		public string[] m_categoryNames;
		public Color[] m_categoryColours;
	}
}
