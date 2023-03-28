using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	[CreateAssetMenu(menuName = "MSP2050/ValueConversion/Unit", fileName = "NewConversionUnit")]
	public class ValueConversionUnit: ScriptableObject
	{
		private static UnitEntry[] defaultMetricUnits = new UnitEntry[]
		{
			new UnitEntry { unitPostfix = "k", unitSize = 1000 },
			new UnitEntry { unitPostfix = "M", unitSize = 1000000 },
			new UnitEntry { unitPostfix = "G", unitSize = 1000000000 }
		};

		[Flags]
		private enum EPostfixMatchMode
		{
			MatchExact = 1 << 0,
			StartsWith = 1 << 1,
			IgnoreCase = 1 << 2
		}

		[Serializable]
		private class UnitEntry
		{
			public string unitPostfix = "";
			public float unitSize = 1;
		}

		[SerializeField]
		private string baseUnit = "SOME_UNIT_TYPE";
		[SerializeField]
		private string baseUnitFormat = "SOME_UNIT_TYPE";

		public string BaseUnit
		{
			get
			{
				return baseUnit;
			}
		}

		[SerializeField]
		private UnitEntry[] conversionUnits = new UnitEntry[0];

		[SerializeField]
		private int decimalPlaces = 2;

		public ConvertedUnitFloat ConvertUnit(float value)
		{
			float absValue = Mathf.Abs(value);

			if (value == 0)
			{
				return new ConvertedUnitFloat(0.0f, baseUnitFormat, 0);
			}

			UnitEntry unitConversion = conversionUnits[0];
			for (int i = conversionUnits.Length - 1; i >= 0 ; --i)
			{
				if (absValue >= conversionUnits[i].unitSize)
				{
					unitConversion = conversionUnits[i];
					break;
				}
			}

			return new ConvertedUnitFloat(value / unitConversion.unitSize, unitConversion.unitPostfix, decimalPlaces);
		}

		public ConvertedUnitLong ConvertUnit(long value)
		{
			long absValue = Math.Abs(value);

			if (value == 0L)
			{
				return new ConvertedUnitLong(0D, baseUnitFormat, 0);
			}

			UnitEntry unitConversion = conversionUnits[0];
			for (int i = conversionUnits.Length - 1; i >= 0; --i)
			{
				if (absValue >= conversionUnits[i].unitSize)
				{
					unitConversion = conversionUnits[i];
					break;
				}
			}

			return new ConvertedUnitLong((double)value / (double)unitConversion.unitSize, unitConversion.unitPostfix, decimalPlaces);
		}

		public void ParseUnit(string input, out float amount)
		{
			int separatorIndex = input.LastIndexOfAny("0123456789".ToCharArray());

			string numberString = input.Substring(0, separatorIndex + 1).Trim();
			string unitString = input.Substring(separatorIndex + 1).Trim();

			if (float.TryParse(numberString, Localisation.FloatNumberStyle, Localisation.NumberFormatting, out amount))
			{
				UnitEntry unit = FindUnitEntryForPostfix(unitString, conversionUnits, EPostfixMatchMode.MatchExact | EPostfixMatchMode.IgnoreCase);
				if (unit != null)
				{
					amount *= unit.unitSize;
				}
				else
				{
					unit = FindUnitEntryForPostfix(unitString, defaultMetricUnits, EPostfixMatchMode.StartsWith | EPostfixMatchMode.IgnoreCase);
					if (unit != null)
					{
						amount *= unit.unitSize;
					}
				}
			}
		}

		public void ParseUnit(string input, out long amount)
		{
			int separatorIndex = input.LastIndexOfAny("0123456789".ToCharArray());

			string numberString = input.Substring(0, separatorIndex + 1).Trim();
			string unitString = input.Substring(separatorIndex + 1).Trim();
			double tempResult;

			if (double.TryParse(numberString, Localisation.FloatNumberStyle, Localisation.NumberFormatting, out tempResult))
			{
				UnitEntry unit = FindUnitEntryForPostfix(unitString, conversionUnits, EPostfixMatchMode.MatchExact | EPostfixMatchMode.IgnoreCase);
				if (unit != null)
				{
					tempResult *= (double)unit.unitSize;
				}
				else
				{
					unit = FindUnitEntryForPostfix(unitString, defaultMetricUnits, EPostfixMatchMode.StartsWith | EPostfixMatchMode.IgnoreCase);
					if (unit != null)
					{
						tempResult *= (double)unit.unitSize;
					}
				}
			}
			amount = (long)tempResult;
		}

		private UnitEntry FindUnitEntryForPostfix(string inputPostfix, UnitEntry[] entries, EPostfixMatchMode matchMode)
		{
			StringComparison comparison;
			if ((matchMode & EPostfixMatchMode.IgnoreCase) == EPostfixMatchMode.IgnoreCase)
			{
				comparison = StringComparison.InvariantCultureIgnoreCase;
			}
			else
			{
				comparison = StringComparison.InvariantCulture;
			}

			UnitEntry result = null;
			foreach (UnitEntry unit in entries)
			{
				if ((((matchMode & EPostfixMatchMode.MatchExact) == EPostfixMatchMode.MatchExact) && string.Compare(inputPostfix, unit.unitPostfix, comparison) == 0) ||
				    (((matchMode & EPostfixMatchMode.StartsWith) == EPostfixMatchMode.StartsWith) && inputPostfix.StartsWith(unit.unitPostfix, comparison)))
				{
					result = unit;
					break;
				}
			}

			return result;
		}
	}
}