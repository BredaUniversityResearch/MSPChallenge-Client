using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class FleetMixedToggleGroup : MonoBehaviour
	{
		[SerializeField] ToggleMixedValue m_valueToggle;
		[SerializeField] Toggle m_barToggle;
		[SerializeField] TMPro.TextMeshProUGUI m_nameText;

		[SerializeField] GameObject m_contentContainer;
		[SerializeField] GameObject m_countryTogglePrefab;

		List<CountryMixedToggleGroup> m_countryToggles;
		bool m_ignoreCallback;
		Action<int, bool> m_fleetCallback;
		Action<int, int, bool> m_countryCallback;
		Action<int, int, int, bool> m_monthCallback;
		int m_gearId;

		public GameObject ContentContainer => m_contentContainer;

		private void Start()
		{
			m_valueToggle.m_onValueChangeCallback = OnValueToggleChanged;
			m_barToggle.onValueChanged.AddListener(OnExpandToggled);
		}

		public void SetFleet(int a_gearId, 
			Action<int, bool> a_fleetCallback,
			Action<int, int, bool> a_countryCallback,
			Action<int, int, int, bool> a_monthCallback)
		{
			m_fleetCallback = a_fleetCallback;
			m_countryCallback = a_countryCallback;
			m_monthCallback = a_monthCallback;
			m_nameText.text = PolicyLogicFishing.Instance.GetGearTypes()[a_gearId];
			m_countryToggles = new List<CountryMixedToggleGroup>();

			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (team.IsManager)
					continue;
				CountryMixedToggleGroup countryGroup = Instantiate(m_countryTogglePrefab, ContentContainer.transform).GetComponent<CountryMixedToggleGroup>();
				m_countryToggles.Add(countryGroup);
				countryGroup.SetCountry(a_gearId, team, OnCountryChanged, OnMonthChanged);
			}
		}

		public void SetValues(Dictionary<Entity, PolicyGeometryDataBufferZone> a_values)
		{
			m_ignoreCallback = true;
			int i = 0; //Not all countries appear in list, so keep seperate
			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (team.IsManager)
					continue;

				//Collect all values for country
				List<Months> countryValuesResult = new List<Months>();
				foreach(var entityVP in a_values)
				{
					if(entityVP.Value.fleets.TryGetValue(m_gearId, out Dictionary<int, Months> fleetCountry))
					{
						if (fleetCountry.TryGetValue(team.ID, out Months countryMonths))
						{
							countryValuesResult.Add(countryMonths);
						}
					}
				}
				m_countryToggles[i].SetValue(countryValuesResult);
				i++;
			}
			m_ignoreCallback = false;
			DetermineFleetToggle();
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
			m_fleetCallback.Invoke(m_gearId, a_newValue);
			foreach(CountryMixedToggleGroup countryToggle in m_countryToggles)
			{
				countryToggle.SetValue(a_newValue);
			}
			m_ignoreCallback = false;
		}

		void OnCountryChanged(int a_gearId, int a_countryId, bool a_value)
		{
			if (m_ignoreCallback)
				return;
			DetermineFleetToggle();
			m_countryCallback.Invoke(a_gearId, a_countryId, a_value);
		}

		void OnMonthChanged(int a_gearId, int a_countryId, int a_month, bool a_value)
		{
			if (m_ignoreCallback)
				return;
			DetermineFleetToggle();
			m_monthCallback.Invoke(a_gearId, a_countryId, a_month, a_value);
		}

		void DetermineFleetToggle()
		{
			m_ignoreCallback = true;
			if (!m_countryToggles[0].Value.HasValue)
			{
				m_valueToggle.Value = null;
				m_ignoreCallback = false;
				return;
			}

			bool reference = m_countryToggles[0].Value.Value;
			for (int i = 1; i < m_countryToggles.Count; i++)
			{
				if (!m_countryToggles[i].Value.HasValue || m_countryToggles[i].Value.Value != reference)
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
