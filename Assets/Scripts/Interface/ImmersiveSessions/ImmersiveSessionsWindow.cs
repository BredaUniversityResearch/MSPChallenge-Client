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
		[SerializeField] Button m_refreshButton;
		[SerializeField] GameObject m_noSessionsEntry;
		[SerializeField] ImmersiveSessionDetailsWindow m_detailsWindow;
		[SerializeField] MenuBarToggle m_menuToggle;

		List<ImmersiveSessionEntry> m_sessionEntries;
		ImmersiveSessionEntry m_selectedEntry;
		int m_nextEntryID;
		bool m_initialised;

		private void Awake()
		{
			if(!m_initialised)
				Initialise();
		}

		public void Initialise()
		{
			m_initialised = true;
			m_sessionEntries = new List<ImmersiveSessionEntry>();
			m_nextEntryID = 0;
			m_createSessionButton.onClick.AddListener(OnCreateButtonClicked);
			m_refreshButton.onClick.AddListener(RefreshSessions);
			m_detailsWindow.Initialise(this);
			UpdateManager.Instance.RegisterImmersiveSessionListener(OnSessionListUpdated, OnSessionListUpdateFailed, true);
		}

		private void OnEnable()
		{
			RefreshSessions();
		}

		private void OnDisable()
		{
			m_menuToggle.toggle.isOn = false;
		}

		void RefreshSessions()
		{
			if(m_selectedEntry != null)
			{
				m_detailsWindow.gameObject.SetActive(false);
				m_selectedEntry.ForceSetToggle(false);
				m_selectedEntry = null;
			}
			for (int i = 0; i < m_sessionEntries.Count; i++)
			{
				m_sessionEntries[i].gameObject.SetActive(false);
			}
			m_nextEntryID = 0;
			m_refreshButton.interactable = false;
			ServerCommunication.Instance.DoRequestForm<ImmersiveSession[]>(Server.ImmersiveSessions(), null, OnSessionListUpdated, HandleGetSessionFailure);
		}

		void OnSessionListUpdated(List<ImmersiveSession> a_sessions)
		{ }

		void OnSessionListUpdateFailed(string a_reason)
		{ }

		void OnSessionListUpdated(ImmersiveSession[] a_sessions)
		{
			int i = 0;
			if (a_sessions == null)
			{
				m_noSessionsEntry.SetActive(true);
			}
			else
			{
				//Set entries to list
				m_noSessionsEntry.SetActive(a_sessions.Length == 0);
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
			}
			m_nextEntryID = i;
			for (; i < m_sessionEntries.Count; i++)
			{
				m_sessionEntries[i].gameObject.SetActive(false);
			}
			m_refreshButton.interactable = true;
		}

		void HandleGetSessionFailure(ARequest request, string message)
		{
			if (request.retriesRemaining > 0)
			{
				Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
				ServerCommunication.Instance.RetryRequest(request);
			}
			else
			{
				
				m_refreshButton.interactable = true;
				m_noSessionsEntry.SetActive(true);
				Debug.LogError($"Request failed with code {request.Www.responseCode.ToString()}: {message ?? ""}");
			}
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

		public void AddAndSelectSession(ImmersiveSession a_session)
		{
			if (m_nextEntryID == m_sessionEntries.Count)
			{
				ImmersiveSessionEntry entry = Instantiate(m_sessionEntryPrefab, m_sessionEntryParent).GetComponent<ImmersiveSessionEntry>();
				entry.Initialise(OnSelectedSessionChanged);
				m_sessionEntries.Add(entry);
			}
			m_sessionEntries[m_nextEntryID].SetToSession(a_session);
			OnSelectedSessionChanged(a_session, m_sessionEntries[m_nextEntryID]);
			m_nextEntryID++;
		}
	}
}
