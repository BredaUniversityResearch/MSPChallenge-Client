using Newtonsoft.Json;
using UnityEngine;
using Utility.Serialization;

public class MspGlobalData
{
	public const int num_eras = 4;

	public string region;
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
	public string region_base_url;
	public string user_region_manager_name;
	public string user_region_manager_color;
	[JsonConverter(typeof(JsonConverterBinaryBool))]
	public bool user_common_has_password;
	public ESimulationType[] configured_simulations;
	public string wiki_base_url;
    public ExpertiseDefinition[] expertise_definitions;
    public string windfarm_data_api_url; // https://test-northsea-dot-hydro-engine.appspot.com/get_windfarm_data

    public int session_num_years
	{
		get
		{
			return Mathf.FloorToInt((float)(era_total_months * num_eras) / 12.0f);
		}
	}

	public int session_end_month
	{
		get
		{
			return era_total_months * num_eras;
		}
	}

    public int YearsPerEra
    {
        get
        {
            return era_total_months / 12;
        }
    }

	public int MonthToEra(int month)
	{
		return (month / era_total_months);
	}
}