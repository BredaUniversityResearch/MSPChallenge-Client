using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Joins;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyManager : MonoBehaviour
	{
		public const string ENERGY_POLICY_NAME = "energy";
		public const string FISHING_POLICY_NAME = "fishing";
		public const string SHIPPING_POLICY_NAME = "shipping";

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
			m_policyDefinitions.Add(ENERGY_POLICY_NAME, new PolicyDefinition { m_name = ENERGY_POLICY_NAME, 
				m_displayName = "Energy Distribution", 
				m_planUpdateType = typeof(PolicyUpdateEnergyPlan), 
				m_updateType = typeof(PolicyUpdateEnergy), 
				m_logicType = typeof(PolicySettingsEnergy) 
			});
			m_policyDefinitions.Add(FISHING_POLICY_NAME, new PolicyDefinition { m_name = FISHING_POLICY_NAME,
				m_displayName = "Fishing Effort",
				m_planUpdateType = typeof(PolicyUpdateFishingPlan),
				m_logicType = typeof(PolicyLogicFishing)
			});
			m_policyDefinitions.Add(SHIPPING_POLICY_NAME, new PolicyDefinition { m_name = SHIPPING_POLICY_NAME,
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

		public void RunPlanUpdate(APolicyData[] a_data, Plan a_plan, APolicyLogic.EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				HashSet<string> existingPolicies = new HashSet<string>();
				foreach (var kvp in a_plan.m_policies)
				{
					existingPolicies.Add(kvp.Key);
				}

				//Handle updates
				foreach (APolicyData data in a_data)
				{
					if (m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
					{
						policy.HandlePlanUpdate(data, a_plan, a_stage);
					}
					existingPolicies.Remove(data.policy_type);
				}

				//Handle removal
				foreach (string removed in existingPolicies)
				{
					if (m_policyLogic.TryGetValue(removed, out APolicyLogic policy))
					{
						policy.RemoveFromPlan(a_plan);
					}
				}
			}
			else
			{
				foreach (APolicyData data in a_data)
				{
					if (m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
					{
						policy.HandlePlanUpdate(data, a_plan, a_stage);
					}
				}
			}
		}

		public void RunGeneralUpdate(APolicyData[] a_data, APolicyLogic.EPolicyUpdateStage a_stage)
		{
			foreach (APolicyData data in a_data)
			{
				if (m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
				{
					policy.HandleGeneralUpdate(data, a_stage);
				}
			}
		}
	}
}