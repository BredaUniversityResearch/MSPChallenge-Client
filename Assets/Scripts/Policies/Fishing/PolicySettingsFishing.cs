using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicySettingsFishing : APolicyData
	{
		public bool all_country_approval = true;
		public FleetInfo fleet_info;
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
	}
}
