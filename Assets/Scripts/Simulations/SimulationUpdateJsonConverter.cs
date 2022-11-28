using System.Collections;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class SimulationUpdateJsonConverter : JsonConverter
    {
		public override bool CanConvert(Type objectType)
		{
			return (typeof(IList).IsAssignableFrom(objectType) || typeof(Array).IsAssignableFrom(objectType)) &&
			typeof(ASimulationData).IsAssignableFrom(objectType.GetGenericArguments()[0]);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jo;
			ASimulationData target;
			string policyType;

			List<ASimulationData> result = new List<ASimulationData>();

			while (reader.Read() && reader.Value != null)
			{
				jo = JObject.Load(reader);
				policyType = jo["policy_type"].ToObject<string>();
				target = null;

				if (SimulationManager.Instance.TryGetDefinition(policyType, out SimulationDefinition definition))
				{
					target = (ASimulationData)Activator.CreateInstance(definition.m_updateType);
				}
				else
				{
					Debug.LogError("Policy data received for an unregistered simulation type: " + policyType);
					return null;
				}
				serializer.Populate(jo.CreateReader(), target);
				result.Add(target);
			}

			return result;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override bool CanWrite
        {
            get { return false; }
        }
	}
}