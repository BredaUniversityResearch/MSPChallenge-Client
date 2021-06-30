
//Full set of fishing distributions calculated by accumulating the delta sets of different plans.

using System.Collections.Generic;

public class FishingDistributionSet
{
	private Dictionary<string, Dictionary<int, float>> fishingValues;

	public FishingDistributionSet(FishingDistributionDelta initialValues)
	{
		fishingValues = new Dictionary<string, Dictionary<int, float>>(initialValues.FleetCount);
		foreach (KeyValuePair<string, Dictionary<int, float>> fleetValues in initialValues.GetValuesByFleet())
		{
			Dictionary<int, float> distributionValues = new Dictionary<int, float>(fleetValues.Value);
			fishingValues.Add(fleetValues.Key, distributionValues);
		}
	}

	public void ApplyValues(FishingDistributionDelta deltaSet)
	{
		foreach (KeyValuePair<string, Dictionary<int, float>> values in deltaSet.GetValuesByFleet())
		{
			Dictionary<int, float> target = fishingValues[values.Key];
			foreach (KeyValuePair<int, float> value in values.Value)
			{
				target[value.Key] = value.Value;
			}
		}
	}

	public IEnumerable<KeyValuePair<string, Dictionary<int, float>>> GetValues()
	{
		return fishingValues;
	}

	public Dictionary<int, float> FindValuesForFleet(string fleetName)
	{
		Dictionary<int, float> result;
		fishingValues.TryGetValue(fleetName, out result);
		return result;
	}

	/*public void NormalizeValues()
	{
		foreach (KeyValuePair<string, Dictionary<int, float>> valuesByFleet in fishingValues)
		{
			float sum = 0.0f;
			foreach (KeyValuePair<int, float> valuesByTeam in valuesByFleet.Value)
			{
				sum += valuesByTeam.Value;
			}

			if (sum > 1.0f)
			{
				//Normalize categories to a 1.0 range.
				float reciprocal = 1.0f / sum;
				foreach (Team team in TeamManager.GetTeams())
				{
					float oldValue;
					if (valuesByFleet.Value.TryGetValue(team.ID, out oldValue))
					{
						valuesByFleet.Value[team.ID] = oldValue * reciprocal;
					}
				}
			}
		}
	}*/
}
