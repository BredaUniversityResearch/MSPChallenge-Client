﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class LoginSessionEntry : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI m_sessionNameText;
		[SerializeField] private TextMeshProUGUI m_sessionStateText;
		[SerializeField] private TextMeshProUGUI m_configNameText;
		[SerializeField] private TextMeshProUGUI m_gameTimeText;
		[SerializeField] private TextMeshProUGUI m_realTimeText;
		[SerializeField] private TextMeshProUGUI m_playersText;
		[SerializeField] private CustomToggle m_barToggle;
		[SerializeField] private CustomButton m_connectButton;
		[SerializeField] private GameObject m_expandContent;
		[SerializeField] private float m_collapsedHeight;
		[SerializeField] private float m_expandedHeight;
		[SerializeField] private DynamicLogo m_regionLogo;

		private GameSession m_session;

		void Start()
		{
			m_barToggle.onValueChanged.AddListener(OnBarToggled);
			m_connectButton.onClick.AddListener(ConnectPressed);
		}

		public void SetToSession(GameSession a_session, ToggleGroup a_toggleGroup)
		{
			m_barToggle.group = a_toggleGroup;
			m_barToggle.isOn = false;
			gameObject.SetActive(true);
			m_session = a_session;
			m_sessionNameText.text = a_session.name;
			if (a_session.session_state == GameSession.SessionState.Healthy)
				m_sessionStateText.text = a_session.game_state.ToString();
			else
				m_sessionStateText.text = a_session.session_state.ToString();

			int monthZeroBased = a_session.game_current_month;
			if (monthZeroBased == -1) // setup month
			{
				monthZeroBased = 0;
			}

			int year = monthZeroBased / 12 + a_session.game_start_year;
			int month = monthZeroBased % 12 + 1;

			m_realTimeText.text = $"{a_session.GetStartTime()} - {a_session.GetEndTime()}";
			m_configNameText.text = a_session.edition_name;
			m_gameTimeText.text = $"{a_session.game_start_year} - {(a_session.game_start_year + (a_session.game_end_month / 12))} ({Localisation.DateFormatting.GetMonthName(month).Substring(0, 3)} {year})";
			m_playersText.text = a_session.players_active.ToString();
			m_regionLogo.SetContent(a_session.edition_colour, a_session.edition_letter);
		}

		void ConnectPressed()
		{
			LoginManager.Instance.ConnectPressedForSession(m_session);
		}

		public GameSession GetSession()
		{
			return m_session;
		}

		void OnBarToggled(bool a_isOn)
		{
			m_expandContent.SetActive(a_isOn);
			m_connectButton.gameObject.SetActive(a_isOn);

			GetComponent<LayoutElement>().preferredHeight = a_isOn ? m_expandedHeight : m_collapsedHeight;
		}

		public void SetSelected(bool a_selected)
		{
			m_barToggle.isOn = a_selected;
		}
	}
}
