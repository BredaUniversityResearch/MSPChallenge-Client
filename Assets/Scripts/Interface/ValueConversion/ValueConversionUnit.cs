using System;
using System.Globalization;
using UnityEngine;

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

	public ConvertedUnit ConvertUnit(float value)
	{
        float absValue = Mathf.Abs(value);

		if (value == 0)
		{
			return new ConvertedUnit(0.0f, baseUnit, 0);
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

		return new ConvertedUnit(value / unitConversion.unitSize, unitConversion.unitPostfix, decimalPlaces);
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