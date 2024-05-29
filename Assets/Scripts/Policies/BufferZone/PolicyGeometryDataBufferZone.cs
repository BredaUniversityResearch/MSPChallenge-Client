using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyGeometryDataBufferZone : APolicyData
	{
		public Dictionary<int, Dictionary<int, Months>> fleets; //fleet ids, country ids, months
		public float radius;

		public PolicyGeometryDataBufferZone()
		{ }

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
			return new PolicyGeometryDataBufferZone() { fleets = fleetsCopy, policy_type = this.policy_type, radius = this.radius };
		}
	}
}
