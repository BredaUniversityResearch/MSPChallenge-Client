using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicySettingsFishing : APolicyData
	{
		public bool all_country_approval = true;
		public FleetInfo fleet_info;
		public float default_fishing_effort = 0f;
		public float fishing_display_scale = 100f;
	}

	public class FleetInfo
	{
		public string[] gear_types;
		public CountryFleetInfo[] fleets;
	}

	public class CountryFleetInfo
	{
		public int gear_type;
		public int country_id;
		public InitialFishingDistribution[] initial_fishing_distribution;
	}

	public class InitialFishingDistribution
	{
		public int country_id;
		public float effort_weight;
	}
}
