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
		[SerializeField] ToggleMixedValue[] m_monthToggles;

		bool m_ignoreCallback;
		Action<int, int, bool> m_countryCallback;
		Action<int, int, int, bool> m_monthCallback;
		int m_countryId;
		int m_gearId;

		private void Start()
		{
			m_valueToggle.m_onValueChangeCallback = OnValueToggleChanged;
			m_barToggle.onValueChanged.AddListener(OnExpandToggled);
			for(int i = 0; i < m_monthToggles.Length; i++)
			{
				int month = i;
				m_monthToggles[i].m_onValueChangeCallback = (b) => OnMonthToggleChanged(b, month);
			}
		}

		public void SetCountry(int a_gearId, Team a_country, Months a_months, 
			Action<int, int, bool> a_countryCallback,
			Action<int, int, int, bool> a_monthCallback)
		{
			m_gearId = a_gearId;
			m_countryCallback = a_countryCallback;
			m_monthCallback = a_monthCallback;
			m_nameText.text = a_country.name;
			m_countryId = a_country.ID;
			m_barToggle.isOn = false;
			m_contentContainer.SetActive(false);
		}

		public void SetValue(Months a_months)
		{
			m_ignoreCallback = true;
			//TODO: set month toggles
			DetermineCountryToggle();
			m_ignoreCallback = false;
		}

		public void SetValue(bool a_value)
		{
			m_ignoreCallback = true;
			m_valueToggle.Value = a_value;
			//TODO: set month toggles
			m_ignoreCallback = false;
		}

		void OnExpandToggled(bool a_newValue)
		{
			m_contentContainer.SetActive(a_newValue);
		}

		void OnValueToggleChanged(bool a_newValue)
		{
			if (m_ignoreCallback)
				return;

			m_ignoreCallback = true;
			foreach(ToggleMixedValue t in m_monthToggles)
			{
				t.Value = a_newValue;
			}
			m_ignoreCallback = false;

			m_countryCallback.Invoke(m_gearId, m_countryId, a_newValue);
		}

		void OnMonthToggleChanged(bool a_newValue, int a_month)
		{
			if (m_ignoreCallback)
				return;

			DetermineCountryToggle();
			m_monthCallback.Invoke(m_gearId, m_countryId, a_month, a_newValue);
		}

		void DetermineCountryToggle()
		{
			m_ignoreCallback = true;
			if (!m_monthToggles[0].Value.HasValue)
			{
				m_valueToggle.Value = null;
				m_ignoreCallback = false;
				return;
			}

			bool reference = m_monthToggles[0].Value.Value;
			for (int i = 1; i < m_monthToggles.Length; i++)
			{
				if (!m_monthToggles[i].Value.HasValue || m_monthToggles[i].Value.Value != reference)
				{
					m_valueToggle.Value = null;
					m_ignoreCallback = false;
					return;
				}
			}
			m_valueToggle.Value = reference;
			m_ignoreCallback = false;
		}
	}
}
