using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public struct ConvertedUnitLong
	{
		private double amount;		//e.g. 1.56
		private int amountDecimalPlaces;
		private string unit;        //e.g. kg

		public int AmountDecimalPlaces => amountDecimalPlaces;
		public string Unit => unit;

		public ConvertedUnitLong(double amount, string unit, int amountDecimalPlaces)
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
			bool sign = amount >= 0D;
			double formatValue = Math.Abs(amount);
			int power = 0;

			if (formatValue >= 10000D)
			{
				formatValue /= 10000D;
				power += 4;
				while (formatValue >= 10D)
				{
					formatValue /= 10D;
					power++;
				}
			}
			else if (formatValue != 0D)
			{
				while (formatValue <= 0.1D)
				{
					formatValue *= 10D;
					power--;
				}
			}

			if (power == 0)
			{
				int decimals = 2;
				if (formatValue > 1000D)
					decimals = 0;
				else if (formatValue > 100D)
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
}