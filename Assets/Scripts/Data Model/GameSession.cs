using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class GameSession
	{
		public enum GameState { Setup, Simulation, Play, Pause, End, FastForward }
		public enum SessionState { Request, Initializing, Healthy, Failed, Archived }

		public int id;
		public string name;
		public int game_config_version_id;
		public int game_server_id;
		public int watchdog_server_id;
		public int game_creation_time;
		public int game_start_year;
		public int game_current_month;
		public int game_end_month;
		public int game_running_til_time;
		public SessionState session_state;
		public GameState game_state;
		//public int game_visibility;
		public int players_active;
		public int players_past_hour;
		public string game_server_name;
		public string game_server_address;
		public string game_ws_server_address;
		public string watchdog_name;
		public string watchdog_address;
		public int config_version_version;
		public string config_version_message;
		public string config_file_name;
		public string config_file_description;
		public string region;
		public string edition_name;
		[JsonConverter(typeof(JsonConverterHexColor))]
		public Color edition_colour;
		public string edition_letter;
		public string endpoint;

		public string GetStartTime()
		{
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(game_creation_time).ToLocalTime();
			return dtDateTime.ToShortDateString();
		}

		public string GetEndTime()
		{
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(game_running_til_time).ToLocalTime();
			return dtDateTime.ToShortDateString();
		}
	}

	public class GameSessionList
	{
		public bool success;
		public string message;
		public GameSession[] sessionslist;
	}
}
