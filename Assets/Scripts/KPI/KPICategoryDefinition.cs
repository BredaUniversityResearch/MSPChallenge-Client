using Newtonsoft.Json;
using UnityEngine;
using Utility.Serialization;

namespace KPI
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
