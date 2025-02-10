using System.Collections;
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
		public string m_undefinedUnit;	//Unit name used when unit string does not have a defined VCU
		public int m_unitIndex;			//Index of selected unitEntry within VCU
		public int m_scalePower;		//Power to be used for value strings, determined by axis
		public int m_unitEOffset;		//Offset from m_scalePower due to unit scale, e.g. GW = 9

		//Data display range
		public float m_graphMin; //Set by value axis
		public float m_graphRange;

		public List<string> m_stepNames;
		public List<int> m_absoluteCategoryIndices; //Used to keep colours consistent when values are hidden, behaves different with patterns
		public List<int> m_valueCountries;			//Country of all the selected values
		public List<string> m_selectedDisplayIDs;

		public string FormatValue(float a_value)
		{
			return (a_value * Mathf.Pow(10, -m_scalePower)).ToString("0.#####");
		}

		public string GetUnitString()
		{
			if (m_unit == null)
			{
				if (m_scalePower != 0)
					return $"e{m_scalePower} {m_undefinedUnit}";
				return "N/A";
			}
			if (m_scalePower - m_unitEOffset != 0)
				return $"e{m_scalePower - m_unitEOffset} {m_unit.GetUnitStringForUnitIndex(m_unitIndex)}";
			return m_unit.GetUnitStringForUnitIndex(m_unitIndex);
		}


		public virtual string GetDisplayName(int a_categoryIndex)
		{
			return m_selectedDisplayIDs[a_categoryIndex];
		}

		public virtual Color GetBarDisplayColor(int a_categoryIndex)
		{
			return DashboardManager.Instance.ColourList.GetColour(m_absoluteCategoryIndices[a_categoryIndex]);
		}

		public virtual Color GetLegendDisplayColor(int a_categoryIndex)
		{
			return DashboardManager.Instance.ColourList.GetColour(m_absoluteCategoryIndices[a_categoryIndex]);
		}

		public virtual bool UsesPattern => false;
		public virtual bool OverLapPatternSet => false;
		public virtual List<string> PatternNames => null;

		public virtual int PatternSetsPerStep => 0;

		public virtual int GetPatternIndex(int a_categoryIndex)
		{
			return 0;
		}
	}
}

