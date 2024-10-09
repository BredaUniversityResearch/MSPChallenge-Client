
//Full set of fishing distributions calculated by accumulating the delta sets of different plans.

using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class FishingDistributionSet
	{
		private Dictionary<int, Dictionary<int, float>> fishingValues; //gear_type, country_id, effort

		public FishingDistributionSet(FishingDistributionDelta initialValues)
		{
			if (initialValues == null)
			{
				Debug.LogError("No initial values (FishingDistributionDelta) available. Please setup initial plans with \"fishing\" -> \"initialFishingDistribution\".");
				return;
			}
			fishingValues = new Dictionary<int, Dictionary<int, float>>(initialValues.FleetCount);
			foreach (KeyValuePair<int, Dictionary<int, float>> fleetValues in initialValues.GetValuesByGear())
			{
				Dictionary<int, float> distributionValues = new Dictionary<int, float>(fleetValues.Value);
				fishingValues.Add(fleetValues.Key, distributionValues);
			}
		}

		public void ApplyValues(FishingDistributionDelta deltaSet)
		{
			foreach (KeyValuePair<int, Dictionary<int, float>> values in deltaSet.GetValuesByGear())
			{
				Dictionary<int, float> target = fishingValues[values.Key];
				foreach (KeyValuePair<int, float> value in values.Value)
				{
					target[value.Key] = value.Value;
				}
			}
		}

		public IEnumerable<KeyValuePair<int, Dictionary<int, float>>> GetValues()
		{
			return fishingValues;
		}

		public Dictionary<int, float> FindValuesForGear(int a_gearType)
		{
			Dictionary<int, float> result;
			fishingValues.TryGetValue(a_gearType, out result);
			return result;
		}
	}
}
