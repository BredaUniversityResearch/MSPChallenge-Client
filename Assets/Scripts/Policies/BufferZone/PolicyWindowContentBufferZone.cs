using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class PolicyWindowContentBufferZone : AGeometryPolicyWindowContent
	{
		[SerializeField] CustomInputField m_radiusInput;
		[SerializeField] GameObject m_fleetGroupPrefab;
		[SerializeField] Transform m_fleetGroupParent;

		bool m_initialised;
		bool m_ignoreCallback;
		float m_currentRadius;
		List<FleetMixedToggleGroup> m_fleetGroups;
		Dictionary<Entity, PolicyGeometryDataBufferZone> m_policyValues;
		Action<Dictionary<Entity, string>> m_changedCallback;

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
			Dictionary<Entity, string> changes = new Dictionary<Entity, string>();
			if (a_value)
			{ 
				foreach(var entityVP in m_policyValues)
				{
					bool changed = false;
					Dictionary<int, Months> countryDict = null;
					if (!entityVP.Value.fleets.TryGetValue(a_gearId, out countryDict))
					{
						countryDict = new Dictionary<int, Months>();
						entityVP.Value.fleets.Add(a_gearId, countryDict);
						changed = true;
					}
					foreach(var countryFleet in PolicyLogicFishing.Instance.GetFleetsForGear(a_gearId))
					{
						if(!changed && (!countryDict.TryGetValue(countryFleet.country_id, out Months result) || !result.AllMonths()))
							changed = true;
						
						countryDict[countryFleet.country_id] = (Months)MonthsMethods.AllMonthsValue;
					}
					if(changed)
						changes.Add(entityVP.Key, entityVP.Value.GetJson());
				}
			}
			else
			{
				foreach (var entityVP in m_policyValues)
				{
					if(entityVP.Value.fleets.Remove(a_gearId))
					{
						changes.Add(entityVP.Key, entityVP.Value.GetJson());
					}
				}
			}
			if (changes.Count > 0)
				m_changedCallback.Invoke(changes);
		}
		void OnCountryChanged(int a_gearId, int a_countryId, bool a_value)
		{
			Dictionary<Entity, string> changes = new Dictionary<Entity, string>();
			foreach (var entityVP in m_policyValues)
			{
				if (entityVP.Value.fleets.TryGetValue(a_gearId, out var countryDict))
				{
					if (a_value)
					{
						bool changed = !countryDict.TryGetValue(a_countryId, out Months result) || !result.AllMonths();
						countryDict[a_countryId] = (Months)MonthsMethods.AllMonthsValue;
						if(changed)
							changes.Add(entityVP.Key, entityVP.Value.GetJson());
					}
					else
					{
						if(countryDict.Remove(a_countryId))
							changes.Add(entityVP.Key, entityVP.Value.GetJson());
					}
				}
				else if(a_value)
				{
					entityVP.Value.fleets.Add(a_gearId, new Dictionary<int, Months> { { a_countryId, (Months)MonthsMethods.AllMonthsValue } });
					changes.Add(entityVP.Key, entityVP.Value.GetJson());
				}
			}
			if (changes.Count > 0)
				m_changedCallback.Invoke(changes);
		}
		void OnMonthChanged(int a_gearId, int a_countryId, int a_month, bool a_value)
		{
			Dictionary<Entity, string> changes = new Dictionary<Entity, string>();
			foreach (var entityVP in m_policyValues)
			{
				if (entityVP.Value.fleets.TryGetValue(a_gearId, out var countryDict))
				{
					if(countryDict.TryGetValue(a_countryId, out Months oldMonths))
					{ 
						if(a_value)
						{
							Months newMonths = (Months)((int)oldMonths | (1 << a_month));
							countryDict[a_countryId] = newMonths;
							if(oldMonths != newMonths)
								changes.Add(entityVP.Key, entityVP.Value.GetJson());
						}
						else
						{
							Months newMonths = (Months)((int)oldMonths & ~(1 << a_month));
							countryDict[a_countryId] = newMonths;
							if (oldMonths != newMonths)
								changes.Add(entityVP.Key, entityVP.Value.GetJson());
						}
					}
					else if(a_value)
					{
						countryDict[a_countryId] = (Months)(1 << a_month);
						changes.Add(entityVP.Key, entityVP.Value.GetJson());
					}
				}
				else if(a_value)
				{
					entityVP.Value.fleets.Add(a_gearId, new Dictionary<int, Months> { { a_countryId, (Months)(1 << a_month) } });
					changes.Add(entityVP.Key, entityVP.Value.GetJson());
				}
			}
			if (changes.Count > 0)
				m_changedCallback.Invoke(changes);
		}

		void OnRadiusChanged(string a_newValue)
		{
			if (m_ignoreCallback)
				return;

			Dictionary<Entity, string> changes = new Dictionary<Entity, string>();
			float result = float.NegativeInfinity;
			if(float.TryParse(a_newValue, out result))
			{
				if (result >= 0)
				{
					m_currentRadius = result;
					foreach (var entityVP in m_policyValues)
					{
						bool changed = Mathf.Abs(entityVP.Value.radius - result) > 0.01f;
						entityVP.Value.radius = m_currentRadius;
						if(changed)
							changes.Add(entityVP.Key, entityVP.Value.GetJson());
					}
				}
			}
			SetRadiusText();
			if (changes.Count > 0)
				m_changedCallback.Invoke(changes);
		}

		public override void SetContent(Dictionary<Entity, string> a_values, List<Entity> a_geometry, Action<Dictionary<Entity, string>> a_changedCallback)
		{
			Initialise();
			m_changedCallback = a_changedCallback;
			m_policyValues = new Dictionary<Entity, PolicyGeometryDataBufferZone>();
			m_currentRadius = 0f;
			bool first = true;
			foreach(var kvp in a_values)
			{
				PolicyGeometryDataBufferZone data = new PolicyGeometryDataBufferZone(kvp.Value);
				if(first)
				{
					m_currentRadius = data.radius;
					first = false;
				}
				else if(m_currentRadius >= 0 && Mathf.Abs(data.radius - m_currentRadius) > 0.01f)
				{
					m_currentRadius = Mathf.NegativeInfinity;
				}
				m_policyValues.Add(kvp.Key, data);
			}
			foreach(Entity e in a_geometry)
			{
				if (!a_values.ContainsKey(e))
				{
					//Create empty entries for geometry that doesnt have a value yet
					m_policyValues.Add(e, new PolicyGeometryDataBufferZone());
					if (m_currentRadius > 0.01f)
					{
						m_currentRadius = Mathf.NegativeInfinity;
					}
				}
			}
			foreach(FleetMixedToggleGroup fleetGroup in m_fleetGroups)
			{
				fleetGroup.SetValues(m_policyValues);
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
