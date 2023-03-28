using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public interface IConvertedUnit
	{
		public int AmountDecimalPlaces { get; }
		public string Unit { get; }

		public string FormatAsString();
		public string FormatValue();
	}
}