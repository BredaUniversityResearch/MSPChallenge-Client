using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class GraphData
	{
		public List<float[]> m_data;
		public ValueConversionUnit m_yUnit;
		public float m_yScale;
		public string[] m_sequenceNames;
		public string[] m_xValueNames;
		public Color[] m_valueColours;
	}
}
