using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyLogicShipping : APolicyLogic
	{
		public override void Initialise(APolicyData a_settings)
		{ }

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan, EPolicyUpdateStage a_stage) 
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				PolicyUpdateShippingPlan data = (PolicyUpdateShippingPlan)a_data;
				RestrictionAreaManager.instance.ProcessReceivedRestrictions(a_plan, data.restriction_settings);
				if (!a_plan.m_policies.ContainsKey(PolicyManager.SHIPPING_POLICY_NAME))
					a_plan.m_policies.Add(PolicyManager.SHIPPING_POLICY_NAME, new PolicyPlanDataShipping());
			}
		}

		public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage) 
		{ }

		public override APolicyData FormatPlanData(Plan a_plan) 
		{
			//TODO
			return null;
		}

		public override void UpdateAfterEditing(Plan a_plan) 
		{ }

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.m_policies.Remove(PolicyManager.SHIPPING_POLICY_NAME);
		}

		public override void GetRequiredApproval(APolicyData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel)
		{
		}
	}
}