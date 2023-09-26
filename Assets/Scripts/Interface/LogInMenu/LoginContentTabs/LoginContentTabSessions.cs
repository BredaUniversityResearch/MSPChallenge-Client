using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using TMPro;
using UnityEngine;
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
		[SerializeField] private GameObject m_sessionTopLine;
		[SerializeField] private TextMeshProUGUI m_sessionErrorObj;
		[SerializeField] private Button m_sessionErrorButton;
		[SerializeField] private GameObject m_sessionErrorButtonContainer;
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
			m_refreshSessionsButton.onClick.AddListener(RefreshSessions);
			RefreshSessions();
		}

		void ResetServerAddress()
		{
			m_serverAddress.text = GAME_SERVER_MANAGER_HOSTNAME;
			RefreshSessions();
		}

		void ServerAddressChanged(string a_newAddress)
		{
			RefreshSessions();
		}

		void RefreshSessions()
		{
			m_sessionsLoadingObj.SetActive(true);
			foreach (LoginSessionEntry entry in m_sessionEntries)
			{
				entry.SetSelected(false);
				entry.gameObject.SetActive(false);
			}
			m_noSessionsObj.SetActive(false);
			m_sessionErrorObj.gameObject.SetActive(false);
			m_sessionErrorButtonContainer.SetActive(false);
			m_serverInfoText.gameObject.SetActive(false); 
			m_refreshSessionsButton.interactable = false;
			m_expectedServerListID++;
			m_nextEntryIndex = 0;
			StartCoroutine(GetServerList(m_expectedServerListID));
		}

		IEnumerator GetServerList(int a_serverListID)
		{
			m_sessionTopLine.SetActive(false);
			var host = CommandLineArgumentsManager.GetInstance().AutoFill(
				CommandLineArgumentsManager.CommandLineArgumentName.ServerAddress,
				!string.IsNullOrEmpty(m_serverAddress.text) ?
					m_serverAddress.text.Trim(' ', '\r', '\n', '\t') : ""
			);
			PlayerPrefs.SetString(LOGIN_SERVER_ADRESS, host);
			if (host == "")
			{
				host = "localhost";
			}

			//Sessions from server address
			WWWForm form = new WWWForm();
			form.AddField("visibility", 0);

            if (!ApplicationBuildIdentifier.Instance.GetHasInformation())
                ApplicationBuildIdentifier.Instance.GetManifest();

            form.AddField("client_timestamp", ApplicationBuildIdentifier.Instance.GetBuildTime());
            RetrieveSessionListHandler handler = new RetrieveSessionListHandler(host, form);
			yield return handler.RetrieveListAsync();

			if (m_expectedServerListID == a_serverListID)
			{
				m_sessionsLoadingObj.SetActive(false);
				m_refreshSessionsButton.interactable = true;

				//Create custom sessions
				if (handler.Success)
				{
					//if (!handler.SessionList.success)
					//{
					//	m_sessionErrorObj.gameObject.SetActive(true);
					//	m_sessionErrorObj.text = handler.SessionList.message;
					//	m_serverInfoText.gameObject.SetActive(false);
					//}
					//else 
					if(!ApplicationBuildIdentifier.Instance.ServerVersionCompatible(handler.SessionListPayload.server_version))
					{
						ShowVersionIncompatibilityError(handler.SessionListPayload);

					}
					else if (handler.SessionListPayload.sessionslist != null && handler.SessionListPayload.sessionslist.Length > 0)
					{
						m_sessionTopLine.SetActive(true);
						foreach (GameSession session in handler.SessionListPayload.sessionslist)
							SetSessionEntry(session);

						if (!string.IsNullOrEmpty(handler.SessionListPayload.server_description))
						{
							m_serverInfoText.gameObject.SetActive(true);
							m_serverInfoText.text = handler.SessionListPayload.server_description;
						}
						else
							m_serverInfoText.gameObject.SetActive(false);

					}
					else
					{
						m_noSessionsObj.SetActive(true);
					}
				}
				else if(handler.SessionListPayload != null)
				{
					ShowVersionIncompatibilityError(handler.SessionListPayload);
				}
				else
				{
					m_sessionErrorObj.gameObject.SetActive(true);
					m_sessionErrorObj.text = "Failed to fetch session list from server address";
				}
			}

			if (null == CommandLineArgumentsManager.GetInstance().GetCommandLineArgumentValue(
				CommandLineArgumentsManager.CommandLineArgumentName.AutoLogin)) yield break;
			IEnumerable<LoginSessionEntry> entries = GetSessionEntryByAutoLogin();
			if (!entries.Any()) yield break;
			LoginManager.Instance.ConnectPressedForSession(entries.First().GetSession());
		}

		void ShowVersionIncompatibilityError(GetSessionListPayload a_sessionListPayload)
		{
			string url = a_sessionListPayload.clients_url;
			m_sessionErrorObj.gameObject.SetActive(true);
			m_sessionErrorObj.text = $"The server (version {a_sessionListPayload.server_version}) is not compatible with the current client (version {ApplicationBuildIdentifier.Instance.GetGitTag()}).\nVisit <u>{url}</u> to download a compatible client version, or connect to a different server.";
			m_sessionErrorButtonContainer.SetActive(true);
			m_sessionErrorButton.onClick.RemoveAllListeners();
			m_sessionErrorButton.onClick.AddListener(() => Application.OpenURL(url));
			m_serverInfoText.gameObject.SetActive(false);
		}
		
		private IEnumerable<LoginSessionEntry> GetSessionEntryByAutoLogin()
		{
			// get by session entry index.
			var sessionEntryIndexText = CommandLineArgumentsManager.GetInstance().GetCommandLineArgumentValue(
				CommandLineArgumentsManager.CommandLineArgumentName.SessionEntryIndex);
			if (null != sessionEntryIndexText && int.TryParse(sessionEntryIndexText, out var sessionEntryIndex) &&
				sessionEntryIndex > 0 && sessionEntryIndex < m_sessionEntries.Count)
			{
				return new List<LoginSessionEntry> {m_sessionEntries[sessionEntryIndex]};
			}
			// or, get by config file name match
			var configFilename = CommandLineArgumentsManager.GetInstance().GetCommandLineArgumentValue(
				CommandLineArgumentsManager.CommandLineArgumentName.ConfigFileName);
			
			// no match found
			if (null == configFilename)
				return new List<LoginSessionEntry>();

			return m_sessionEntries.Where(
				x => x.GetSession().config_file_name == configFilename
			);
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

			m_nextEntryIndex++;
		}
	}
}
