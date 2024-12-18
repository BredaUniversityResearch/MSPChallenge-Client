using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyUpdateEcoGearPlan : APolicyData
	{
		public List<EcoGearSetting> items;
		public float pressure = 0f;
	}

	public class EcoGearSetting
	{
		public int[] fleets;
		public bool enabled;
	}
}
