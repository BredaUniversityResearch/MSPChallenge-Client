using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	[Flags]
	public enum Months
	{
		None = 0,
		January = 1,
		February = 2,
		March = 4,
		April = 8,
		May = 16,
		June = 32,
		July = 64,
		August = 128,
		September = 256,
		October = 512,
		November = 1024,
		December = 2048,
	}

	public class PolicyGeometryDataSeasonalClosure : APolicyData
	{
		public List<string> fleets;
		public Months months;

		public override bool ContentIdentical(APolicyData a_other)
		{
			PolicyGeometryDataSeasonalClosure other = (PolicyGeometryDataSeasonalClosure)a_other;
			if (other == null || other.months != months || fleets.Count != other.fleets.Count)
				return false;
			for(int i = 0; i < fleets.Count; i++)
			{
				if (fleets[i] != other.fleets[i])
					return false;
			}
			return true;
		}
	}
}
