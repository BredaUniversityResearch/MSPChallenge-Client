using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ImmersiveSessionDetailsWindow : MonoBehaviour
	{
		[SerializeField] GenericTextField m_sessionName;
		[SerializeField] GenericTimeField m_sessionMonth;
		[SerializeField] GenericDropdownField m_sessionType;
		[SerializeField] GenericBoundsField m_sessionBounds;
		[SerializeField] Button m_sessionCreateButton;

		[SerializeField] GameObject m_qrCodeSection;
		[SerializeField] Image m_qrCode;
		[SerializeField] Button m_qrCodeFullscreenButton;

		[SerializeField] GameObject m_connectionSection;
		[SerializeField] GenericTextField m_connectionId;
		[SerializeField] GenericTextField m_connectionSession;
		[SerializeField] GenericTextField m_connectionDockerAPI;
		[SerializeField] GenericTextField m_connectionPort;
		[SerializeField] GenericTextField m_connectionDockerContainer;

		public void Initialise()
		{
			m_sessionName.Initialise("Session Name", 5, null, "Name");
			m_sessionMonth.Initialise("Month", 5, null);
			m_sessionType.Initialise("Type", 5, null, Enum.GetNames(typeof(ImmersiveSession.ImmersiveSessionType)));
			m_sessionBounds.Initialise("Area", 5, 25000, null);

			m_connectionId.Initialise("ID", 5, null, "");
			m_connectionSession.Initialise("Session", 5, null, "");
			m_connectionDockerAPI.Initialise("Docker API", 5, null, "");
			m_connectionPort.Initialise("Port", 5, null, "");
			m_connectionDockerContainer.Initialise("Docker Container", 5, null, "");

			m_connectionId.SetInteractable(false);
			m_connectionSession.SetInteractable(false);
			m_connectionDockerAPI.SetInteractable(false);
			m_connectionPort.SetInteractable(false);
			m_connectionDockerContainer.SetInteractable(false);
			m_sessionCreateButton.onClick.AddListener(OnCreateButtonPressed);
		}

		public void SetToSession(ImmersiveSession a_session)
		{
			m_sessionName.SetContent(a_session.name);
			m_sessionName.SetInteractable(false);

			m_sessionMonth.SetContent(a_session.month);
			m_sessionMonth.SetInteractable(false);

			m_sessionType.SetContent((int)a_session.type);
			m_sessionType.SetInteractable(false);

			m_sessionBounds.SetContent(new Vector4(a_session.bottom_left_x, a_session.bottom_left_y, a_session.top_right_x, a_session.top_right_y));
			m_sessionBounds.SetInteractable(false);

			m_connectionSection.SetActive(true);
			m_connectionId.SetContent(a_session.connection.id.ToString());
			m_connectionSession.SetContent(a_session.connection.session);
			m_connectionDockerAPI.SetContent(a_session.connection.dockerApiID.ToString());
			m_connectionPort.SetContent(a_session.connection.port.ToString());
			m_connectionDockerContainer.SetContent(a_session.connection.dockerContainerID.ToString());

			m_qrCodeSection.SetActive(true);
			//TODO: set qr code image
			gameObject.SetActive(true);
		}

		public void StartCreatingNewSession()
		{
			//TODO
			m_sessionName.SetContent(null);
			m_sessionName.SetInteractable(true);

			m_sessionMonth.SetContent(TimeManager.Instance.GetCurrentMonth());
			m_sessionMonth.SetInteractable(true);

			m_sessionType.SetContent(0);
			m_sessionType.SetInteractable(true);

			m_sessionBounds.SetContent(new Vector4(0f, 0f));
			m_sessionBounds.SetInteractable(true);

			m_qrCodeSection.SetActive(false);
			m_connectionSection.SetActive(false);
			gameObject.SetActive(true);
		}

		void OnCreateButtonPressed()
		{
			//TODO
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}
