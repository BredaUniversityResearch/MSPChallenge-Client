using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyUpdateFishingPlan : APolicyData
	{
		public List<FleetFishingEffort> fishing;
		public float pressure = 0f;
	}

	public class FleetFishingEffort
	{
		public int country_id;
		public int gear_type;
		public float effort_weight;
	}
}