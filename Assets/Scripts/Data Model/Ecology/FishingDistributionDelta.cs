using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FishingDistributionDelta
{
	public const float MAX_SUMMED_FISHING_VALUE = 1.0f;

	private Dictionary<string, Dictionary<int, float>> values = new Dictionary<string, Dictionary<int, float>>(); //per fishing type, a dictionary of countries and their expected values

	public int FleetCount
	{
		get
		{
			return values.Count;
		}
	}

	public FishingDistributionDelta()
	{
	}

	/// <summary>
	/// Returns a fishing distribution set to the given objects. If an empty list is given, it creates a basic distribution.
	/// </summary>
	public FishingDistributionDelta(List<FishingObject> objects)
	{
		LoadDistribution(objects);
	}

	public Dictionary<int, float> FindValuesForFleet(string fleetName)
	{
		Dictionary<int, float> result;
		values.TryGetValue(fleetName, out result);
		return result;
	}

	public IEnumerable<KeyValuePair<string, Dictionary<int, float>>> GetValuesByFleet()
	{
		return values;
	}

	public bool HasDistributionValues()
	{
		return values.Count > 0;
	}

	public void Clear()
	{
		values.Clear();
	}

	public void SetFishingValue(string fleetName, int country, float fishingValue)
	{
		Dictionary<int, float> fleetValues;
		if (!values.TryGetValue(fleetName, out fleetValues))
		{
			fleetValues = new Dictionary<int, float>(TeamManager.TeamCount);
			values.Add(fleetName, fleetValues);
		}

		fleetValues[country] = fishingValue;
	}

	private void LoadDistribution(List<FishingObject> deltaValues)
	{
		values.Clear();
		if (deltaValues != null)
		{
			foreach (FishingObject obj in deltaValues)
			{
				SetFishingValue(obj.type, obj.country_id, obj.amount);
			}
		}
	}

	public void SubmitToServer(int planId, BatchRequest batch)
	{
		List<FishingObject> valuesToSubmit = new List<FishingObject>(32);

		foreach (KeyValuePair<string, Dictionary<int, float>> fishingType in values)
		{
			foreach (KeyValuePair<int, float> kvp in fishingType.Value)
			{
				valuesToSubmit.Add(new FishingObject {
						country_id = kvp.Key,
						type = fishingType.Key,
						amount = kvp.Value
				});
			}
		}

		JObject dataObject = new JObject();

		//form.AddField("fishing_values", valuesToSubmit);
		dataObject.Add("plan", planId);
		dataObject.Add("fishing_values", JToken.FromObject(valuesToSubmit));

		batch.AddRequest(Server.SendFishingAmount(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public FishingDistributionDelta Clone()
	{
		FishingDistributionDelta result = new FishingDistributionDelta();
		foreach (var fleetValues in values)
		{
			result.values.Add(fleetValues.Key, new Dictionary<int, float>(fleetValues.Value));
		}

		return result;
	}
}
