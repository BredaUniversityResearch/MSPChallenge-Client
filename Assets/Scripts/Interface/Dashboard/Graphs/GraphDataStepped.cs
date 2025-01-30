﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class GraphDataStepped
	{
		//Actual data: Step (time step or distribution), value per category
		public List<float?[]> m_steps; 

		//Unit information
		public ValueConversionUnit m_unit;
		public string m_undefinedUnit;
		public int m_unitIndex;
		public int m_unitPower;
		public int m_unitEOffset;

		//Data display range
		public float m_graphMin; //Set by value axis
		public float m_graphRange;

		public List<string> m_stepNames;
		public List<int> m_absoluteCategoryIndices; //Used to keep colours consistent when values are hidden
		public List<int> m_valueCountries; //Country of all the selected values
		public List<string> m_selectedDisplayIDs;

		public string FormatValue(float a_value)
		{
			return (a_value * Mathf.Pow(10, -m_unitPower)).ToString("0.#####");
		}

		public string GetUnitString()
		{
			if (m_unit == null)
			{
				if (m_unitPower != 0)
					return $"e{m_unitPower} {m_undefinedUnit}";
				return "N/A";
			}
			if (m_unitPower - m_unitEOffset != 0)
				return $"e{m_unitPower - m_unitEOffset} {m_unit.GetUnitStringForUnitIndex(m_unitIndex)}";
			return m_unit.GetUnitStringForUnitIndex(m_unitIndex);
		}
	}
}

