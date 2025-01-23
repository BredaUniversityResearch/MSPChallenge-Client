using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class WidgetOptionToggle : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] TextMeshProUGUI m_name;

		Action<bool> m_callback;

		public void Initialise(string a_name, bool a_value, Action<bool> a_callback)
		{
			m_toggle.isOn = a_value;
			m_name.text = a_name;
			m_callback = a_callback;
			m_toggle.onValueChanged.AddListener(OnToggleChanged);
		}

		void OnToggleChanged(bool a_value)
		{
			m_callback?.Invoke(a_value);
		}
	}
}