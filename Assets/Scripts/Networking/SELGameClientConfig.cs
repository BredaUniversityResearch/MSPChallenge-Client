using Newtonsoft.Json;
using UnityEngine;
using Utility.Serialization;

public class SELGameClientConfig
{
	[JsonConverter(typeof(JsonConverterHexColor))]
	public Color directionality_icon_color = Color.white;
}