using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ImmersiveSessionsWindow : MonoBehaviour
	{
		[SerializeField] GameObject m_sessionEntryPrefab;
		[SerializeField] Transform m_sessionEntryParent;
		[SerializeField] Button m_createSessionButton;
		[SerializeField] GameObject m_noSessionsEntry;
		[SerializeField] ImmersiveSessionDetailsWindow m_detailsWindow;

		List<ImmersiveSessionEntry> m_sessionEntries;
		ImmersiveSessionEntry m_selectedEntry;

		public void Initialise()
		{
			m_sessionEntries = new List<ImmersiveSessionEntry>();
			m_createSessionButton.onClick.AddListener(OnCreateButtonClicked);
		}

		public void OnSessionListUpdated(ImmersiveSession[] a_sessions)
		{
			//Set entries to list
			int i = 0;
			for (; i < a_sessions.Length; i++)
			{
				if (i == m_sessionEntries.Count)
				{
					ImmersiveSessionEntry entry = Instantiate(m_sessionEntryPrefab, m_sessionEntryParent).GetComponent<ImmersiveSessionEntry>();
					entry.Initialise(OnSelectedSessionChanged);
					m_sessionEntries.Add(entry);
				}
				m_sessionEntries[i].SetToSession(a_sessions[i]);
			}
			for (; i < m_sessionEntries.Count; i++)
			{
				m_sessionEntries[i].gameObject.SetActive(false);
			}
			if (m_selectedEntry != null)
				m_selectedEntry.ForceSetToggle(false);
			m_selectedEntry = null;
		}

		void OnSelectedSessionChanged(ImmersiveSession a_session, ImmersiveSessionEntry a_entry)
		{
			if(a_session == null)
			{
				m_detailsWindow.Hide();
			}
			else
			{
				if (m_selectedEntry != null)
					m_selectedEntry.ForceSetToggle(false);
				m_detailsWindow.SetToSession(a_session);
			}
			m_selectedEntry = a_entry;
		}

		void OnCreateButtonClicked()
		{
			if (m_selectedEntry != null)
				m_selectedEntry.ForceSetToggle(false);
			m_selectedEntry = null;
			m_detailsWindow.StartCreatingNewSession();
		}
	}
}
