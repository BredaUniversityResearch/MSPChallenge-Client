using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginManager : SerializedMonoBehaviour
	{
		private const string LOGIN_SERVER_NAME = "LoginScreenServerName";

		public enum ELoginMenuTab {Home, Intro, Sessions, News, Settings, Quit, Login}

		private static LoginManager instance;
		public static LoginManager Instance => instance;

		[Header("Tab content")] 
		[SerializeField] private GameObject m_leftBar;
		[SerializeField] private LoginBGMask m_bgMask;
		[SerializeField] private GameObject m_bgBlur;
		[SerializeField] private GameObject m_tabScriptObject;

		[Header("Generic")]
		public TeamImporter m_teamImporter;
		[SerializeField] private GameObject m_loadingOverlay;
		[SerializeField] private Button m_optionsButton;

		private Dictionary<ELoginMenuTab, LoginContentTab> m_tabs;
		private LoginContentTab m_currentTab;
		private GameSession m_sessionConnectingTo;

		private void Awake()
		{
			instance = this;

			if (SystemInfo.systemMemorySize < 8000)
				DialogBoxManager.instance.NotificationWindow("Device not supported", "The current device does not satisfy MSP Challenge's minimum requirements. Effects may range from none to the program feeling unresponsive and/or crashing. Switching to another device is recommended.", null, "Confirm");

			m_tabs = new Dictionary<ELoginMenuTab, LoginContentTab>();
			foreach (LoginContentTab tab in m_tabScriptObject.GetComponents<LoginContentTab>())
			{
				m_tabs[tab.Tab] = tab;
			}
			SetTabActive(ELoginMenuTab.Home);

			m_optionsButton.onClick.AddListener(OnOptionsButtonClicked);
		}

		void Update()
		{
			ServerCommunication.Update(false);
		}

		void OnOptionsButtonClicked()
		{
			SetTabActive(ELoginMenuTab.Settings);
		}

		public void SetTabActive(ELoginMenuTab a_newTab)
		{
			if(m_currentTab != null)
				m_currentTab.SetTabActive(false);
			m_currentTab = m_tabs[a_newTab];
			m_currentTab.SetTabActive(true);

			m_leftBar.SetActive(m_currentTab.m_showLeftBar);
			m_bgMask.SetMaskActive(m_currentTab.m_showMask);
			m_bgBlur.SetActive(m_currentTab.m_showBlur);
		}

		public void ConnectPressedForSession(GameSession a_session)
		{
			if (a_session.session_state == GameSession.SessionState.Request || a_session.session_state == GameSession.SessionState.Initializing)
			{
				DialogBoxManager.instance.NotificationWindow("Connecting failed", "Session is initializing, please try again later.", null, "Continue");
				return;
			}
			else if (a_session.session_state != GameSession.SessionState.Healthy)
			{
				DialogBoxManager.instance.NotificationWindow("Connecting failed", "Session has been archived or initialization failed.", null, "Continue");
				return;
			}
			else if (string.IsNullOrEmpty(a_session.game_ws_server_address))
			{
				DialogBoxManager.instance.NotificationWindow("Connecting failed", "The selected session is not compatible with the current client version", null, "Continue");
				return;
			}

			//Set server settings to session
			Server.Host = a_session.game_server_address;
			Server.Endpoint = a_session.endpoint ?? "";
			Server.GameSessionId = a_session.id;
			Server.WsServerUri = new Uri(a_session.game_ws_server_address);
			PlayerPrefs.SetString(LOGIN_SERVER_NAME, a_session.game_server_address);

			//Import global data for session
			m_sessionConnectingTo = a_session;
			m_teamImporter.OnImportComplete += OnTeamImportFinished;
			m_loadingOverlay.SetActive(true);
			m_teamImporter.ImportGlobalData();
		}

		public void OnTeamImportFinished(bool success)
		{
			m_teamImporter.OnImportComplete -= OnTeamImportFinished;
			m_loadingOverlay.SetActive(false);
			if (success)
			{
				SetTabActive(ELoginMenuTab.Login);
				((LoginContentTabLogin) m_tabs[ELoginMenuTab.Login]).SetToSession(m_sessionConnectingTo);
			}
			else
			{
				DialogBoxManager.instance.NotificationWindow("Connecting failed", "An error occurred when connecting to the session", null, "Continue");
			}
		}

		public void SetLoadingOverlayActive(bool a_active)
		{
			m_loadingOverlay.SetActive(a_active);
		}
	}
}
