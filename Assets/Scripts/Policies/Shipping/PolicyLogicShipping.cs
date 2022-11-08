using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyLogicShipping : APolicyLogic
	{
		public const float shippingDisplayScale = 10000; // = 10km

		public override void Destroy()
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

		public override void StopEditingPlan(Plan a_plan) 
		{ }

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.m_policies.Remove(PolicyManager.SHIPPING_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan)
		{
			//TODO: store backup
		}

		public override void RestoreBackupForPlan(Plan a_plan)
		{
			//TODO
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch)
		{
			RestrictionAreaManager.instance.SubmitSettingsForPlan(a_plan, a_batch);
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel)
		{
		}

		public override void AddToPlan(Plan a_plan)
		{
			throw new NotImplementedException();
			//TODO
		}
	}
}