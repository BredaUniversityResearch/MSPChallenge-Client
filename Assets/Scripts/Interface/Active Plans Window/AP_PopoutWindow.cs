using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public abstract class AP_PopoutWindow : MonoBehaviour
	{
		protected Plan m_plan;
		protected AP_ContentToggle m_contentToggle;
		protected GenericWindow m_genericWindow;

		protected virtual void Start()
		{
			m_genericWindow = GetComponent<GenericWindow>();
			m_genericWindow.exitButton.onClick.AddListener(TryClose);
		}

		public virtual void OpenToContent(Plan a_content, AP_ContentToggle a_toggle)
		{
			m_plan = a_content;
			m_contentToggle = a_toggle;
			//TODO: align to toggle
		}

		public virtual void Close()
		{
			gameObject.SetActive(false);
		}

		protected void TryClose()
		{
			m_contentToggle.TryClose();
		}

		public virtual bool MayClose()
		{
			//Overwritten by children that have confirm / cancel buttons
			return true; 
		}
	}
}
