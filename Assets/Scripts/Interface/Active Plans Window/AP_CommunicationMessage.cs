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
		[SerializeField] TextMeshProUGUI m_team;
		[SerializeField] TextMeshProUGUI m_time;
		[SerializeField] TextMeshProUGUI m_content;
		//[SerializeField] CustomImage m_background;
		//[SerializeField] CustomImage m_lipImage;
		[SerializeField] RectTransform m_lipTransform;

		public void SetToContent(PlanMessage a_message)
		{
			gameObject.SetActive(true);
			m_sender.color = a_message.team.color;
			m_team.color = a_message.team.color;
			m_time.color = a_message.team.color;

			m_sender.text = a_message.user_name;
			m_time.text = a_message.time;
			m_team.text = $"& {a_message.team.name} team";
			m_content.text = a_message.message;

			AlignLip(a_message.team.ID == SessionManager.Instance.CurrentTeam.ID);
		}

		void AlignLip(bool a_right)
		{
			m_lipTransform.rotation = Quaternion.Euler(0, a_right ? -90f : 90f, 0);
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
		}
	}
}
