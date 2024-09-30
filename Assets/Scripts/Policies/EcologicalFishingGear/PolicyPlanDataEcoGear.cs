using System;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyPlanDataEcoGear : APolicyPlanData
	{
		public Dictionary<int, bool> m_values; //fleet_id, eco_gear
		public float pressure = 0f;

		public PolicyPlanDataEcoGear(APolicyLogic a_logic) : base(a_logic)
		{
			m_values = new Dictionary<int, bool>();
		}

		public void AddUnchangedValues(Dictionary<int, bool> a_result)
		{
			if (m_values == null)
				return;
			foreach(var fleetVal in m_values)
			{
				if (!a_result.ContainsKey(fleetVal.Key))
				{
					a_result[fleetVal.Key] = fleetVal.Value;
				}
			}
		}
	}
}
