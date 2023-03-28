using System.Collections;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyPlanUpdateJsonConverter : JsonConverter
    {
		public override bool CanConvert(Type objectType)
		{
			return typeof(APolicyData).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jo = JObject.Load(reader);

			string policyType = jo["policy_type"].ToObject<string>();

			object target = null;

			if (PolicyManager.Instance.TryGetDefinition(policyType, out PolicyDefinition definition))
			{
				target = Activator.CreateInstance(definition.m_planUpdateType);
			}
			else
			{
				Debug.LogError("Policy data received for an unregistered policy type: " + policyType);
				return null;
			}

			serializer.Populate(jo.CreateReader(), target);
			return target;
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