using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyLogicShipping : APolicyLogic
	{
		public const float shippingDisplayScale = 10000; // = 10km
		bool m_wasShippingPlanBeforeEditing;
		List<RestrictionAreaObject> m_backup;

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan, EPolicyUpdateStage a_stage) 
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				PolicyUpdateShippingPlan data = (PolicyUpdateShippingPlan)a_data;
				RestrictionAreaManager.Instance.SetRestrictionsToObject(a_plan, data.restriction_settings);
				if (!a_plan.Policies.ContainsKey(PolicyManager.SHIPPING_POLICY_NAME))
					a_plan.Policies.Add(PolicyManager.SHIPPING_POLICY_NAME, new PolicyPlanDataShipping(this));
			}
		}

		public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage) 
		{ }

		public override void StopEditingPlan(Plan a_plan) 
		{
			m_backup = null;
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.Policies.Remove(PolicyManager.SHIPPING_POLICY_NAME);
			RestrictionAreaManager.Instance.ClearSettingsForPlan(a_plan);
		}

		public override void StartEditingPlan(Plan a_plan)
		{
			if (a_plan == null)
			{
				m_wasShippingPlanBeforeEditing = false;
			}
			else if (a_plan.TryGetPolicyData<PolicyPlanDataShipping>(PolicyManager.SHIPPING_POLICY_NAME, out var data))
			{
				m_backup = RestrictionAreaManager.Instance.GatherSettingsForPlan(a_plan);
				m_wasShippingPlanBeforeEditing = true;
			}
			else
			{
				m_wasShippingPlanBeforeEditing = false;
				m_backup = null;
			}
		}

		public override void RestoreBackupForPlan(Plan a_plan)
		{
			if(m_wasShippingPlanBeforeEditing || a_plan.ContainsPolicy(PolicyManager.SHIPPING_POLICY_NAME))
				RestrictionAreaManager.Instance.SetRestrictionsToObject(a_plan, m_backup);
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch)
		{
			if (a_plan.TryGetPolicyData<PolicyPlanDataShipping>(PolicyManager.SHIPPING_POLICY_NAME, out var data))
			{
				SetGeneralPolicyData(a_plan, new EmptyPolicyPlanData(PolicyManager.SHIPPING_POLICY_NAME), a_batch);
			}
			else if(m_wasShippingPlanBeforeEditing)
			{
				DeleteGeneralPolicyData(a_plan, PolicyManager.SHIPPING_POLICY_NAME, a_batch);
			}
			RestrictionAreaManager.Instance.SubmitSettingsForPlan(a_plan, a_batch);
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
		{ 
			//TODO CHECK: is it possible to change restriction size for other teams, if so: should it require approval?
		}

		public override void AddToPlan(Plan a_plan)
		{
			a_plan.AddPolicyData(new PolicyPlanDataShipping(this));
		}

		public override void OnPlanLayerRemoved(PlanLayer a_layer) 
		{
			RestrictionAreaManager.Instance.ClearSettingsForPlanLayer(a_layer);
		}
	}
}