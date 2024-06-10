using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.Utilities;

namespace MSP2050.Scripts
{
	public class CountryEcoGearToggles : MonoBehaviour
	{
		[SerializeField] GameObject m_fleetEcoGearPrefab;
		[SerializeField] Transform m_contentContainer;

		List<FleetEcoGearToggle> m_fleetGearToggles;

		public void Initialise(Team a_team)
		{
			List<CountryFleetInfo> fleetsForCountry = PolicyLogicFishing.Instance.GetFleetsForCountry(a_team.ID);
			string[] gear = PolicyLogicFishing.Instance.GetGearTypes();
			m_fleetGearToggles = new List<FleetEcoGearToggle>(fleetsForCountry.Count);

			foreach(CountryFleetInfo countryFleet in fleetsForCountry)
			{
				FleetEcoGearToggle toggle = Instantiate(m_fleetEcoGearPrefab, m_contentContainer).GetComponent<FleetEcoGearToggle>();
				m_fleetGearToggles.Add(toggle);
				toggle.m_nameText.text = gear[countryFleet.gear_type];
			}
		}
	}
}
