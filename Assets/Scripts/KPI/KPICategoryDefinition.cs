using Newtonsoft.Json;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPICategoryDefinition
	{
		public string categoryName = null;
		public string categoryDisplayName = null;
		[JsonConverter(typeof(JsonConverterHexColor))]
		public Color categoryColor = Color.white;
		public EKPICategoryValueType categoryValueType = EKPICategoryValueType.Sum;
		public string unit = null;
		public EKPIValueColorScheme valueColorScheme = EKPIValueColorScheme.DefinedColor;
		public KPIValueDefinition[] valueDefinitions = null;
	}
}
