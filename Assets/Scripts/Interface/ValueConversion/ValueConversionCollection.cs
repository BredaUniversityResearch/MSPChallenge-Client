using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MSP2050/ValueConversion/Collection")]
public class ValueConversionCollection: ScriptableObject
{
	public const string UNIT_WATT = "W";

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

	public ConvertedUnit ConvertUnit(float currentValue, string unit)
	{
		ValueConversionUnit converter = null;
		if (!string.IsNullOrEmpty(unit))
		{
			conversionUnits.TryGetValue(unit, out converter);
		}

		return converter != null? converter.ConvertUnit(currentValue) : new ConvertedUnit(currentValue, unit, 0);
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
}