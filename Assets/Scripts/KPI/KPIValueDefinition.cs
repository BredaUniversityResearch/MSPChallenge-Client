using Newtonsoft.Json;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPIValueDefinition
	{
		public string valueName = null;
		public string valueDisplayName = null;
		[JsonConverter(typeof(JsonConverterHexColor))]
		public Color valueColor = Color.white;
		public string unit = null;
		public int valueDependentCountry = KPIValue.CountrySpecific; 
	}
}
