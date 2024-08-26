using System;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyPlanDataEcoGear : APolicyPlanData
	{
		public Dictionary<int, Dictionary<int, bool>> m_values; //country, gear_type, eco_gear

		public PolicyPlanDataEcoGear(APolicyLogic a_logic) : base(a_logic)
		{
		}

		public void AddUnchangedValues(Dictionary<int, Dictionary<int, bool>> a_result)
		{
			if (m_values == null)
				return;
			foreach(var countryVal in m_values)
			{
				Dictionary<int, bool> resultFleetVal;
				if (!a_result.TryGetValue(countryVal.Key, out resultFleetVal))
				{
					resultFleetVal = new Dictionary<int, bool>();
					a_result.Add(countryVal.Key, resultFleetVal);
				}

				foreach(var fleetVal in countryVal.Value)
				{
					if(!resultFleetVal.ContainsKey(fleetVal.Key))
						resultFleetVal[fleetVal.Key] = fleetVal.Value;
				}
			}
		}
	}
}
