using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class FishingDistributionDelta
	{
		public const float MaxSummedFishingValue = 1.0f;

		private Dictionary<int, Dictionary<int, float>> m_values = new Dictionary<int, Dictionary<int, float>>(); //gear_type, country_id, effort

		public int FleetCount => m_values.Count;

		public FishingDistributionDelta()
		{
		}

		/// <summary>
		/// Returns a fishing distribution set to the given objects. If an empty list is given, it creates a basic distribution.
		/// </summary>
		public FishingDistributionDelta(List<FleetFishingEffort> a_objects)
		{
			LoadDistribution(a_objects);
		}

		public Dictionary<int, float> FindValuesForGear(int a_gear_type)
		{
			Dictionary<int, float> result;
			m_values.TryGetValue(a_gear_type, out result);
			return result;
		}

		public IEnumerable<KeyValuePair<int, Dictionary<int, float>>> GetValuesByGear()
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

		public bool HasFishingValue(int a_gearType)
		{
			return m_values.ContainsKey(a_gearType);
		}

		public bool HasCountryGearValue(int a_gearType, int a_countryId)
		{
			if(m_values.TryGetValue(a_gearType, out var gearValues))
			{
				return gearValues.ContainsKey(a_countryId);
			}
			return false;
		}

		public void SetFishingEffort(int a_gearType, int a_country, float a_fishingEffort)
		{
			Dictionary<int, float> countryValues;
			if (!m_values.TryGetValue(a_gearType, out countryValues))
			{
				countryValues = new Dictionary<int, float>(SessionManager.Instance.TeamCount);
				m_values.Add(a_gearType, countryValues);
			}

			countryValues[a_country] = a_fishingEffort;
		}

		private void LoadDistribution(List<FleetFishingEffort> a_deltaValues)
		{
			m_values.Clear();
			if (a_deltaValues != null)
			{
				foreach (FleetFishingEffort obj in a_deltaValues)
				{
					SetFishingEffort(obj.gear_type, obj.country_id, obj.effort_weight);
				}
			}
		}

		public void SubmitToServer(string a_planId, BatchRequest a_batch)
		{
			List<FleetFishingEffort> valuesToSubmit = new List<FleetFishingEffort>(32);

			foreach (KeyValuePair<int, Dictionary<int, float>> fishingType in m_values)
			{
				foreach (KeyValuePair<int, float> kvp in fishingType.Value)
				{
					valuesToSubmit.Add(new FleetFishingEffort
					{
						country_id = kvp.Key,
						gear_type = fishingType.Key,
						effort_weight = kvp.Value
					});
				}
			}

			JObject dataObject = new JObject();

			//form.AddField("fishing_values", valuesToSubmit);
			dataObject.Add("plan", a_planId);
			dataObject.Add("fishing_values", JToken.FromObject(valuesToSubmit));

			a_batch.AddRequest(Server.SendFishingAmount(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
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
