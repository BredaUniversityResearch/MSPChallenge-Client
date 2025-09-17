using QRCoder;
using QRCoder.Unity;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class ImmersiveSessionDetailsWindow : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_windowTitle;

		[SerializeField] GenericTextField m_sessionName;
		[SerializeField] GenericTimeField m_sessionMonth;
		[SerializeField] GenericDropdownField m_sessionType;
		[SerializeField] GenericBoundsField m_sessionBounds;

		[SerializeField] GameObject m_buttonSection;
		[SerializeField] Button m_sessionCreateButton;
		[SerializeField] Button m_sessionCancelButton;

		[SerializeField] GameObject m_qrCodeSection;
		[SerializeField] RawImage m_qrCode;
		[SerializeField] Button m_qrCodeFullscreenButton;
		[SerializeField] GameObject m_fullscreenQRPrefab;

		[SerializeField] GameObject m_connectionSection;
		[SerializeField] GenericTextField m_connectionId;
		[SerializeField] GenericTextField m_connectionSession;
		[SerializeField] GenericTextField m_connectionDockerAPI;
		[SerializeField] GenericTextField m_connectionPort;
		[SerializeField] GenericTextField m_connectionDockerContainer;

		ImmersiveSessionsWindow m_baseWindow;

		public void Initialise(ImmersiveSessionsWindow a_baseWindow)
		{
			m_baseWindow = a_baseWindow;
			m_sessionName.Initialise("Session Name", 5, null, "Name");
			m_sessionMonth.Initialise("Month", 5, null);
			m_sessionType.Initialise("Type", 5, null, Enum.GetNames(typeof(ImmersiveSession.ImmersiveSessionType)));
			m_sessionBounds.Initialise("Area", 4, 5, 25000, null);

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
			m_qrCodeFullscreenButton.onClick.AddListener(OnQRFullscreenPressed);
			m_sessionCancelButton.onClick.AddListener(() => gameObject.SetActive(false));
		}

		public void SetToSession(ImmersiveSession a_session)
		{
			m_windowTitle.text = "Immersive Session Details";
			m_sessionName.SetContent(a_session.name);
			m_sessionMonth.SetContent(a_session.month);
			m_sessionType.SetContent((int)a_session.type);
			m_sessionBounds.SetContent(new Vector4(a_session.bottomLeftX, a_session.bottomLeftY, a_session.topRightX, a_session.topRightY));
			SetCreationElementsInteractable(false);

			if (a_session.connection != null)
			{
				m_connectionSection.SetActive(true);
				m_connectionId.SetContent(a_session.connection.id.ToString());
				m_connectionSession.SetContent(a_session.connection.session);
				m_connectionDockerAPI.SetContent(a_session.connection.dockerApiID.ToString());
				m_connectionPort.SetContent(a_session.connection.port.ToString());
				m_connectionDockerContainer.SetContent(a_session.connection.dockerContainerID.ToString());
				m_qrCodeSection.SetActive(true);
				m_qrCode.texture = GenerateQR(a_session);
			}
			else
			{
				m_connectionSection.SetActive(false);
				m_qrCodeSection.SetActive(false);
			}
			m_buttonSection.gameObject.SetActive(false);

			gameObject.SetActive(true);
		}

		public void StartCreatingNewSession()
		{
			m_windowTitle.text = "Create Immersive Session";
			m_sessionName.SetContent(null);
			m_sessionMonth.SetContent(TimeManager.Instance.GetCurrentMonth());
			m_sessionType.SetContent(0);
			m_sessionBounds.SetContent(new Vector4(0f, 0f));
			SetCreationElementsInteractable(true);

			m_buttonSection.gameObject.SetActive(true);
			m_qrCodeSection.SetActive(false);
			m_connectionSection.SetActive(false);
			gameObject.SetActive(true);
		}

		void SetCreationElementsInteractable(bool a_value)
		{
			m_sessionName.SetInteractable(a_value);
			m_sessionMonth.SetInteractable(a_value);
			m_sessionType.SetInteractable(a_value);
			m_sessionBounds.SetInteractable(a_value);
			m_sessionCreateButton.interactable = a_value;
			m_sessionCancelButton.interactable = a_value;
		}

		void OnCreateButtonPressed()
		{
			SetCreationElementsInteractable(false);
			ImmersiveSessionSubmit newSession = new ImmersiveSessionSubmit();
			newSession.name = m_sessionName.CurrentValue;
			newSession.month = m_sessionMonth.CurrentValue;
			newSession.type = (ImmersiveSession.ImmersiveSessionType)m_sessionType.CurrentValue;
			Vector4 bounds = m_sessionBounds.CurrentValue;
			newSession.bottomLeftX = bounds.x;
			newSession.bottomLeftY = bounds.y;
			newSession.topRightX = bounds.z;
			newSession.topRightY = bounds.w;

			//NetworkForm form = new NetworkForm();
			//form.AddField("name", m_sessionName.CurrentValue);
			//form.AddField("month", m_sessionMonth.CurrentValue);
			//form.AddField("type", m_sessionType.CurrentValue);
			//form.AddField("bottom_left_x", bounds.x.ToString());
			//form.AddField("bottom_left_y", bounds.y.ToString());
			//form.AddField("top_right_x", bounds.z.ToString());
			//form.AddField("top_right_y", bounds.w.ToString());
			ServerCommunication.Instance.DoRequestRaw<ImmersiveSession>(Server.ImmersiveSessions(), JsonConvert.SerializeObject(newSession), SessionCreationSuccess, SessionCreationFailure);
		}

		void SessionCreationSuccess(ImmersiveSession a_session)
		{
			m_baseWindow.AddAndSelectSession(a_session);
		}

		void SessionCreationFailure(ARequest request, string message)
		{
			if (request.retriesRemaining > 0)
			{
				Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
				ServerCommunication.Instance.RetryRequest(request);
			}
			else
			{
				SetCreationElementsInteractable(true);
				Debug.LogError($"Request failed with code {request.Www.responseCode.ToString()}: {message ?? ""}");
			}
		}

		void OnQRFullscreenPressed()
		{
			FullscreenQR fs = Instantiate(m_fullscreenQRPrefab, InterfaceCanvas.Instance.canvas.transform).GetComponent<FullscreenQR>();
			fs.m_qrImage.texture = m_qrCode.texture;
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		Texture2D GenerateQR(ImmersiveSession a_session)
		{
			QRCodeGenerator qrGenerator = new QRCodeGenerator();
			QRCodeData qrCodeData = qrGenerator.CreateQrCode($"{{\"ip\":\"{a_session.connection.session}\",\"port\":{a_session.connection.port}}}", QRCodeGenerator.ECCLevel.Q);
			UnityQRCode qrCode = new UnityQRCode(qrCodeData);
			return qrCode.GetGraphic(20);
		}
	}
}
