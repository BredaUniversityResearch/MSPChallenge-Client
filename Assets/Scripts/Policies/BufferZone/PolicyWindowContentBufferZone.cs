using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class PolicyWindowContentBufferZone : AGeometryPolicyWindowContent
	{
		[SerializeField] CustomInputField m_radiusInput;
		[SerializeField] List<FleetMixedToggleGroup> m_fleetGroups;
		[SerializeField] GameObject m_fleetGroupPrefab;
		[SerializeField] Transform m_fleetGroupParent;

		bool m_initialised;
		Dictionary<Entity, PolicyGeometryDataBufferZone> m_originalValues;
		Dictionary<Entity, PolicyGeometryDataBufferZone> m_newValues;
		List<Entity> m_geometry;
		bool m_ignoreCallbacks;

		void Initialise()
		{
			if (m_initialised)
				return;

			//Spawn toggle, set callbacks
			string[] fleets = new string[5]; //TODO: get the actual value here
			m_fleetGroups = new List<FleetMixedToggleGroup>(fleets.Length);

			for(int i = 0; i < fleets.Length; i++)
			{
				FleetMixedToggleGroup fleetGroup = Instantiate(m_fleetGroupPrefab, m_fleetGroupParent).GetComponent<FleetMixedToggleGroup>();
				int fleetId = i;
				fleetGroup.SetFleet(fleets[i], (b) => OnFleetChanged(fleetId, b));
				m_fleetGroups.Add(fleetGroup);
				foreach(Team team in SessionManager.Instance.GetTeams())
				{
					FleetMixedToggleGroup countryGroup = Instantiate(m_fleetGroupPrefab, fleetGroup.ContentContainer.transform).GetComponent<FleetMixedToggleGroup>();
					countryGroup.SetFleet(team.name, (b) => OnCountryChanged(fleetId, team.ID, b));
				}
			}
		}

		void OnFleetChanged(int a_fleetId, bool a_value)
		{ }
		void OnCountryChanged(int a_fleetId, int a_countryId, bool a_value)
		{ }
		void OnMonthChanged(int a_fleetId, int a_countryId, int a_month, bool a_value)
		{ }

		public override Dictionary<Entity, string> GetChanges()
		{
			Dictionary<Entity, string> results = new Dictionary<Entity, string>();
			foreach (var kvp in m_newValues)
			{
				if(m_originalValues.TryGetValue(kvp.Key, out var newValue))
				{
					if(!newValue.ContentIdentical(kvp.Value))
					{
						results.Add(kvp.Key, JsonConvert.SerializeObject(kvp.Value));
					}
				}
				else
				{
					results.Add(kvp.Key, JsonConvert.SerializeObject(kvp.Value));
				}
			}
			return results;
		}

		public override void SetContent(Dictionary<Entity, string> a_values, List<Entity> a_geometry)
		{
			Initialise();
			m_geometry = a_geometry;
			m_originalValues = new Dictionary<Entity, PolicyGeometryDataBufferZone>();
			m_newValues = new Dictionary<Entity, PolicyGeometryDataBufferZone>();
			foreach(var kvp in a_values)
			{
				PolicyGeometryDataBufferZone data = JsonConvert.DeserializeObject<PolicyGeometryDataBufferZone>(kvp.Value);
				m_originalValues.Add(kvp.Key, data);
				m_newValues.Add(kvp.Key, data.GetValueCopy());

			}
			//TODO: set toggles to values
		}
	}
}
