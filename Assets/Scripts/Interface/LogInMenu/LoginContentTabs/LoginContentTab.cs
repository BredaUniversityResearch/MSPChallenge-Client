using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTab : MonoBehaviour
	{
		public bool m_showBlur;
		public bool m_showMask;
		public bool m_showLeftBar;

		[SerializeField] protected LoginManager.ELoginMenuTab m_tab;
		[SerializeField] private GameObject m_tabContent;
		[SerializeField] private Toggle m_tabToggle;

		private bool m_ignoreToggleCallback;

		void Start()
		{
			Initialize();
		}

		protected virtual void Initialize()
		{
			m_tabToggle.onValueChanged.AddListener(OnToggleChanged);
		}

		public virtual void SetTabActive(bool a_active)
		{
			m_tabContent.SetActive(a_active);
			m_ignoreToggleCallback = true;
			m_tabToggle.isOn = a_active;
			m_ignoreToggleCallback = false;

		}

		void OnToggleChanged(bool a_newValue)
		{
			if (m_ignoreToggleCallback)
				return;
			LoginManager.Instance.SetTabActive(m_tab);
		}
	}
}
