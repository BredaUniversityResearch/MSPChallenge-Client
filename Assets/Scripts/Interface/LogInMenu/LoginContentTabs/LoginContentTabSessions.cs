﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTabSessions : LoginContentTab
	{
		private const string LOGIN_SERVER_ADRESS = "LoginScreenServerAdress";
		private const string GAME_SERVER_MANAGER_HOSTNAME = "server.mspchallenge.info";

		[Header("Server address")]
		[SerializeField] private TMP_InputField m_serverAddress;
		[SerializeField] private Button m_resetAddressButton;
		[SerializeField] private TextMeshProUGUI m_serverInfoText;

		[Header("Session list")]
		[SerializeField] private Button m_refreshSessionsButton;
		[SerializeField] private GameObject m_sessionsLoadingObj;
		[SerializeField] private GameObject m_noSessionsObj;
		[SerializeField] private TextMeshProUGUI m_sessionErrorObj;
		[SerializeField] private Transform m_sessionEntryParent;
		[SerializeField] private GameObject m_sessionEntryPrefab;
		[SerializeField] private ToggleGroup m_sessionEntryToggleGroup;

		private List<LoginSessionEntry> m_sessionEntries;
		private int m_nextEntryIndex = 0;
		private int m_expectedServerListID = 0;

		protected override void Initialize()
		{
			base.Initialize();
			m_sessionEntries = new List<LoginSessionEntry>();

			m_serverAddress.text = PlayerPrefs.GetString(LOGIN_SERVER_ADRESS, GAME_SERVER_MANAGER_HOSTNAME);

			m_serverAddress.onEndEdit.AddListener(ServerAddressChanged);
			m_resetAddressButton.onClick.AddListener(ResetServerAddress);
			m_refreshSessionsButton.onClick.AddListener(RefreshSession);
		}

		void ResetServerAddress()
		{
			m_serverAddress.text = GAME_SERVER_MANAGER_HOSTNAME;
			RefreshSession();
		}

		void ServerAddressChanged(string a_newAddress)
		{
			RefreshSession();
		}

		void RefreshSession()
		{
			m_sessionsLoadingObj.SetActive(true);
			foreach (LoginSessionEntry entry in m_sessionEntries)
			{
				entry.SetSelected(false);
				entry.gameObject.SetActive(false);
			}
			m_noSessionsObj.SetActive(false);
			m_sessionErrorObj.gameObject.SetActive(false); 
			m_refreshSessionsButton.interactable = false;
			m_expectedServerListID++;
			m_nextEntryIndex = 0;
			StartCoroutine(GetServerList(m_expectedServerListID));
		}

		IEnumerator GetServerList(int a_serverListID)
		{
			List<GameSession> sessions = new List<GameSession>();

			WWWForm demoForm = new WWWForm();

			//Demo sessions
			demoForm.AddField("client_timestamp", ApplicationBuildIdentifier.FindBuildIdentifier().GetBuildTime());
			demoForm.AddField("visibility", "private");
			demoForm.AddField("demo_servers", 1);
			RetrieveSessionListHandler demoHandler = new RetrieveSessionListHandler(GAME_SERVER_MANAGER_HOSTNAME, demoForm);
			yield return demoHandler.RetrieveListAsync();
			if (demoHandler.Success)
			{
				sessions.AddRange(demoHandler.SessionList.sessionslist);
			}

			//Sessions from server address
			WWWForm form = new WWWForm();
			form.AddField("visibility", 0);
			form.AddField("client_timestamp", ApplicationBuildIdentifier.FindBuildIdentifier().GetBuildTime());
			string host = "localhost";
			if (!string.IsNullOrEmpty(m_serverAddress.text))
			{
				host = m_serverAddress.text.Trim(' ', '\r', '\n', '\t');
				PlayerPrefs.SetString(LOGIN_SERVER_ADRESS, host);
			}
			RetrieveSessionListHandler handler = new RetrieveSessionListHandler(host, form);
			yield return handler.RetrieveListAsync();

			if (m_expectedServerListID == a_serverListID)
			{
				m_sessionsLoadingObj.SetActive(false);
				m_refreshSessionsButton.interactable = true;

				//Create custom sessions
				if (handler.Success)
				{
					if (handler.SessionList?.sessionslist != null && handler.SessionList.sessionslist.Length > 0)
					{
						foreach(GameSession session in handler.SessionList.sessionslist)
							SetSessionEntry(session);
					}
					else
					{
						m_noSessionsObj.SetActive(true);
					}
				}
				else
				{
					m_sessionErrorObj.gameObject.SetActive(true);
					m_sessionErrorObj.text = "Failed to fetch session list from server address";
				}

				//Create demo sessions
				if (demoHandler.Success && demoHandler.SessionList?.sessionslist != null && demoHandler.SessionList.sessionslist.Length > 0)
				{
					foreach (GameSession session in demoHandler.SessionList.sessionslist)
						SetSessionEntry(session);
				}
			}
		}

		void SetSessionEntry(GameSession a_session)
		{
			if (m_nextEntryIndex < m_sessionEntries.Count)
			{
				m_sessionEntries[m_nextEntryIndex].SetToSession(a_session, m_sessionEntryToggleGroup);
			}
			else
			{
				LoginSessionEntry newEntry = Instantiate(m_sessionEntryPrefab, m_sessionEntryParent).GetComponent<LoginSessionEntry>();
				newEntry.SetToSession(a_session, m_sessionEntryToggleGroup);
				m_sessionEntries.Add(newEntry);
			}
		}
	}
}
