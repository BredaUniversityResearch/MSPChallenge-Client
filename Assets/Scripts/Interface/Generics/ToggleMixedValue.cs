using UnityEngine;
using UnityEngine.UI;
using System;

namespace MSP2050.Scripts
{
	public class ToggleMixedValue : MonoBehaviour
	{
		[SerializeField] CustomToggle m_toggle;
		[SerializeField] GameObject m_mixedValueIcon;
		bool m_ignoreCallback;

		public Action<bool> m_onValueChangeCallback;

		public bool? Value
		{
			get { return m_mixedValueIcon.activeSelf ? null : m_toggle.isOn; }
			set
			{
				m_ignoreCallback = true;
				if (value.HasValue)
				{
					m_mixedValueIcon.SetActive(false);
					m_toggle.isOn = value.Value;
				}
				else
				{
					m_mixedValueIcon.SetActive(true);
					m_toggle.isOn = false;
				}
				m_ignoreCallback = false;
			}
		}

		private void Start()
		{
			m_toggle.onValueChanged.AddListener(OnToggleClicked);
		}

		void OnToggleClicked(bool a_newValue)
		{
			if (m_ignoreCallback)
				return;
			m_mixedValueIcon.SetActive(false);
			m_onValueChangeCallback(a_newValue);
		}
	}
}
