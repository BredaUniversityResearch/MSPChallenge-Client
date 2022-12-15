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
		protected ActivePlanWindow m_APWindow;

		protected virtual void Start()
		{
			m_genericWindow = GetComponent<GenericWindow>();
			m_genericWindow.exitButton.onClick.AddListener(TryClose);
		}

		public virtual void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			m_plan = a_content;
			m_contentToggle = a_toggle;
			m_APWindow = a_APWindow;
			gameObject.SetActive(true);
			//TODO: align to toggle
		}

		protected void TryClose()
		{
			m_contentToggle.TryClose();
		}

		public virtual void ApplyContent()
		{ }

		public virtual void DiscardContent()
		{ }

		public virtual bool MayClose()
		{
			//Overwritten by children that have confirm / cancel buttons
			return true; 
		}

		public bool IsOpen => gameObject.activeSelf;
	}
}
