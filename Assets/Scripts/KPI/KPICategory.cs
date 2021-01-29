using System.Collections.Generic;
using UnityEngine;

namespace KPI
{
	public class KPICategory: KPIValue
	{
		public readonly EKPICategoryValueType categoryValueType;
		public readonly EKPIValueColorScheme kpiValueColorScheme = EKPIValueColorScheme.DefinedColor;
		private List<KPIValue> childValues = new List<KPIValue>(16);

		public KPICategory(int numberOfKpiMonths, string categoryName, string categoryUnit, Color categoryColor, EKPICategoryValueType categoryValueType, string categoryDisplayName, EKPIValueColorScheme colorScheme, int countryID)
			: base(categoryName, numberOfKpiMonths, categoryName, categoryUnit, categoryColor, categoryDisplayName, countryID)
		{
			this.categoryValueType = categoryValueType;
			this.kpiValueColorScheme = colorScheme;
		}

		public void AddChildValue(KPIValue child)
		{
			childValues.Add(child);
		}

		public IEnumerable<KPIValue> GetChildValues()
		{
			return childValues;
		}

		public int GetChildValueCount()
		{
			return childValues.Count;
		}
	};
}