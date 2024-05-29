using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class FleetMixedToggleGroup : MonoBehaviour
	{
		[SerializeField] ToggleMixedValue m_valueToggle;
		[SerializeField] Toggle m_barToggle; //TODO: handle expand collapse behaviour
		[SerializeField] TMPro.TextMeshProUGUI m_nameText;

		[SerializeField] GameObject m_contentContainer;
		[SerializeField] GameObject m_countryTogglePrefab;

		public GameObject ContentContainer => m_contentContainer;

		public void SetFleet(string a_fleet, Action<bool> a_callback)
		{
			m_valueToggle.m_onValueChangeCallback = a_callback;
			m_nameText.text = a_fleet;

			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				FleetMixedToggleGroup countryGroup = Instantiate(m_countryTogglePrefab, ContentContainer.transform).GetComponent<FleetMixedToggleGroup>();
				countryGroup.SetFleet(team.name, (b) => OnCountryChanged(fleetId, team.ID, b));
			}
		}

		public void SetValues(Dictionary<int, Months> a_values)
		{

		}
	}
}
