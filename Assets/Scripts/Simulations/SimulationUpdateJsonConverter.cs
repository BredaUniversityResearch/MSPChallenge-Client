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
            return typeof(ASimulationData).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string simulationType = jo["simulation_type"].ToObject<string>();

            object target = null;

            if (SimulationManager.Instance.TryGetDefinition(simulationType, out SimulationDefinition definition))
            {
                target = Activator.CreateInstance(definition.m_updateType);
            }
            else
            {
                Debug.LogError("Policy data received for an unregistered policy type: " + simulationType);
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