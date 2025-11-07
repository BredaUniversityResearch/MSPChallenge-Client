using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyWindowContentMUPlatform : AGeometryPolicyWindowContent
	{
		[SerializeField] List<ToggleMixedValue> m_toggles;

		bool m_initialised;
		Dictionary<Entity, PolicyGeometryDataMUPlatform> m_policyValues;
		Action<Dictionary<Entity, string>> m_changedCallback;

		void Initialise()
		{
			if (m_initialised)
				return;

			m_initialised = true;

			for (int i = 0; i < m_toggles.Count; i++)
			{
				int index = i;
				m_toggles[i].m_onValueChangeCallback = (b) => { OnToggleChanged(index, b); };
			}
		}

		public override void SetContent(Dictionary<Entity, string> a_values, List<Entity> a_geometry, Action<Dictionary<Entity, string>> a_changedCallback)
		{
			Initialise();
			m_changedCallback = a_changedCallback;
			m_policyValues = new Dictionary<Entity, PolicyGeometryDataMUPlatform>();
			bool first = true;
			bool?[] values = new bool?[m_toggles.Count];
			for (int i = 0; i < m_toggles.Count; i++)
				values[i] = false;

			foreach (var kvp in a_values)
			{
				PolicyGeometryDataMUPlatform data = new PolicyGeometryDataMUPlatform(kvp.Value); 
				m_policyValues.Add(kvp.Key, data);
				if(first)
				{
					for (int i = 0; i < m_toggles.Count; i++)
						values[i] = data.options[i];
					first = false;
				}
				else
				{
					for (int i = 0; i < m_toggles.Count; i++)
					{
						if (values[i].HasValue && values[i].Value != data.options[i])
							values[i] = null;
					}
				}
			}
			first = false;
			foreach (Entity e in a_geometry)
			{
				if (!a_values.ContainsKey(e))
				{
					//Create empty entries for geometry that doesnt have a value yet
					m_policyValues.Add(e, new PolicyGeometryDataMUPlatform());
					for (int i = 0; i < m_toggles.Count; i++)
					{
						if (values[i].HasValue && values[i].Value)
							values[i] = null;
					}
				}
			}
			for (int i = 0; i < m_toggles.Count; i++)
			{
				m_toggles[i].Value = values[i];
			}

			//TODO: set energy capacity of geometry based on policy
		}

		void OnToggleChanged(int a_toggleIndex, bool a_value)
		{
			Dictionary<Entity, string> changes = new Dictionary<Entity, string>();

			foreach (var kvp in m_policyValues)
			{
				if (kvp.Value.options[a_toggleIndex] != a_value)
				{
					kvp.Value.options[a_toggleIndex] = a_value;
					changes.Add(kvp.Key, kvp.Value.GetJson());
				}
			}
			if (changes.Count > 0)
				m_changedCallback?.Invoke(changes);
		}

		public override void SetContent(string a_value, Entity a_geometry)
		{
			Initialise();
			m_changedCallback = null;
			PolicyGeometryDataMUPlatform data = new PolicyGeometryDataMUPlatform(a_value);
			m_policyValues = new Dictionary<Entity, PolicyGeometryDataMUPlatform>() { { a_geometry, data } };

			for (int i = 0; i < m_toggles.Count; i++)
			{
				m_toggles[i].Value = data.options[i];
			}
		}

		public override void SetInteractable(bool a_interactable)
		{
			for (int i = 0; i < m_toggles.Count; i++)
			{
				m_toggles[i].Interactable = a_interactable;
			}
		}
	}
}
