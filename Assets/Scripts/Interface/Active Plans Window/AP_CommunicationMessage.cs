using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ColourPalette;

namespace MSP2050.Scripts
{
	public class AP_CommunicationMessage : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_sender;
		[SerializeField] TextMeshProUGUI m_time;
		[SerializeField] TextMeshProUGUI m_content;
		[SerializeField] Image m_teamBall;
		[SerializeField] RectTransform m_lipTransform;

		public void SetToContent(PlanMessage a_message)
		{
			gameObject.SetActive(true);
			m_teamBall.color = a_message.team.color;

			m_sender.text = a_message.user_name;
			m_time.text = a_message.time;
			m_content.text = a_message.message;

			AlignLip(a_message.team.ID == SessionManager.Instance.CurrentTeam.ID);
		}

		void AlignLip(bool a_right)
		{
			m_lipTransform.rotation = Quaternion.Euler(0, 0, a_right ? -90f : 90f);
			if (a_right)
			{
				m_lipTransform.anchorMin = new Vector2(1f, 0.5f);
				m_lipTransform.anchorMax = new Vector2(1f, 0.5f);
			}
			else
			{
				m_lipTransform.anchorMin = new Vector2(0f, 0.5f);
				m_lipTransform.anchorMax = new Vector2(0f, 0.5f);
			}
			m_lipTransform.anchoredPosition = Vector2.zero;
			GetComponent<RectTransform>().pivot = a_right ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
		}
	}
}
