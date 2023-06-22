using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reactive.Joins;

namespace MSP2050.Scripts
{
	public class AP_Communication : AP_PopoutWindow
	{
		[SerializeField] Transform m_messageParent;
		[SerializeField] GameObject m_messagePrefab;
		[SerializeField] TMP_InputField m_chatInputField;
		[SerializeField] Button m_sendButton;
		[SerializeField] ScrollRect m_messageScrollRect;

		List<AP_CommunicationMessage> m_messageObjects = new List<AP_CommunicationMessage>();
		int m_nextMessageIndex;

		protected override void Start()
		{
			base.Start();
			m_sendButton.onClick.AddListener(SendMessage);
		}

		protected void Update()
		{
			if (gameObject.activeInHierarchy && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
			{
				SendMessage();
			}
		}
		private void OnDisable()
		{
			if (m_plan != null)
			{
				m_plan.OnMessageReceivedCallback -= OnMessageReceived;
				m_plan = null;
			}
		}

		public override void OpenToContent(Plan a_plan, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			if (m_plan != null)
			{
				m_plan.OnMessageReceivedCallback -= OnMessageReceived;
				m_plan = null;
			}
			base.OpenToContent(a_plan, a_toggle, a_APWindow);

			m_plan.OnMessageReceivedCallback += OnMessageReceived;

			m_nextMessageIndex = 0;
			for(; m_nextMessageIndex < a_plan.PlanMessages.Count; m_nextMessageIndex++)
			{
				SetMessageEntry(a_plan.PlanMessages[m_nextMessageIndex]);
			}
			for(int i = m_nextMessageIndex; i < m_messageObjects.Count; i++)
			{
				m_messageObjects[i].gameObject.SetActive(false);
			}

			Canvas.ForceUpdateCanvases();
			m_messageScrollRect.verticalNormalizedPosition = 0f;
			Canvas.ForceUpdateCanvases();
		}

		void SetMessageEntry(PlanMessage a_message)
		{
			if (m_nextMessageIndex < m_messageObjects.Count)
			{
				m_messageObjects[m_nextMessageIndex].SetToContent(a_message);
			}
			else
			{
				AP_CommunicationMessage newMessage = Instantiate(m_messagePrefab, m_messageParent).GetComponent<AP_CommunicationMessage>();
				newMessage.SetToContent(a_message);
				m_messageObjects.Add(newMessage);
			}
		}

		void OnMessageReceived(PlanMessage a_message)
		{
			SetMessageEntry(a_message);
			m_nextMessageIndex++;

			if(m_messageScrollRect.verticalNormalizedPosition < 0.01f)
            {
				Canvas.ForceUpdateCanvases();
				m_messageScrollRect.verticalNormalizedPosition = 0f;
				Canvas.ForceUpdateCanvases();
			}

		}

		void SendMessage()
		{
			if (!string.IsNullOrEmpty(m_chatInputField.text))
			{
				m_plan.SendMessage(m_chatInputField.text);
				m_chatInputField.text = "";
				m_chatInputField.ActivateInputField();
				m_chatInputField.Select();

				Canvas.ForceUpdateCanvases();
				m_messageScrollRect.verticalNormalizedPosition = 0f;
				Canvas.ForceUpdateCanvases();
			}
		}
	}
}
