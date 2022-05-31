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
		private GameSession m_session;

		void Start()
		{
			m_barToggle.onValueChanged.AddListener(BarToggled);
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

			//TODO: set other entries
		}

		void ConnectPressed()
		{

		}

		void BarToggled(bool a_isOn)
		{

		}
	}
}
