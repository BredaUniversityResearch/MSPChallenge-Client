using System;
using Newtonsoft.Json;

namespace Utility.Serialization
{
	public class JsonConverterBinaryBool : JsonConverter
	{
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
			//Color color = (bool)value;
			//writer.WriteValue(Util.ColorToHex(color));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string input = reader.Value.ToString().ToLower().Trim();
			bool result;
			if (input == "0" || input == "false")
				result = false;
			else if (input == "1" || input == "true")
				result = true;
			else
				throw new Exception("Could not convert " + input + " to a boolean value.");
			return result;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(bool);
		}
	}
}