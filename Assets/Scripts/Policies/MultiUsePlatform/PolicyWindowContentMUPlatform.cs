using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyWindowContentMUPlatform : AGeometryPolicyWindowContent
	{
		bool m_initialised;
		Dictionary<Entity, PolicyGeometryDataMUPlatform> m_policyValues;
		Action<Dictionary<Entity, string>> m_changedCallback;

		void Initialise()
		{
			if (m_initialised)
				return;

			m_initialised = true;
		}

		public override void SetContent(Dictionary<Entity, string> a_values, List<Entity> a_geometry, Action<Dictionary<Entity, string>> a_changedCallback)
		{
			Initialise();
			m_changedCallback = a_changedCallback;
			m_policyValues = new Dictionary<Entity, PolicyGeometryDataMUPlatform>();
			foreach (var kvp in a_values)
			{
				PolicyGeometryDataMUPlatform data = new PolicyGeometryDataMUPlatform(kvp.Value); 
				m_policyValues.Add(kvp.Key, data);
			}
			foreach (Entity e in a_geometry)
			{
				if (!a_values.ContainsKey(e))
				{
					//Create empty entries for geometry that doesnt have a value yet
					m_policyValues.Add(e, new PolicyGeometryDataMUPlatform());
				}
			}
			//TODO: update display
			//TODO: set energy capacity of geometry based on policy
		}

		public override void SetContent(string a_value, Entity a_geometry)
		{
			Initialise();
			m_changedCallback = null;
			m_policyValues = new Dictionary<Entity, PolicyGeometryDataMUPlatform>() { { a_geometry, new PolicyGeometryDataMUPlatform(a_value) } };
			
			//TODO: update display
		}

		public override void SetInteractable(bool a_interactable)
		{
			//TODO: set display interactable
		}
	}
}
