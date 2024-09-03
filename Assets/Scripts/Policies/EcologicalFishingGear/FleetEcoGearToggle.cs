using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class FleetEcoGearToggle : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_nameText;
		[SerializeField] Toggle m_toggle;

		bool m_previousValue;

		private void Start()
		{
			m_toggle.onValueChanged.AddListener(OnToggled);
		}

		public void SetContent(string a_name, bool a_currentValue, bool a_previousValue)
		{
			m_nameText.text = a_name;
			m_previousValue = a_previousValue;
			m_toggle.isOn = a_currentValue;
			CheckChanged();
		}

		void OnToggled(bool a_value)
		{
			CheckChanged();
		}

		void CheckChanged()
		{
			//TODO: highlight changed values
		}
	}
}
