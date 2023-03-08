using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class FishingDistributionDelta
	{
		public const float MaxSummedFishingValue = 1.0f;

		private Dictionary<string, Dictionary<int, float>> m_values = new Dictionary<string, Dictionary<int, float>>(); //per fishing type, a dictionary of countries and their expected values

		public int FleetCount => m_values.Count;

		public FishingDistributionDelta()
		{
		}

		/// <summary>
		/// Returns a fishing distribution set to the given objects. If an empty list is given, it creates a basic distribution.
		/// </summary>
		public FishingDistributionDelta(List<FishingObject> a_objects)
		{
			LoadDistribution(a_objects);
		}

		public Dictionary<int, float> FindValuesForFleet(string a_fleetName)
		{
			Dictionary<int, float> result;
			m_values.TryGetValue(a_fleetName, out result);
			return result;
		}

		public IEnumerable<KeyValuePair<string, Dictionary<int, float>>> GetValuesByFleet()
		{
			return m_values;
		}

		public bool HasDistributionValues()
		{
			return m_values.Count > 0;
		}

		public void Clear()
		{
			m_values.Clear();
		}

		public bool HasFinishingValue(string a_fleetName)
		{
			return m_values.ContainsKey(a_fleetName);
		}

		public void SetFishingValue(string a_fleetName, int a_country, float a_fishingValue)
		{
			Dictionary<int, float> fleetValues;
			if (!m_values.TryGetValue(a_fleetName, out fleetValues))
			{
				fleetValues = new Dictionary<int, float>(SessionManager.Instance.TeamCount);
				m_values.Add(a_fleetName, fleetValues);
			}

			fleetValues[a_country] = a_fishingValue;
		}

		private void LoadDistribution(List<FishingObject> a_deltaValues)
		{
			m_values.Clear();
			if (a_deltaValues != null)
			{
				foreach (FishingObject obj in a_deltaValues)
				{
					SetFishingValue(obj.type, obj.country_id, obj.amount);
				}
			}
		}

		public void SubmitToServer(string a_planId, BatchRequest a_batch)
		{
			List<FishingObject> valuesToSubmit = new List<FishingObject>(32);

			foreach (KeyValuePair<string, Dictionary<int, float>> fishingType in m_values)
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
			dataObject.Add("plan", a_planId);
			dataObject.Add("fishing_values", JToken.FromObject(valuesToSubmit));

			a_batch.AddRequest(Server.SendFishingAmount(), dataObject, BatchRequest.BatchGroupPlanChange);
		}

		public FishingDistributionDelta Clone()
		{
			FishingDistributionDelta result = new FishingDistributionDelta();
			foreach (var fleetValues in m_values)
			{
				result.m_values.Add(fleetValues.Key, new Dictionary<int, float>(fleetValues.Value));
			}

			return result;
		}
	}
}
