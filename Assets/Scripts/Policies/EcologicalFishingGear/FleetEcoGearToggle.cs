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
		[SerializeField] Image m_countryBall;
		[SerializeField] Toggle m_toggle;

		public bool Value => m_toggle.isOn;
		public bool Interactable => m_toggle.interactable;


		public void SetContent(string a_name, int a_countryId, bool a_currentValue, bool a_previousValue, bool a_interactable)
		{
			m_nameText.text = a_name;
			m_toggle.isOn = a_currentValue;
			m_toggle.interactable = a_interactable;
			if(a_countryId > 0)
				m_countryBall.color = SessionManager.Instance.GetTeamByTeamID(a_countryId).color;
		}

	}
}
