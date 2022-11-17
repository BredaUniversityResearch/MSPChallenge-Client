using System.Collections;
using System;
using UnityEngine;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class SimulationSettingsSEL : ASimulationData
	{
		[JsonConverter(typeof(JsonConverterHexColor))]
		public Color directionality_icon_color = Color.white;

		public KPICategoryDefinition[] kpis;
	}
}