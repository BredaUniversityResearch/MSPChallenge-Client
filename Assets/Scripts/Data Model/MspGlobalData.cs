using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class MspGlobalData
	{
		public const int num_eras = 4;

		public string region;
		public string edition_name;
		[JsonConverter(typeof(JsonConverterHexColor))] 
		public Color edition_colour;
		public string edition_letter;
		public int start; //Year
		public int era_total_months;
		public string era_planning_months;
		public string era_plannin_realtime;
		public string countries;
		public string maxzoom;
		public string user_admin_name;
		public string user_admin_color;
		[JsonConverter(typeof(JsonConverterBinaryBool))]
		public bool user_admin_has_password;
		public string team_info_base_url;
		public string user_region_manager_name;
		public string user_region_manager_color;
		[JsonConverter(typeof(JsonConverterBinaryBool))]
		public bool user_common_has_password;
		public string wiki_base_url;
		public ExpertiseDefinition[] expertise_definitions;
		public string windfarm_data_api_url; // https://test-northsea-dot-hydro-engine.appspot.com/get_windfarm_data
		public JObject dependencies;

		public int session_num_years => Mathf.FloorToInt((float)(era_total_months * num_eras) / 12.0f);

		public int session_end_month => era_total_months * num_eras;

		public int YearsPerEra => era_total_months / 12;
	}
}
