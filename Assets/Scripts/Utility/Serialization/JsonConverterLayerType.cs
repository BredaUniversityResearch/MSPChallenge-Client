using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

class JsonConverterLayerType : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Array)
        {
            List<EntityTypeValues> layerTypeList = token.ToObject<List<EntityTypeValues>>(serializer);
            Dictionary<int, EntityTypeValues> result = new Dictionary<int, EntityTypeValues>();
            for (int i = 0; i < layerTypeList.Count; i++)
            {
                result.Add(i, layerTypeList[i]);
            }
            return result;
        }
        else if (token.Type == JTokenType.Object)
        {
            return token.ToObject<Dictionary<int, EntityTypeValues>>(serializer);
        }

        throw new JsonSerializationException("Unexpected JSON format encountered in LayerTypeConverter: " + token.ToString());
    }

    public override bool CanWrite
    {
        get
        {
            return false;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Dictionary<int, EntityTypeValues>) || objectType == typeof(List<EntityTypeValues>);
    }
}