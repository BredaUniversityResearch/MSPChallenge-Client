using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Utility.Serialization
{
	public class JsonConverterHexColor : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Color color = (Color)value;
			writer.WriteValue(Util.ColorToHex(color));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Color result = Util.HexToColor((string)reader.Value);
			return result;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Color);
		}
	}
}