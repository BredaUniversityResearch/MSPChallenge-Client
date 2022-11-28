using System.Collections;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MSP2050.Scripts
{
	public class PolicySettingsJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
			return (typeof(IList).IsAssignableFrom(objectType) || typeof(Array).IsAssignableFrom(objectType)) &&
            typeof(APolicyData).IsAssignableFrom(objectType.GetGenericArguments()[0]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo;
            APolicyData target;
			string policyType;

            List<APolicyData> result = new List<APolicyData>();

            while (reader.Read() && reader.Value != null)
            {
                jo = JObject.Load(reader);
                Debug.Log(reader.Value);
                policyType = jo["policy_type"].ToObject<string>();
				target = null;

                if (PolicyManager.Instance.TryGetDefinition(policyType, out PolicyDefinition definition))
                {
                    target = (APolicyData)Activator.CreateInstance(definition.m_settingsType);
                }
                else
                {
                    Debug.LogError("Policy data received for an unregistered policy type: " + policyType);
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