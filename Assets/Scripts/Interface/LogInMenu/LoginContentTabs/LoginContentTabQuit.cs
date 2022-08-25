using System;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTabQuit : LoginContentTab
	{
		[SerializeField] private Button m_quitButton;
		[SerializeField] private Button m_cancelButton;

		private List<LoginNewsEntry> m_newsEntries;

		protected override void Initialize()
		{
			base.Initialize();

			m_quitButton.onClick.AddListener(OnQuitClick);
			m_cancelButton.onClick.AddListener(OnCancelClick);
		}

		void OnQuitClick()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
			Application.Quit();

		}

		void OnCancelClick()
		{
			LoginManager.Instance.SetTabActive(LoginManager.ELoginMenuTab.Home);
		}
	}
}
