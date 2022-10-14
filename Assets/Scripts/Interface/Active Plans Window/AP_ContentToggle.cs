using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace MSP2050.Scripts
{
	public class AP_ContentToggle : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] TextMeshProUGUI m_nameText;
		[SerializeField] Image m_icon;

		ActivePlanWindow m_apWindow;
		AP_PopoutWindow m_popoutWindow;
		bool m_ignoreCallback;

		public void Initialise(ActivePlanWindow a_apWindow, AP_PopoutWindow a_popoutWindow)
		{
			m_toggle.onValueChanged.AddListener(OnToggleValueChanged);
			m_apWindow = a_apWindow;
			m_popoutWindow = a_popoutWindow;
		}

		public void SetContent(string a_text, Sprite a_icon)
		{
			m_nameText.text = a_text;
			m_icon.sprite = a_icon;
			gameObject.SetActive(true);
		}

		public void SetContent(string a_text)
		{
			m_nameText.text = a_text;
		}

		void OnToggleValueChanged(bool a_value)
		{
			if (m_ignoreCallback)
				return;

			if(a_value)
			{
				if (!m_apWindow.MayOpenNewPopout())
				{
					m_ignoreCallback = true;
					m_toggle.isOn = false;
					m_ignoreCallback = false;
				}
				else
				{
					m_popoutWindow.OpenToContent(m_apWindow.CurrentPlan, this);
				}
			}
			else
			{
				TryClose();
			}
		}

		public bool TryClose()
		{
			if (m_popoutWindow.MayClose())
			{
				m_ignoreCallback = true;
				m_toggle.isOn = false;
				m_ignoreCallback = false;
				m_popoutWindow.Close();
				return true;
			}
			return false;
		}
	}
}
