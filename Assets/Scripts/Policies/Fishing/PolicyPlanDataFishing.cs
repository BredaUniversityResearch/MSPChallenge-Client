using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyPlanDataFishing : APolicyPlanData
	{
		public FishingDistributionDelta fishingDistributionDelta;

		public PolicyPlanDataFishing(APolicyLogic a_logic) : base(a_logic)
		{ }
	}
}