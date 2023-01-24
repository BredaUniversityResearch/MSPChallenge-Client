﻿using System;
using System.Collections;
using System.Collections.Generic;
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
		private Dictionary<string, APolicyData> m_policySettings = new Dictionary<string, APolicyData>();

		public Dictionary<string, APolicyLogic> PolicyLogic => m_policyLogic;

		public delegate void PoliciesInitialisedCallback();
		public event PoliciesInitialisedCallback m_onPoliciesInitialised;

		private bool m_initialised;
		public bool Initialised => m_initialised;

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
			foreach(var kvp in m_policyLogic)
			{
				kvp.Value.Destroy();
			}
		}

		//All possible policies should be registered before policies are initilised
		public void RegisterPolicy(PolicyDefinition a_policy)
		{
			m_policyDefinitions.Add(a_policy.m_name, a_policy);
		}

		public void RegisterBuiltInPolicies()
		{
			m_policyDefinitions.Add(ENERGY_POLICY_NAME, new PolicyDefinition
			{
				m_name = ENERGY_POLICY_NAME,
				m_displayName = "Energy Distribution",
				m_planUpdateType = typeof(PolicyUpdateEnergyPlan),
				m_updateType = typeof(PolicyUpdateEnergy),
				m_logicType = typeof(PolicyLogicEnergy),
				m_settingsType = typeof(APolicyData),
				m_activePlanPrefab = Resources.Load<GameObject>("AP_PolicyEnergy_PRFB"),
				m_activePlanIcon = Resources.Load<Sprite>("Icons/energy")
			});
			m_policyDefinitions.Add(FISHING_POLICY_NAME, new PolicyDefinition
			{
				m_name = FISHING_POLICY_NAME,
				m_displayName = "Fishing Effort",
				m_planUpdateType = typeof(PolicyUpdateFishingPlan),
				m_logicType = typeof(PolicyLogicFishing),
				m_settingsType = typeof(APolicyData),
				m_activePlanPrefab = Resources.Load<GameObject>("AP_PolicyFishing_PRFB"),
				m_activePlanIcon = Resources.Load<Sprite>("Icons/fishing")
			});
			m_policyDefinitions.Add(SHIPPING_POLICY_NAME, new PolicyDefinition
			{
				m_name = SHIPPING_POLICY_NAME,
				m_displayName = "Shipping Restriction Zones",
				m_planUpdateType = typeof(PolicyUpdateShippingPlan),
				m_logicType = typeof(PolicyLogicShipping),
				m_settingsType = typeof(APolicyData),
				m_activePlanPrefab = Resources.Load<GameObject>("AP_PolicyShipping_PRFB"),
				m_activePlanIcon = Resources.Load<Sprite>("Icons/shipping")
			});
		}

		public void InitialisePolicies(List<APolicyData> a_policySettings)
		{
			//Create logic instances
			foreach (APolicyData data in a_policySettings)
			{
				if(m_policyDefinitions.TryGetValue(data.policy_type, out PolicyDefinition definition))
				{
					APolicyLogic logic = (APolicyLogic)gameObject.AddComponent(definition.m_logicType);
					logic.Initialise(data, definition);
					m_policyLogic.Add(data.policy_type, logic);
					m_policySettings.Add(data.policy_type, data);
				}
				else
				{
					Debug.LogError("Policy settings received from the server for a policy without definition: " + data.policy_type);
				}
			}
			if (m_onPoliciesInitialised != null)
			{
				m_onPoliciesInitialised.Invoke();
				m_onPoliciesInitialised = null;
			}
			m_initialised = true;
		}

		public bool TryGetDefinition(string a_name, out PolicyDefinition a_definition)
		{
			return m_policyDefinitions.TryGetValue(a_name, out a_definition);
		}

		public bool TryGetLogic(string a_name, out APolicyLogic a_logic)
		{
			return m_policyLogic.TryGetValue(a_name, out a_logic);
		}

		public bool TryGetSettings(string a_name, out APolicyData a_settings)
		{
			return m_policySettings.TryGetValue(a_name, out a_settings);
		}

		public void RunPlanUpdate(List<APolicyData> a_data, Plan a_plan, APolicyLogic.EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				HashSet<string> existingPolicies = new HashSet<string>();

				foreach (var kvp in a_plan.Policies)
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

		public void RunGeneralUpdate(List<APolicyData> a_data, APolicyLogic.EPolicyUpdateStage a_stage)
		{
			foreach (APolicyData data in a_data)
			{
				if (m_policyLogic.TryGetValue(data.policy_type, out APolicyLogic policy))
				{
					policy.HandleGeneralUpdate(data, a_stage);
				}
			}
		}

		public void StartEditingPlan(Plan a_plan)
		{
			foreach(var kvp in m_policyLogic)
			{
				kvp.Value.StartEditingPlan(a_plan);
			}
		}

		public void RestoreBackupForPlan(Plan a_plan)
		{
			foreach (var kvp in m_policyLogic)
			{
				kvp.Value.RestoreBackupForPlan(a_plan);
			}
		}

		public void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch)
		{
			foreach (var kvp in m_policyLogic)
			{
				kvp.Value.SubmitChangesToPlan(a_plan, a_batch);
			}
		}

		public void StopEditingPlan(Plan a_plan)
		{
			foreach (var kvp in m_policyLogic)
			{
				kvp.Value.StopEditingPlan(a_plan);
			}
		}

		public int CalculateEffectsOfEditing(Plan a_plan)
		{
			int result = 0;
			foreach (var kvp in m_policyLogic)
			{
				if (kvp.Value.CalculateEffectsOfEditing(a_plan))
					result++;
			}
			return result;
		}

		public void GetPolicyIssueText(Plan a_plan, List<string> a_issueText)
		{
			if (a_plan.Policies != null)
			{
				foreach (var kvp in a_plan.Policies)
				{
					kvp.Value.logic.GetIssueText(kvp.Value, a_issueText);
				}
			}
		}
	}
}