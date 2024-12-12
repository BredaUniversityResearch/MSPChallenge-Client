using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class GraphDataStepped
	{
		public List<float?[]> m_steps; //Step (time step or distribution), value per category

		public ValueConversionUnit m_unit;
		public int m_unitIndex;
		public int m_unitPower;
		public float m_graphMin; //Set by value axis
		public float m_graphRange;
		public List<string> m_stepNames;
		public string[] m_categoryNames;
		public List<int> m_absoluteCategoryIndices;

		public string FormatValue(float a_value)
		{
			if(m_unit == null)
				return (a_value * Mathf.Pow(10, -m_unitPower)).ToString("0.#####");
			return (a_value * Mathf.Pow(10, -m_unitPower) * m_unit.GetUnitEntrySize(m_unitIndex)).ToString("0.#####");
		}

		public string GetUnitString()
		{
			if (m_unit == null)
			{
				if (m_unitPower != 0)
					return $"e{m_unitPower} ?";
				return "?";
			}
			if (m_unitPower != 0)
				return $"e{m_unitPower} {m_unit.GetUnitStringForUnitIndex(m_unitIndex)}";
			return m_unit.GetUnitStringForUnitIndex(m_unitIndex);
		}
	}
}

