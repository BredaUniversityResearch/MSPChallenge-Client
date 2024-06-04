using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyWindowContentSeasonalClosure : AGeometryPolicyWindowContent
	{
		[SerializeField] GameObject m_fleetGroupPrefab;
		[SerializeField] Transform m_fleetGroupParent;

		bool m_initialised;
		float m_currentRadius;
		List<FleetMixedToggleGroup> m_fleetGroups;
		Dictionary<Entity, PolicyGeometryDataSeasonalClosure> m_originalValues;
		Dictionary<Entity, PolicyGeometryDataSeasonalClosure> m_newValues;

		void Initialise()
		{
			if (m_initialised)
				return;

			//Spawn toggle, set callbacks
			m_fleetGroups = new List<FleetMixedToggleGroup>(PolicyLogicFishing.Instance.GetGearTypes().Length);
			for (int i = 0; i < PolicyLogicFishing.Instance.GetGearTypes().Length; i++)
			{
				FleetMixedToggleGroup fleetGroup = Instantiate(m_fleetGroupPrefab, m_fleetGroupParent).GetComponent<FleetMixedToggleGroup>();
				fleetGroup.SetFleet(i, OnFleetChanged, OnCountryChanged, OnMonthChanged);
				m_fleetGroups.Add(fleetGroup);
			}
		}

		void OnFleetChanged(int a_gearId, bool a_value)
		{
			if (a_value)
			{
				foreach (var entityVP in m_newValues)
				{
					Dictionary<int, Months> countryDict = null;
					if (!entityVP.Value.fleets.TryGetValue(a_gearId, out countryDict))
					{
						countryDict = new Dictionary<int, Months>();
						entityVP.Value.fleets.Add(a_gearId, countryDict);
					}
					foreach (Team team in SessionManager.Instance.GetTeams())
					{
						if (team.IsManager)
							continue;
						countryDict[team.ID] = (Months)int.MaxValue;
					}
				}
			}
			else
			{
				foreach (var entityVP in m_newValues)
				{
					entityVP.Value.fleets.Remove(a_gearId);
				}
			}
		}
		void OnCountryChanged(int a_gearId, int a_countryId, bool a_value)
		{
			foreach (var entityVP in m_newValues)
			{
				if (entityVP.Value.fleets.TryGetValue(a_gearId, out var countryDict))
				{
					if (a_value)
					{
						countryDict[a_countryId] = (Months)int.MaxValue;
					}
					else
					{
						countryDict.Remove(a_countryId);
					}
				}
			}
		}
		void OnMonthChanged(int a_gearId, int a_countryId, int a_month, bool a_value)
		{
			foreach (var entityVP in m_newValues)
			{
				if (entityVP.Value.fleets.TryGetValue(a_gearId, out var countryDict))
				{
					if (countryDict.TryGetValue(a_countryId, out Months oldMonths))
					{
						if (a_value)
						{
							countryDict[a_countryId] = (Months)((int)oldMonths | (1 << a_month));//TODO: is Month+1 needed here?
						}
						else
						{
							countryDict[a_countryId] = (Months)((int)oldMonths & ~(1 << a_month));//TODO: is Month+1 needed here?
						}
					}
					else if (a_value)
					{
						countryDict[a_countryId] = (Months)(1 << a_month);//TODO: is Month+1 needed here?
					}
				}
			}
		}

		public override Dictionary<Entity, string> GetChanges()
		{
			Dictionary<Entity, string> results = new Dictionary<Entity, string>();
			foreach (var kvp in m_newValues)
			{
				if (m_originalValues.TryGetValue(kvp.Key, out var newValue))
				{
					if (!newValue.ContentIdentical(kvp.Value))
					{
						results.Add(kvp.Key, kvp.Value.GetJson());
					}
				}
				else
				{
					results.Add(kvp.Key, kvp.Value.GetJson());
				}
			}
			return results;
		}

		public override void SetContent(Dictionary<Entity, string> a_values, List<Entity> a_geometry)
		{
			Initialise();
			m_originalValues = new Dictionary<Entity, PolicyGeometryDataSeasonalClosure>();
			m_newValues = new Dictionary<Entity, PolicyGeometryDataSeasonalClosure>();
			foreach (var kvp in a_values)
			{
				PolicyGeometryDataSeasonalClosure data = new PolicyGeometryDataSeasonalClosure(kvp.Value);
				m_originalValues.Add(kvp.Key, data);
				m_newValues.Add(kvp.Key, data.GetValueCopy());
			}
			foreach (Entity e in a_geometry)
			{
				if (!a_values.ContainsKey(e))
				{
					//Create empty entries for geometry that doesnt have a value yet
					m_newValues.Add(e, new PolicyGeometryDataSeasonalClosure());
				}
			}
			foreach (FleetMixedToggleGroup fleetGroup in m_fleetGroups)
			{
				fleetGroup.SetValues(m_newValues);
			}
		}
	}
}
