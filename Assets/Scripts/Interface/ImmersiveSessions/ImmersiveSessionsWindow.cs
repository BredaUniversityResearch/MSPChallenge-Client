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
		//[SerializeField] Button m_refreshButton;
		[SerializeField] GameObject m_noSessionsEntry;
		[SerializeField] ImmersiveSessionDetailsWindow m_detailsWindow;
		[SerializeField] MenuBarToggle m_menuToggle;

		Dictionary<int, (ImmersiveSession session, ImmersiveSessionEntry entry)> m_sessions;
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
			m_sessions = new Dictionary<int, (ImmersiveSession, ImmersiveSessionEntry)>();
			m_nextEntryID = 0;
			m_createSessionButton.onClick.AddListener(OnCreateButtonClicked);
			//m_refreshButton.onClick.AddListener(RefreshSessionsManually);
			m_detailsWindow.Initialise(this);
			ServerCommunication.Instance.DoRequestForm<List<ImmersiveSession>>(Server.ImmersiveSessions(), null, OnSessionListUpdated, HandleGetSessionFailure);
			UpdateManager.Instance.RegisterImmersiveSessionListener(OnSessionListUpdated, OnSessionListUpdateFailed, true);
		}

		private void OnDestroy()
		{
			UpdateManager.Instance.RegisterImmersiveSessionListener(OnSessionListUpdated, OnSessionListUpdateFailed, false);
		}

		private void OnDisable()
		{
			m_menuToggle.toggle.isOn = false;
		}

		void OnSessionListUpdateFailed(string a_reason)
		{
			Debug.LogError($"WS Server Immersive session update failed with message: {a_reason}");
		}

		void OnSessionListUpdated(List<ImmersiveSession> a_sessions)
		{
			foreach(ImmersiveSession session in a_sessions)
			{
				if(m_sessions.TryGetValue(session.id, out var existingSession))
				{
					if (session.status == ImmersiveSession.ImmersiveSessionStatus.removed)
					{
						//Remove existing
						if(m_selectedEntry == existingSession.entry)
						{
							m_selectedEntry = null;
							m_detailsWindow.Hide();
						}

						Destroy(existingSession.entry.gameObject);
						m_sessions.Remove(session.id);
					}
					else
					{
						//Update existing
						m_sessions[session.id] = (session, existingSession.entry);
						existingSession.entry.SetToSession(session);
						if(m_selectedEntry == existingSession.entry)
						{
							m_detailsWindow.SetToSession(session);
						}
					}
				}
				else if(session.status != ImmersiveSession.ImmersiveSessionStatus.removed)
				{
					//Add new
					ImmersiveSessionEntry entry = Instantiate(m_sessionEntryPrefab, m_sessionEntryParent).GetComponent<ImmersiveSessionEntry>();
					entry.Initialise(OnSelectedSessionChanged);
					entry.SetToSession(session);
					m_sessions.Add(session.id, (session, entry));
				}
			}
			m_noSessionsEntry.SetActive(m_sessions.Count == 0);
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
				if (m_selectedEntry != null && m_selectedEntry != a_entry)
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
			if(m_sessions.TryGetValue(a_session.id, out var existingSession))
			{ 
				//WS update came in first, session exists
				OnSelectedSessionChanged(a_session, existingSession.entry);
				existingSession.entry.ForceSetToggle(true);
			}
			else
			{
				//Add new
				ImmersiveSessionEntry entry = Instantiate(m_sessionEntryPrefab, m_sessionEntryParent).GetComponent<ImmersiveSessionEntry>();
				entry.Initialise(OnSelectedSessionChanged);
				entry.SetToSession(a_session);
				entry.ForceSetToggle(true);
				m_sessions.Add(a_session.id, (a_session, entry));
				OnSelectedSessionChanged(a_session, entry);
				m_noSessionsEntry.SetActive(false);
			}
		}
	}
}
