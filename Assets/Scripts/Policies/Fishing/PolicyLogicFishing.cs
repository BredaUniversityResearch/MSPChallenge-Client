using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyLogicFishing : APolicyLogic
	{
		public override void Initialise(APolicyData a_settings)
		{ }

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan)
		{
			PolicyUpdateFishingPlan updateData = (PolicyUpdateFishingPlan)a_data;
			if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(updateData.policy_type, out PolicyPlanDataFishing planData))
			{
				planData.fishingDistributionDelta = new FishingDistributionDelta(updateData.fishing);
			}
			else
			{
				a_plan.AddPolicyData(new PolicyPlanDataFishing() { 
					policy_type = updateData.policy_type, 
					fishingDistributionDelta = updateData.fishing != null ? new FishingDistributionDelta(updateData.fishing) : new FishingDistributionDelta() //If null, it cant pick the right constructor automatically
				});
			}
		}

		public override void HandlePreKPIUpdate(APolicyData a_data) 
		{ }

		public override APolicyData FormatPlanData(Plan a_plan) 
		{
			return null;
		}

		public override void UpdateAfterEditing(Plan a_plan) 
		{ }

		public override void HandlePostKPIUpdate(APolicyData a_data)
		{
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.m_policies.Remove(PolicyManager.FISHING_POLICY_NAME);
		}

		public override void GetRequiredApproval(APolicyData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel)
		{
			if (a_requiredApprovalLevel < EApprovalType.AllCountries)
			{
				PolicyPlanDataFishing planData = (PolicyPlanDataFishing)a_planData;

				foreach (KeyValuePair<string, Dictionary<int, float>> fishingFleets in planData.fishingDistributionDelta.GetValuesByFleet())
				{
					foreach (KeyValuePair<int, float> fishingValues in fishingFleets.Value)
					{
						if (!a_approvalStates.ContainsKey(fishingValues.Key))
						{
							a_approvalStates.Add(fishingValues.Key, EPlanApprovalState.Maybe);
						}
					}
				}
			}
		}
	}
}