using System;
using System.Collections.Generic;
using UnityEngine;

public static class TeamManager
{
	public const int GM_ID = 1;
	public const int AM_ID = 2;

	private static Dictionary<int, Team> teamsByID = new Dictionary<int, Team>();
	private static Dictionary<string, Team> teamsByName = new Dictionary<string, Team>();

	public static int TeamCount { get { return teamsByID.Count; } }
	public static int CurrentSessionID { get; private set; }
	public static int CurrentUserTeamID { get; private set; }
	public static string CurrentUserName { get; private set; }

	public static bool IsGameMaster { get { return CurrentUserTeamID == GM_ID; } }
	public static bool IsAreaManager { get { return CurrentUserTeamID == AM_ID; } }
	public static bool IsManager { get { return CurrentUserTeamID == AM_ID || CurrentUserTeamID == GM_ID; } }

	public static event Action OnTeamsLoadComplete;

	public static Color CurrentTeamColor
	{
		get
		{
			Team currentTeam;
			if (teamsByID.TryGetValue(CurrentUserTeamID, out currentTeam))
			{
				return currentTeam.color;
			}
			else
			{
				return Color.magenta;
			}
		}
	}

	public static Team CurrentTeam
	{
		get
		{
			Team currentTeam = null;
			teamsByID.TryGetValue(CurrentUserTeamID, out currentTeam);
			return currentTeam;
		}
	}

	static TeamManager()
	{
		CurrentSessionID = 1;
		CurrentUserTeamID = GM_ID;
		CurrentUserName = "DEFAULT_USER_NAME_CHANGE_ME";
	}

	public static void LoadTeams()
	{
		if (LayerManager.EEZLayer == null)
			Debug.LogError("No EEZ layer loaded. Teams cannot be determined.");
		else
		{
			foreach (KeyValuePair<int, EntityType> kvp in LayerManager.EEZLayer.EntityTypes)
			{
				teamsByID.Add(kvp.Value.value, new Team(kvp.Value.value, kvp.Value.DrawSettings.PolygonColor, kvp.Value.Name));
			}
		}

		//Load manager and admin from global data
		teamsByID.Add(1, new Team(1, Util.HexToColor(Main.MspGlobalData.user_admin_color), Main.MspGlobalData.user_admin_name));
		teamsByID.Add(2, new Team(2, Util.HexToColor(Main.MspGlobalData.user_region_manager_color), Main.MspGlobalData.user_region_manager_name));

		TeamsLoaded();
	}

	public static void TeamsLoaded()
	{
		foreach (KeyValuePair<int, Team> kvp in GetTeamsByID())
		{
			teamsByName.Add(kvp.Value.name, kvp.Value);
		}

		InterfaceCanvas.Instance.SetAccent(CurrentTeamColor);
		//InterfaceCanvas.instance.teamSelector.FillTeamDropdown();
		InterfaceCanvas.Instance.activePlanWindow.OnCountriesLoaded();
		KPIManager.CreateEnergyKPIs();

		if (OnTeamsLoadComplete != null)
		{
			OnTeamsLoadComplete();
		}
	}

	public static void InitializeUserValues(int userID, string userName, int sessionID, Dictionary<int, Team> aTeams)
	{
		CurrentUserTeamID = userID;
		CurrentUserName = userName;
		CurrentSessionID = sessionID;
		teamsByID = aTeams;
	}

	public static Team GetTeamByTeamID(int teamID)
	{
		return teamsByID[teamID];
	}

	public static Team FindTeamByID(int teamID)
	{
		Team result;
		teamsByID.TryGetValue(teamID, out result);
		return result;
	}

	public static IEnumerable<Team> GetTeams()
	{
		return teamsByID.Values;
	}

	public static IEnumerable<KeyValuePair<int, Team>> GetTeamsByID()
	{
		return teamsByID;
	}

	public static Team FindTeamByName(string teamName)
	{
		Team result;
		teamsByName.TryGetValue(teamName, out result);
		return result;
	}
}