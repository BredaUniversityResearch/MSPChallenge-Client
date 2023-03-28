using System;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTabSettings : LoginContentTab
	{
		[SerializeField] private Button m_acceptButton;
		[SerializeField] private Button m_cancelButton;

		protected override void Initialize()
		{
			base.Initialize();

			m_acceptButton.onClick.AddListener(ReturnToHome);
			m_cancelButton.onClick.AddListener(ReturnToHome);
		}
		
		void ReturnToHome()
		{
			LoginManager.Instance.SetTabActive(LoginManager.ELoginMenuTab.Home);
		}
	}
}
