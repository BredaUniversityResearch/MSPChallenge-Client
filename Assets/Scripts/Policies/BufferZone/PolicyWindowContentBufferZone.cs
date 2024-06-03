using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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
		bool m_ignoreCallback;
		float m_currentRadius;
		Dictionary<Entity, PolicyGeometryDataBufferZone> m_originalValues;
		Dictionary<Entity, PolicyGeometryDataBufferZone> m_newValues;

		void Initialise()
		{
			if (m_initialised)
				return;

			//Spawn toggle, set callbacks
			m_fleetGroups = new List<FleetMixedToggleGroup>(PolicyLogicFishing.Instance.GetGearTypes().Length);
			for(int i = 0; i < PolicyLogicFishing.Instance.GetGearTypes().Length; i++)
			{
				FleetMixedToggleGroup fleetGroup = Instantiate(m_fleetGroupPrefab, m_fleetGroupParent).GetComponent<FleetMixedToggleGroup>();
				fleetGroup.SetFleet(i, OnFleetChanged, OnCountryChanged, OnMonthChanged);
				m_fleetGroups.Add(fleetGroup);
			}
			m_radiusInput.onEndEdit.AddListener(OnRadiusChanged);
		}

		void OnFleetChanged(int a_gearId, bool a_value)
		{
			if (a_value)
			{ 
				foreach(var entityVP in m_newValues)
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
					if(countryDict.TryGetValue(a_countryId, out Months oldMonths))
					{ 
						if(a_value)
						{
							countryDict[a_countryId] = (Months)((int)oldMonths | (1 << a_month));//TODO: is Month+1 needed here?
						}
						else
						{
							countryDict[a_countryId] = (Months)((int)oldMonths & ~(1 << a_month));//TODO: is Month+1 needed here?
						}
					}
					else if(a_value)
					{
						countryDict[a_countryId] = (Months)(1 << a_month);//TODO: is Month+1 needed here?
					}
				}
			}
		}

		void OnRadiusChanged(string a_newValue)
		{
			if (m_ignoreCallback)
				return;

			float result = float.NegativeInfinity;
			if(float.TryParse(a_newValue, out result))
			{
				if (result >= 0)
				{
					m_currentRadius = result;
					foreach (var entityVP in m_newValues)
					{
						entityVP.Value.radius = m_currentRadius;
					}
				}
			}
			SetRadiusText();
		}

		public override Dictionary<Entity, string> GetChanges()
		{
			Dictionary<Entity, string> results = new Dictionary<Entity, string>();
			foreach (var kvp in m_newValues)
			{
				if(m_originalValues.TryGetValue(kvp.Key, out var newValue))
				{
					if(!newValue.ContentIdentical(kvp.Value))
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
			m_originalValues = new Dictionary<Entity, PolicyGeometryDataBufferZone>();
			m_newValues = new Dictionary<Entity, PolicyGeometryDataBufferZone>();
			m_currentRadius = 0f;
			bool first = true;
			foreach(var kvp in a_values)
			{
				PolicyGeometryDataBufferZone data = new PolicyGeometryDataBufferZone(kvp.Value);
				m_originalValues.Add(kvp.Key, data);
				m_newValues.Add(kvp.Key, data.GetValueCopy());
				if(first)
				{
					m_currentRadius = data.radius;
					first = false;
				}
				else if(m_currentRadius >= 0 && Mathf.Abs(data.radius - m_currentRadius) > 0.01f)
				{
					m_currentRadius = Mathf.NegativeInfinity;
				}
			}
			foreach(Entity e in a_geometry)
			{
				if (!a_values.ContainsKey(e))
				{
					//Create empty entries for geometry that doesnt have a value yet
					m_newValues.Add(e, new PolicyGeometryDataBufferZone());
				}
			}
			foreach(FleetMixedToggleGroup fleetGroup in m_fleetGroups)
			{
				fleetGroup.SetValues(m_newValues);
			}
			SetRadiusText();
		}

		void SetRadiusText()
		{
			m_ignoreCallback = true;
			if (m_currentRadius >= 0)
			{
				m_radiusInput.text = m_currentRadius.ToString("N2");
			}
			else
			{
				m_radiusInput.text = "~";
			}
			m_ignoreCallback = false;
		}
	}
}
