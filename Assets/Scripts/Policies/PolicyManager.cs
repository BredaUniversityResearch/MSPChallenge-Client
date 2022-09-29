using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyManager : MonoBehaviour
	{
		private static PolicyManager singleton;
		public static PolicyManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<PolicyManager>();
				return singleton;
			}
		}

		private Dictionary<string, PolicyDefinition> m_policyDefinitions = new Dictionary<string, PolicyDefinition>();
		private Dictionary<string, APolicyLogic> m_policyLogic = new Dictionary<string, APolicyLogic>();

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
		}

		void OnDestroy()
		{
			singleton = null;
		}

		//All possible policies should be registered before policies are initilised
		public void RegisterPolicy(PolicyDefinition a_policy)
		{
			m_policyDefinitions.Add(a_policy.m_name, a_policy);
		}

		public void InitialisePolicies(APolicyData[] a_policySettings)
		{
			//Create logic instances
			foreach(APolicyData data in a_policySettings)
			{
				if(m_policyDefinitions.TryGetValue(data.policy_type, out PolicyDefinition definition))
				{
					APolicyLogic logic = (APolicyLogic)gameObject.AddComponent(definition.m_logicType);
					logic.Initialise(data);
					m_policyLogic.Add(data.policy_type, logic);
				}
				else
				{
					Debug.LogError("Policy settings received from the server for a policy without definition: " + data.policy_type);
				}
			}
		}

		public bool TryGetDefinition(string a_name, out PolicyDefinition a_definition)
		{
			return m_policyDefinitions.TryGetValue(a_name, out a_definition);
		}

		public bool TryGetLogic(string a_name, out APolicyLogic a_logic)
		{
			return m_policyLogic.TryGetValue(a_name, out a_logic);
		}
	}
}