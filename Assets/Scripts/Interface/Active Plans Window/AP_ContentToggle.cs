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
		Action m_onOpenCallback;
		bool m_ignoreCallback;

		public bool IsOn => m_toggle.isOn;

		public void Initialise(ActivePlanWindow a_apWindow, AP_PopoutWindow a_popoutWindow, Action a_onOpenCallback = null)
		{
			m_toggle.onValueChanged.AddListener(OnToggleValueChanged);
			m_apWindow = a_apWindow;
			m_popoutWindow = a_popoutWindow;
			m_onOpenCallback = a_onOpenCallback;
		}

		private void OnDisable()
		{
			ForceClose(false);
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
				if (!m_apWindow.MayOpenNewPopout(this))
				{
					m_ignoreCallback = true;
					m_toggle.isOn = false;
					m_ignoreCallback = false;
				}
				else
				{
					m_popoutWindow.OpenToContent(m_apWindow.CurrentPlan, this, m_apWindow);
					if (m_onOpenCallback != null)
						m_onOpenCallback.Invoke();
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
				ForceClose(true);
				return true;
			}
			return false;
		}

		public void ForceClose(bool a_applyChanges)
		{
			m_ignoreCallback = true;
			m_toggle.isOn = false;
			if(a_applyChanges)
				m_popoutWindow.ApplyContent();
			else
				m_popoutWindow.DiscardContent();
			m_popoutWindow.gameObject.SetActive(false);
			m_apWindow.ClearSelectedContentToggle();
			m_ignoreCallback = false;
		}

		public void ForceClose()
		{
			ForceClose(false);
		}
	}
}
