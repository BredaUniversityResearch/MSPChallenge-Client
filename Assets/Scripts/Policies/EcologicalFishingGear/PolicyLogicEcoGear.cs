using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class PolicyLogicEcoGear : APolicyLogic
	{
		public override void AddToPlan(Plan a_plan)
		{
			a_plan.AddPolicyData(new PolicyPlanDataEcoGear(this)
			{
				m_values = new Dictionary<int, Dictionary<int, bool>>()
			});
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
		{
			//Never requires approval because people can only change their own values
		}

		public override void HandleGeneralUpdate(APolicyData a_updateData, EPolicyUpdateStage a_stage)
		{ }

		public override void HandlePlanUpdate(APolicyData a_updateData, Plan a_plan, EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				PolicyUpdateEcoGearPlan update = (PolicyUpdateEcoGearPlan)a_updateData;
				//TODO: convert into 
				PolicyPlanDataEcoGear planData = new PolicyPlanDataEcoGear(this);
				throw new NotImplementedException();
			}
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.Policies.Remove(PolicyManager.ECO_GEAR_POLICY_NAME);
		}

		public Dictionary<int, Dictionary<int, bool>> GetEcoGearSettingBeforePlan(Plan a_plan)
		{
			List<Plan> plans = PlanManager.Instance.Plans;
			Dictionary<int, Dictionary<int, bool>> result = new Dictionary<int, Dictionary<int, bool>>(); //country, gear_type, eco_gear

			//Find the index of the given plan
			int planIndex = 0;
			for (; planIndex < plans.Count; planIndex++)
				if (plans[planIndex] == a_plan)
					break;

			for (int i = planIndex - 1; i >= 0; i--)
			{
				if (plans[i].InInfluencingState && plans[i].TryGetPolicyData<PolicyPlanDataEcoGear>(PolicyManager.ECO_GEAR_POLICY_NAME, out var planData))
				{
					planData.AddUnchangedValues(result);
				}
			}
			return result;
		}
	}
}
