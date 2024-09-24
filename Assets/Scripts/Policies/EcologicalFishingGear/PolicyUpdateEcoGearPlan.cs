using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyUpdateEcoGearPlan : APolicyData
	{
		public List<EcoGearSetting> items;
	}

	public class EcoGearSetting
	{
		public int[] fleets;
		public bool enabled;
	}
}
