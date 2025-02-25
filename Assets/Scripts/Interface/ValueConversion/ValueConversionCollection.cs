﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	[CreateAssetMenu(menuName = "MSP2050/ValueConversion/Collection")]
	public class ValueConversionCollection: ScriptableObject
	{
		public const string UNIT_WATT = "W";
		public const string UNIT_KM2 = "km2";

		[SerializeField]
		private ValueConversionUnit[] availableConversionUnits = null;

		private Dictionary<string, ValueConversionUnit> conversionUnits = new Dictionary<string, ValueConversionUnit>();

		private void OnEnable()
		{
			foreach (ValueConversionUnit unit in availableConversionUnits)
			{
				conversionUnits.Add(unit.BaseUnit, unit);
			}
		}

		public ConvertedUnitFloat ConvertUnit(float currentValue, string unit)
		{
			ValueConversionUnit converter = null;
			if (!string.IsNullOrEmpty(unit))
			{
				conversionUnits.TryGetValue(unit, out converter);
			}

			return converter != null? converter.ConvertUnit(currentValue) : new ConvertedUnitFloat(currentValue, unit, 0);
		}

		public ConvertedUnitLong ConvertUnit(long currentValue, string unit)
		{
			ValueConversionUnit converter = null;
			if (!string.IsNullOrEmpty(unit))
			{
				conversionUnits.TryGetValue(unit, out converter);
			}

			return converter != null ? converter.ConvertUnit(currentValue) : new ConvertedUnitLong(currentValue, unit, 0);
		}

		public void ParseUnit(string input, string baseUnitIdentifier, out float amount)
		{
			amount = 0.0f;

			ValueConversionUnit converter = null;
			if (!string.IsNullOrEmpty(baseUnitIdentifier))
			{
				conversionUnits.TryGetValue(baseUnitIdentifier, out converter);
			}

			if (converter != null)
			{
				converter.ParseUnit(input, out amount);
			}
		}

		public void ParseUnit(string input, string baseUnitIdentifier, out long amount)
		{
			amount = 0l;

			ValueConversionUnit converter = null;
			if (!string.IsNullOrEmpty(baseUnitIdentifier))
			{
				conversionUnits.TryGetValue(baseUnitIdentifier, out converter);
			}

			if (converter != null)
			{
				converter.ParseUnit(input, out amount);
			}
		}

		public bool TryGetConverter(string unit, out ValueConversionUnit result)
		{
			if (unit == null)
			{
				result = null;
				return false;
			}
			return conversionUnits.TryGetValue(unit, out result);
		}
	}
}