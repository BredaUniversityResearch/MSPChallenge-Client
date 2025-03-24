using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class GraphDataSteppedPattern: GraphDataStepped
	{
		//Patterns
		public List<string> m_patternNames;
		public List<int> m_patternIndices; //Pattern indices for all values per step
		public int m_patternSetsPerStep; //Number of pattern sets per step, for grouping
		public bool m_overLapPatternSet;

		//Note: m_absoluteCategoryIndices behaves different with patterns

		public override string GetDisplayName(int a_categoryIndex)
		{
			return m_selectedDisplayIDs[m_absoluteCategoryIndices[a_categoryIndex]];
		}

		//public override Color GetBarDisplayColor(int a_categoryIndex)
		//{
		//	return DashboardManager.Instance.ColourList.GetColour(m_absoluteCategoryIndices[a_categoryIndex]);
		//}

		public override Color GetLegendDisplayColor(int a_categoryIndex)
		{
			return DashboardManager.Instance.ColourList.GetColour(a_categoryIndex);
		}

		public override bool UsesPattern => true;
		public override bool OverLapPatternSet => m_overLapPatternSet;
		public override List<string> PatternNames => m_patternNames;

		public override int PatternSetsPerStep => m_patternSetsPerStep;

		public override int GetPatternIndex(int a_categoryIndex)
		{
			return m_patternIndices[a_categoryIndex];
		}
	}
}

