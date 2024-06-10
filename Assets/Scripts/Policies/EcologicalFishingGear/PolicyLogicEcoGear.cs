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
			throw new NotImplementedException();
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
		{
			throw new NotImplementedException();
		}

		public override void HandleGeneralUpdate(APolicyData a_updateData, EPolicyUpdateStage a_stage)
		{
			throw new NotImplementedException();
		}

		public override void HandlePlanUpdate(APolicyData a_updateData, Plan a_plan, EPolicyUpdateStage a_stage)
		{
			throw new NotImplementedException();
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			throw new NotImplementedException();
		}
	}
}
