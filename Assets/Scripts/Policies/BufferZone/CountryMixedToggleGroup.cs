using UnityEngine;
using UnityEngine.UI;
using System;

namespace MSP2050.Scripts
{
	public class CountryMixedToggleGroup : MonoBehaviour
	{
		[SerializeField] ToggleMixedValue m_valueToggle;
		[SerializeField] Toggle m_barToggle;
		[SerializeField] GameObject m_contentContainer;
		[SerializeField] TMPro.TextMeshProUGUI m_nameText;
		[SerializeField] Toggle[] m_monthToggles;

		public GameObject ContentContainer => m_contentContainer;

		public void SetFleet(string a_fleet, Action<bool> a_callback)
		{
			m_valueToggle.m_onValueChangeCallback = a_callback;
			m_nameText.text = a_fleet;
		}

		public void SetValue()
		{

		}
	}
}
