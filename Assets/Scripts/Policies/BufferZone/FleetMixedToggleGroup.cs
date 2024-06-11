using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.Analytics;

namespace MSP2050.Scripts
{
	public class FleetMixedToggleGroup : MonoBehaviour
	{
		[SerializeField] ToggleMixedValue m_valueToggle;
		[SerializeField] Toggle m_barToggle;
		[SerializeField] TMPro.TextMeshProUGUI m_nameText;

		[SerializeField] GameObject m_contentContainer;
		[SerializeField] GameObject m_countryTogglePrefab;
		[SerializeField] GameObject m_monthsTogglePrefab;

		List<CountryMixedToggleGroup> m_countryToggles;
		MonthsMixedToggleGroup m_monthToggles;
		List<CountryFleetInfo> m_countryFleetInfo;

		bool m_usingCountries;
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
			m_gearId = a_gearId;
			m_fleetCallback = a_fleetCallback;
			m_countryCallback = a_countryCallback;
			m_monthCallback = a_monthCallback;
			m_nameText.text = PolicyLogicFishing.Instance.GetGearTypes()[a_gearId];
			m_countryFleetInfo = PolicyLogicFishing.Instance.GetFleetsForGear(a_gearId);

			if(m_countryFleetInfo.Count == 0)
			{
				Debug.LogError("No fleets defined for gear: " + m_nameText.text);
			}
			else if(m_countryFleetInfo.Count == 1)
			{
				m_usingCountries = false;
				m_monthToggles = Instantiate(m_monthsTogglePrefab, ContentContainer.transform).GetComponent<MonthsMixedToggleGroup>();
				m_monthToggles.m_monthChangedCallback = OnMonthChanged;
			}
			else
			{
				m_usingCountries = true;
				m_countryToggles = new List<CountryMixedToggleGroup>();
				foreach(CountryFleetInfo countryFleet in m_countryFleetInfo)
				{
					CountryMixedToggleGroup countryGroup = Instantiate(m_countryTogglePrefab, ContentContainer.transform).GetComponent<CountryMixedToggleGroup>();
					m_countryToggles.Add(countryGroup);
					countryGroup.SetCountry(SessionManager.Instance.GetTeamByTeamID(countryFleet.country_id), OnCountryChanged, OnCountryMonthChanged);
				}
			}
		}

		public void SetValues(Dictionary<Entity, PolicyGeometryDataBufferZone> a_values)
		{
			m_ignoreCallback = true;
			for(int i = 0; i < m_countryFleetInfo.Count; i++)
			{
				//Collect all values for country
				List<Months> countryValuesResult = new List<Months>();
				foreach(var entityVP in a_values)
				{
					if(entityVP.Value != null && entityVP.Value.fleets.TryGetValue(m_gearId, out Dictionary<int, Months> fleetCountry))
					{
						if (fleetCountry.TryGetValue(m_countryFleetInfo[i].country_id, out Months countryMonths))
						{
							countryValuesResult.Add(countryMonths);
						}
					}
				}
				if (m_usingCountries)
				{
					m_countryToggles[i].SetValue(countryValuesResult);
				}
				else
				{
					m_valueToggle.Value = m_monthToggles.SetValue(countryValuesResult);
				}
			}
			m_ignoreCallback = false;
			if (m_usingCountries)
			{
				DetermineFleetToggle();
			}
		}

		public void SetValues(Dictionary<Entity, PolicyGeometryDataSeasonalClosure> a_values)
		{
			m_ignoreCallback = true;
			for (int i = 0; i < m_countryFleetInfo.Count; i++)
			{
				//Collect all values for country
				List<Months> countryValuesResult = new List<Months>();
				foreach (var entityVP in a_values)
				{
					if (entityVP.Value != null && entityVP.Value.fleets.TryGetValue(m_gearId, out Dictionary<int, Months> fleetCountry))
					{
						if (fleetCountry.TryGetValue(m_countryFleetInfo[i].country_id, out Months countryMonths))
						{
							countryValuesResult.Add(countryMonths);
						}
					}
				}
				if (m_usingCountries)
				{
					m_countryToggles[i].SetValue(countryValuesResult);
				}
				else
				{
					m_valueToggle.Value = m_monthToggles.SetValue(countryValuesResult);
				}
			}
			m_ignoreCallback = false;
			if (m_usingCountries)
			{
				DetermineFleetToggle();
			}
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
			if (m_usingCountries)
			{
				foreach (CountryMixedToggleGroup countryToggle in m_countryToggles)
				{
					countryToggle.SetValue(a_newValue);
				}
			}
			else
			{
				m_monthToggles.CombinedValue = a_newValue;
			}
			m_ignoreCallback = false;
		}

		void OnCountryChanged(int a_countryId, bool a_value)
		{
			if (m_ignoreCallback)
				return;
			DetermineFleetToggle();
			m_countryCallback.Invoke(m_gearId, a_countryId, a_value);
		}

		void OnCountryMonthChanged(int a_countryId, int a_month, bool a_value)
		{
			if (m_ignoreCallback)
				return;
			DetermineFleetToggle();
			m_monthCallback.Invoke(m_gearId, a_countryId, a_month, a_value);
		}

		void OnMonthChanged(bool a_value, int a_month)
		{
			if (m_ignoreCallback)
				return;
			DetermineFleetToggle();
			m_monthCallback.Invoke(m_gearId, m_countryFleetInfo[0].country_id, a_month, a_value);
		}

		void DetermineFleetToggle()
		{
			m_ignoreCallback = true;
			if (m_usingCountries)
			{
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
			}
			else
			{
				m_valueToggle.Value = m_monthToggles.CombinedValue;
			}
			m_ignoreCallback = false;
		}
	}
}
