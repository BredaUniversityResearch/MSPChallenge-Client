﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SessionManager : MonoBehaviour
	{
		private static SessionManager singleton;
		public static SessionManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<SessionManager>();
				return singleton;
			}
		}

		public const int GM_ID = 1;
		public const int AM_ID = 2;

		private Dictionary<int, Team> teamsByID;
		private Dictionary<string, Team> teamsByName = new Dictionary<string, Team>();

		public int TeamCount { get { return teamsByID.Count; } }
		public int CurrentSessionID { get; set; }
		public int CurrentUserTeamID { get; private set; }
		public string CurrentUserName { get; private set; }

		[CanBeNull]
		public string Password { get; private set; }

		public bool AreWeGameMaster { get { return CurrentUserTeamID == GM_ID; } }
		public bool AreWeAreaManager { get { return CurrentUserTeamID == AM_ID; } }
		public bool AreWeManager { get { return CurrentUserTeamID == AM_ID || CurrentUserTeamID == GM_ID; } }

		public bool IsGameMaster(int id) { return  id == GM_ID; }
		public bool IsAreaManager(int id) { return id == AM_ID; }
		public bool IsManager(int id) { return id == AM_ID || id == GM_ID; }

		public MspGlobalData MspGlobalData { get; private set; }

		public delegate void ImportCompleteCallback(bool success);
		public event ImportCompleteCallback OnImportComplete;

		public Color CurrentTeamColor
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

		public Team CurrentTeam
		{
			get
			{
				Team currentTeam = null;
				teamsByID.TryGetValue(CurrentUserTeamID, out currentTeam);
				return currentTeam;
			}
		}

		void Awake()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
			{
				singleton = this;
				DontDestroyOnLoad(gameObject);
			}
			CurrentSessionID = 1;
			CurrentUserTeamID = GM_ID;
			CurrentUserName = "DEFAULT_USER_NAME_CHANGE_ME";
		}

		public void SetSession(int teamID, string password, string username, int sessionID)
		{
			CurrentSessionID = sessionID;
			CurrentUserTeamID = teamID;
			CurrentUserName = username;
			Password = password;
		}

		public void ImportGlobalData()
		{
			NetworkForm form = new NetworkForm();
			ServerCommunication.Instance.DoRequest<MspGlobalData>(Server.GetGlobalData(), form, HandleGlobalData);
			//TODO: handle import failed
		}

		public void HandleGlobalData(MspGlobalData data)
		{
			MspGlobalData = data;

			NetworkForm form = new NetworkForm();
			form.AddField("name", data.countries);
			ServerCommunication.Instance.DoRequest<LayerMeta>(Server.LayerMetaByName(), form, LoadTeams);
			//TODO: handle import failed
		}

		public void LoadTeams(LayerMeta eezMeta)
		{
			teamsByID = new Dictionary<int, Team>();
			if (LayerManager.Instance.EEZLayer == null)
				Debug.LogError("No EEZ layer loaded. Teams cannot be determined.");
			else
			{
				foreach (KeyValuePair<int, EntityTypeValues> kvp in eezMeta.layer_type)
				{
					teamsByID.Add(kvp.Value.value, new Team(kvp.Value.value, Util.HexToColor(kvp.Value.polygonColor), kvp.Value.displayName));
				}
			}

			//Load manager and admin from global data
			teamsByID.Add(GM_ID, new Team(GM_ID, Util.HexToColor(SessionManager.Instance.MspGlobalData.user_admin_color), SessionManager.Instance.MspGlobalData.user_admin_name));
			teamsByID.Add(AM_ID, new Team(AM_ID, Util.HexToColor(SessionManager.Instance.MspGlobalData.user_region_manager_color), SessionManager.Instance.MspGlobalData.user_region_manager_name));

			foreach (KeyValuePair<int, Team> kvp in GetTeamsByID())
			{
				teamsByName.Add(kvp.Value.name, kvp.Value);
			}

			OnImportComplete?.Invoke(true);
		}

		public Team GetTeamByTeamID(int teamID)
		{
			return teamsByID[teamID];
		}

		public Team FindTeamByID(int teamID)
		{
			Team result;
			teamsByID.TryGetValue(teamID, out result);
			return result;
		}

		public IEnumerable<Team> GetTeams()
		{
			return teamsByID.Values;
		}

		public IEnumerable<KeyValuePair<int, Team>> GetTeamsByID()
		{
			return teamsByID;
		}

		public Team FindTeamByName(string teamName)
		{
			Team result;
			teamsByName.TryGetValue(teamName, out result);
			return result;
		}
	}
}