using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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

	public static class MonthsMethods
	{
		public const int AllMonthsValue = 4095;
		public static bool MonthSet(this Months a_months, int a_check)
		{
			return ((int)a_months & (1 << a_check)) != 0;
		}
		public static bool AllMonths(this Months a_months)
		{
			return (int)a_months >= AllMonthsValue;
		}
	}

	public class PolicyGeometryDataSeasonalClosure : APolicyData
	{
		public Dictionary<int, Dictionary<int, Months>> fleets; //gear ids, country ids, months
		public float pressure = 0f;

		public PolicyGeometryDataSeasonalClosure()
		{
			policy_type = PolicyManager.SEASONAL_CLOSURE_POLICY_NAME;
			fleets = new Dictionary<int, Dictionary<int, Months>>();
		}

		public PolicyGeometryDataSeasonalClosure(Dictionary<int, Dictionary<int, Months>> a_fleets)
		{
			policy_type = PolicyManager.SEASONAL_CLOSURE_POLICY_NAME;
			fleets = a_fleets;
		}

		public PolicyGeometryDataSeasonalClosure(string a_jsonData)
		{
			if(string.IsNullOrEmpty(a_jsonData))
			{
				policy_type = PolicyManager.SEASONAL_CLOSURE_POLICY_NAME;
				fleets = new Dictionary<int, Dictionary<int, Months>>();
				return;
			}

			//Convert from server format into client format
			SeasonalClosureData data = JsonConvert.DeserializeObject<SeasonalClosureData>(a_jsonData);
			fleets = new Dictionary<int, Dictionary<int, Months>>();
			foreach (FleetClosureData item in data.items)
			{
				foreach (int fleetId in item.fleets)
				{
					CountryFleetInfo countryFleetInfo = PolicyLogicFishing.Instance.GetFleetInfo(fleetId);
					if (fleets.TryGetValue(countryFleetInfo.gear_type, out var countryMonths))
					{
						countryMonths.Add(countryFleetInfo.country_id, item.months);
					}
					else
					{
						fleets.Add(countryFleetInfo.gear_type, new Dictionary<int, Months>() { { countryFleetInfo.country_id, item.months } });
					}
				}
			}
		}

		public override string GetJson()
		{
			//Convert from client format into server format
			SeasonalClosureData data = new SeasonalClosureData();
			data.items = new List<FleetClosureData>();

			//Group fleets based on months selected
			foreach (var kvp in fleets)
			{
				foreach (var countryMonth in kvp.Value)
				{
					int fleetId = PolicyLogicFishing.Instance.GetFleetId(countryMonth.Key, kvp.Key);
					bool existing = false;
					foreach (FleetClosureData item in data.items)
					{
						if (item.months == countryMonth.Value)
						{
							item.fleets.Add(fleetId);
							existing = true;
							break;
						}
					}
					if (!existing)
					{
						data.items.Add(new FleetClosureData() { fleets = new List<int>() { fleetId }, months = countryMonth.Value });
					}
				}
			}
			return JsonConvert.SerializeObject(data);
		}

		public override bool ContentIdentical(APolicyData a_other)
		{
			PolicyGeometryDataSeasonalClosure other = (PolicyGeometryDataSeasonalClosure)a_other;
			if (other == null || fleets.Count != other.fleets.Count)
				return false;
			foreach (var kvp in fleets)
			{
				if (other.fleets.TryGetValue(kvp.Key, out var otherValue))
				{
					if (kvp.Value.Count != otherValue.Count)
						return false;

					foreach (var countryBan in kvp.Value)
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
				else if (kvp.Value.Count > 0)
					return false;
			}
			return true;
		}

		public PolicyGeometryDataSeasonalClosure GetValueCopy()
		{
			Dictionary<int, Dictionary<int, Months>> fleetsCopy = new Dictionary<int, Dictionary<int, Months>>();
			foreach (var kvp in fleets)
			{
				Dictionary<int, Months> newValue = new Dictionary<int, Months>();

				foreach (var countryBan in kvp.Value)
				{
					newValue[countryBan.Key] = countryBan.Value;
				}
				fleetsCopy.Add(kvp.Key, newValue);
			}
			return new PolicyGeometryDataSeasonalClosure(fleetsCopy);
		}

		private class SeasonalClosureData
		{
			public List<FleetClosureData> items;
		}
	}
}
