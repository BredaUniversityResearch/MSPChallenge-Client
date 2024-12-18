using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class CountryMixedToggleGroup : MonoBehaviour
	{
		[SerializeField] ToggleMixedValue m_valueToggle;
		[SerializeField] Toggle m_barToggle;
		[SerializeField] GameObject m_contentContainer;
		[SerializeField] TMPro.TextMeshProUGUI m_nameText;
		[SerializeField] MonthsMixedToggleGroup m_monthToggles;

		bool m_ignoreCallback;
		Action<int, bool> m_countryCallback;
		Action<int, int, bool> m_monthCallback;
		int m_countryId;
		int m_gearId;

		public bool? Value => m_valueToggle.Value;

		private void Start()
		{
			m_valueToggle.m_onValueChangeCallback = OnValueToggleChanged;
			m_barToggle.onValueChanged.AddListener(OnExpandToggled);
			m_monthToggles.m_monthChangedCallback = OnMonthToggleChanged;
		}

		public void SetCountry(Team a_country, 
			Action<int, bool> a_countryCallback,
			Action<int, int, bool> a_monthCallback)
		{
			m_countryCallback = a_countryCallback;
			m_monthCallback = a_monthCallback;
			m_nameText.text = a_country.name;
			m_countryId = a_country.ID;
			m_barToggle.isOn = false;
			m_contentContainer.SetActive(false);
		}

		public void SetInteractable(bool a_interactable)
		{
			m_valueToggle.Interactable = a_interactable;
			m_monthToggles.SetInteractable(a_interactable);
		}

		public bool? SetValue(List<Months> a_months)
		{
			m_ignoreCallback = true;
			m_valueToggle.Value = m_monthToggles.SetValue(a_months);
			m_ignoreCallback = false;
			return m_valueToggle.Value;
		}

		public void SetValue(bool a_value)
		{
			m_ignoreCallback = true;
			m_valueToggle.Value = a_value;
			m_monthToggles.CombinedValue = a_value;
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
			m_monthToggles.CombinedValue = a_newValue;
			m_ignoreCallback = false;

			m_countryCallback.Invoke(m_countryId, a_newValue);
		}

		void OnMonthToggleChanged(bool a_newValue, int a_month)
		{
			if (m_ignoreCallback)
				return;

			m_ignoreCallback = true;
			m_valueToggle.Value = m_monthToggles.CombinedValue;
			m_ignoreCallback = false;
			m_monthCallback.Invoke(m_countryId, a_month, a_newValue);
		}
	}
}
