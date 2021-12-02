using UnityEngine;
using System;

public struct ConvertedUnit
{
	public readonly float amount;		//e.g. 1.56
	private readonly int amountDecimalPlaces;
	public readonly string unit;		//e.g. kg

	public ConvertedUnit(float amount, string unit, int amountDecimalPlaces)
	{
		this.amount = amount;
		this.unit = unit;
		this.amountDecimalPlaces = amountDecimalPlaces;
	}

	public string FormatAsString()
	{
		return string.Format("{0} {1}", FormatValue(), unit);
	}

	public string FormatValue()
	{
        //Determine power
        bool sign = amount >= 0;
        float formatValue = Mathf.Abs(amount);
        int power = 0;

        if (formatValue >= 10000f)
        {
            formatValue /= 10000f;
            power += 4;
            while (formatValue >= 10f)
            {
                formatValue /= 10f;
                power++;
            }
        }
        else if (formatValue != 0)
        {
            while (formatValue <= 0.1f)
            {
                formatValue *= 10f;
                power--;
            }
        }

        if (power == 0)
        {
            int decimals = 2;
            if (formatValue > 1000f)
                decimals = 0;
            else if (formatValue > 100f)
                decimals = 1;
            string result = amount.ToString("N" + Math.Min(decimals, amountDecimalPlaces), Localisation.NumberFormatting);
			return result;
		}
        else
        {
            if (!sign)
                formatValue = -formatValue;
            return formatValue.ToString("F2", Localisation.NumberFormatting) + "e" + power;
        }
	}
}