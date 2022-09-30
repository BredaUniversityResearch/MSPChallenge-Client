using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Joins;
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
			//Register built in policies
			m_policyDefinitions.Add("energy", new PolicyDefinition { m_name = "energy", 
				m_displayName = "Energy Distribution", 
				m_planUpdateType = typeof(PolicyUpdateEnergyPlan), 
				m_updateType = typeof(PolicyUpdateEnergy), 
				m_logicType = typeof(PolicySettingsEnergy) 
			});
			m_policyDefinitions.Add("fishing", new PolicyDefinition { m_name = "fishing",
				m_displayName = "Fishing Effort",
				m_planUpdateType = typeof(PolicyUpdateFishingPlan),
				m_logicType = typeof(PolicyLogicFishing)
			});
			m_policyDefinitions.Add("shipping", new PolicyDefinition { m_name = "shipping",
				m_displayName = "Shipping Restriction Zones",
				m_planUpdateType = typeof(PolicyUpdateShippingPlan),
				m_logicType = typeof(PolicyLogicShipping)
			});

			//Create logic instances
			foreach (APolicyData data in a_policySettings)
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

		public void RunPlanUpdate(APolicyData[] a_data, Plan a_plan)
		{ 
			foreach(APolicyData data in a_data)
			{
				if(m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
				{
					policy.HandlePlanUpdate(data, a_plan);
				}
			}
		}

		public void RunPreSimulationUpdate(APolicyData[] a_data)
		{
			foreach (APolicyData data in a_data)
			{
				if (m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
				{
					policy.HandlePreKPIUpdate(data);
				}
			}
		}

		public void RunPostSimulationUpdate(APolicyData[] a_data)
		{
			foreach (APolicyData data in a_data)
			{
				if (m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
				{
					policy.HandlePostKPIUpdate(data);
				}
			}
		}
	}
}