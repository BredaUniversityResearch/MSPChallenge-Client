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
		private const string LOGIN_COUNTRY_NAME_STR = "LoginScreenCountryName";
		private const string LOGIN_COUNTRY_INDEX_STR = "LoginScreenCountryIndex";
		public const string LOGIN_EXPERTISE_INDEX_STR = "LoginScreenExpertiseIndex";
		private const string LOGIN_USER_NAME = "LoginScreenUserName";
		private const string LOGIN_SERVER_NAME = "LoginScreenServerName";
		private const string LOGIN_SERVER_ADRESS = "LoginScreenServerAdress";
		private const string GAME_SERVER_MANAGER_HOSTNAME = "server.mspchallenge.info";

		public enum ELoginMenuTab {Home, Intro, Sessions, News, Settings, Quit, Login}

		private static LoginManager instance;
		public static LoginManager Instance => instance;

		[Header("Tab content")] 
		[SerializeField] private GameObject m_leftBar;
		[SerializeField] private LoginBGMask m_bgMask;
		[SerializeField] private GameObject m_bgBlur;
		[SerializeField] private Dictionary<ELoginMenuTab, LoginContentTab> m_tabs;

		private LoginContentTab m_currentTab;

		private void Awake()
		{
			instance = this;

			if (SystemInfo.systemMemorySize < 8000)
				DialogBoxManager.instance.NotificationWindow("Device not supported", "The current device does not satisfy MSP Challenge's minimum requirements. Effects may range from none to the program feeling unresponsive and/or crashing. Switching to another device is recommended.", null);

			m_currentTab = m_tabs[ELoginMenuTab.Home];
		}

		public void SetTabActive(ELoginMenuTab a_newTab)
		{
			if(m_currentTab != null)
				m_currentTab.SetTabActive(false);
			m_currentTab = m_tabs[a_newTab];
			m_currentTab.SetTabActive(true);
			m_leftBar.SetActive(m_currentTab.m_showLeftBar);
			m_bgMask.gameObject.SetActive(m_currentTab.m_showMask);
			m_bgBlur.SetActive(m_currentTab.m_showBlur);
		}
	}
}
