using System;
using UnityEngine;

namespace KPI
{
	public class KPIValue
	{
		public const int CountryGlobal = 0; //Applies to each country, but is grouped under a global color.
		public const int CountrySpecific = -1; //Applies to each country, but is grouped under each country's specific color.

		public readonly string owningCategoryName;
		public readonly string name;			//Internal name used for lookups.
		public readonly string displayName;		//Display name that is shown to the player.
		public readonly string unit;
		public readonly Color graphColor;
		public readonly int targetCountryId;
		private readonly float[] kpiValuesPerMonth;

		public event Action<KPIValue> OnValueUpdated;

		public int MostRecentMonth
		{
			get;
			private set;
		}

		public KPIValue(string owningCategory, int numberOfKpiMonths, string valueName, string valueUnit, Color valueGraphColor, string valueDisplayName, int targetCountryId)
		{
			owningCategoryName = owningCategory;
			name = valueName;
			displayName = (!string.IsNullOrEmpty(valueDisplayName)) ? valueDisplayName : valueName;
			unit = valueUnit;
			graphColor = valueGraphColor;
			this.targetCountryId = targetCountryId;
			
			kpiValuesPerMonth = new float[numberOfKpiMonths];
			MostRecentMonth = 0;
		}

		public void UpdateValue(int monthId, float value)
		{
			if (monthId >= 0 && monthId < kpiValuesPerMonth.Length)
			{
				kpiValuesPerMonth[monthId] = value;
				if (monthId > MostRecentMonth)
				{
					MostRecentMonth = monthId;
				}

				if (OnValueUpdated != null)
				{
					OnValueUpdated(this);
				}
			}
			else
			{
				Debug.LogWarning("Received KPI value (" + name + ") for month " + monthId + " which is out of bounds (0, " + kpiValuesPerMonth.Length + ") and has been discarded");
			}
		}

		public float GetKpiValueForMonth(int monthId)
		{
			if (monthId >= 0 && monthId <= kpiValuesPerMonth.Length)
			{
				if (monthId > MostRecentMonth)
				{
					return kpiValuesPerMonth[MostRecentMonth];
				}
				return kpiValuesPerMonth[monthId];
			}
			else
			{
				return -1.0f;
			}
		}

		public float GetKpiValueForMostRecentMonth()
		{
			return GetKpiValueForMonth(MostRecentMonth);
		}

		//Returns values in unit values (Returns 2.0 at toValue == 2.0 * fromValue)
		private static float CalculateRelativePercentageUnit(float fromValue, float toValue, out string prefixSymbol)
		{
			prefixSymbol = "";
			if (fromValue == 0.0f)
			{
				return 0;
			}

			float changePercentage = ((toValue - fromValue) / Math.Abs(fromValue));

			if (changePercentage > 0.0f)
			{
				prefixSymbol = "+";
				if (changePercentage > 10.0f)
				{
					//Cap at 1000 %
					changePercentage = 10.0f;
					prefixSymbol = ">+";
				}
			}

			return changePercentage;
		}

		public static string FormatRelativePercentage(float startingValue, float val)
		{
			string prefixSymbol;
			float relativePercentageUnit = CalculateRelativePercentageUnit(startingValue, val, out prefixSymbol);

			return string.Format("{0}{1:N2}%", prefixSymbol, relativePercentageUnit * 100.0f);
		}
	};
}