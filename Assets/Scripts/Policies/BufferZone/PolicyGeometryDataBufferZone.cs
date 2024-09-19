using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class PolicyGeometryDataBufferZone : APolicyData
	{
		public Dictionary<int, Dictionary<int, Months>> fleets; //gear ids, country ids, months
		public float radius;

		public PolicyGeometryDataBufferZone()
		{
			policy_type = PolicyManager.BUFFER_ZONE_POLICY_NAME;
			fleets = new Dictionary<int, Dictionary<int, Months>>();
			radius = 0f;
		}

		public PolicyGeometryDataBufferZone(Dictionary<int, Dictionary<int, Months>> a_fleets, float a_radius)
		{
			policy_type = PolicyManager.BUFFER_ZONE_POLICY_NAME;
			fleets = a_fleets;
			radius = a_radius;
		}

		public PolicyGeometryDataBufferZone(string a_jsonData)
		{
			if(string.IsNullOrEmpty(a_jsonData))
			{
				policy_type = PolicyManager.BUFFER_ZONE_POLICY_NAME;
				fleets = new Dictionary<int, Dictionary<int, Months>>();
				radius = 0f;
				return;
			}

			//Convert from server format into client format
			BufferZoneData data = JsonConvert.DeserializeObject<BufferZoneData>(a_jsonData);
			radius = data.radius;
			fleets = new Dictionary<int, Dictionary<int, Months>>();
			foreach(FleetClosureData item in data.items)
			{
				foreach(int fleetId in item.fleets)
				{
					CountryFleetInfo countryFleetInfo = PolicyLogicFishing.Instance.GetFleetInfo(fleetId);
					if (fleets.TryGetValue(countryFleetInfo.gear_type, out var countryMonths))
					{
						countryMonths.Add(countryFleetInfo.country_id, item.months);
					}
					else
					{
						fleets.Add(countryFleetInfo.gear_type, new Dictionary<int, Months>() { { countryFleetInfo.country_id, item.months} });
					}
				}
			}
		}

		public override string GetJson()
		{
			//Convert from client format into server format
			BufferZoneData data = new BufferZoneData();
			data.items = new List<FleetClosureData>();
			data.radius = this.radius;

			//Group fleets based on months selected
			foreach(var kvp in fleets)
			{
				foreach(var countryMonth in kvp.Value)
				{
					int fleetId = PolicyLogicFishing.Instance.GetFleetId(countryMonth.Key, kvp.Key);
					bool existing = false;
					foreach(FleetClosureData item in data.items)
					{
						if(item.months == countryMonth.Value)
						{
							item.fleets.Add(fleetId);
							existing = true;
							break;
						}
					}
					if(!existing)
					{
						data.items.Add(new FleetClosureData() { fleets = new List<int>() { fleetId }, months = countryMonth.Value });
					}
				}
			}
			return JsonConvert.SerializeObject(data);
		}

		public override bool ContentIdentical(APolicyData a_other)
		{
			PolicyGeometryDataBufferZone other = (PolicyGeometryDataBufferZone)a_other;
			if (other == null || fleets.Count != other.fleets.Count || Mathf.Abs(radius - other.radius) > 0.001f)
				return false;
			foreach(var kvp in fleets)
			{
				if (other.fleets.TryGetValue(kvp.Key, out var otherValue))
				{
					if (kvp.Value.Count != otherValue.Count)
						return false;

					foreach(var countryBan in kvp.Value)
					{
						if (otherValue.TryGetValue(countryBan.Key, out var otherMonths))
						{
							if (countryBan.Value != otherMonths)
								return false;
						}
						else
							return false;
					}
				}
				else if(kvp.Value.Count > 0)
					return false;
			}
			return true;
		}

		public PolicyGeometryDataBufferZone GetValueCopy()
		{
			Dictionary<int, Dictionary<int, Months>> fleetsCopy = new Dictionary<int, Dictionary<int, Months>>();
			foreach(var kvp in fleets)
			{
				Dictionary<int, Months> newValue = new Dictionary<int, Months>();

				foreach(var countryBan in kvp.Value)
				{
					newValue[countryBan.Key] = countryBan.Value;
				}
				fleetsCopy.Add(kvp.Key, newValue);
			}
			return new PolicyGeometryDataBufferZone(fleetsCopy, radius);
		}

		private class BufferZoneData: APolicyData
		{
			public BufferZoneData()
			{
				policy_type = PolicyManager.BUFFER_ZONE_POLICY_NAME;
			}
			public float radius;
			public List<FleetClosureData> items;
			public float pressure = 0f;
		}
	}

	public class FleetClosureData
	{
		public List<int> fleets;
		public Months months;
	}
}
