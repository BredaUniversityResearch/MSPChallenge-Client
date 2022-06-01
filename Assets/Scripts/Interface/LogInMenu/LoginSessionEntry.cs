using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

		private GameSession m_session;
		private Action<GameSession> m_connectCallback;

		void Start()
		{
			m_barToggle.onValueChanged.AddListener(BarToggled);
			m_connectButton.onClick.AddListener(ConnectPressed);
		}

		public void SetToSession(GameSession a_session, ToggleGroup a_toggleGroup, Action<GameSession> a_connectCallback)
		{
			m_connectCallback = a_connectCallback;
			m_barToggle.group = a_toggleGroup;
			m_barToggle.isOn = false;
			gameObject.SetActive(true);
			m_session = a_session;
			m_sessionNameText.text = a_session.name;
			if (a_session.session_state == GameSession.SessionState.Healthy)
				m_sessionStateText.text = a_session.game_state.ToString();
			else
				m_sessionStateText.text = a_session.session_state.ToString();

			m_realTimeText.text = $"{a_session.GetStartTime()} - {a_session.GetEndTime()}";
			m_configNameText.text = $"{a_session.config_file_name} v{a_session.config_version_version}";
			m_gameTimeText.text = $"{a_session.game_start_year} - {(a_session.game_start_year + (a_session.game_end_month / 12))}";
			m_playersText.text = a_session.players_active.ToString();
		}

		void ConnectPressed()
		{
			m_connectCallback?.Invoke(m_session);
		}

		void BarToggled(bool a_isOn)
		{
			m_expandContent.SetActive(a_isOn);
			m_connectButton.gameObject.SetActive(a_isOn);

			GetComponent<LayoutElement>().preferredHeight = a_isOn ? m_expandedHeight : m_collapsedHeight;
		}
	}
}
