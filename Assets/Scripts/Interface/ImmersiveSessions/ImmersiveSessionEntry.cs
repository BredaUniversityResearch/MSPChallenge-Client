using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ImmersiveSessionEntry : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_sessionName;
		//[SerializeField] TextMeshProUGUI m_users;
		//[SerializeField] TextMeshProUGUI m_state;
		[SerializeField] Toggle m_barToggle;

		ImmersiveSession m_session;
		bool m_ignoreCallback;
		Action<ImmersiveSession, ImmersiveSessionEntry> m_toggleCallback;

		public void Initialise(Action<ImmersiveSession, ImmersiveSessionEntry> a_toggleCallback)
		{
			m_toggleCallback = a_toggleCallback;
			m_barToggle.onValueChanged.AddListener(OnToggleChanged);
		}

		public void SetToSession(ImmersiveSession a_session)
		{
			m_session = a_session;
			m_sessionName.text = m_session.name;
			gameObject.SetActive(true);
		}

		public void ForceSetToggle(bool a_value)
		{
			m_ignoreCallback = true;
			m_barToggle.isOn = a_value;
			m_ignoreCallback = false;
		}

		void OnToggleChanged(bool a_value)
		{
			if (m_ignoreCallback)
				return;
			m_toggleCallback.Invoke(a_value ? m_session : null, this);
		}
	}
}
