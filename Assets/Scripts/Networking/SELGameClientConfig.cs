using Newtonsoft.Json;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SELGameClientConfig
	{
		[JsonConverter(typeof(JsonConverterHexColor))]
		public Color directionality_icon_color = Color.white;
	}
}