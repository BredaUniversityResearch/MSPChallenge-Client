using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public abstract class APolicyLogic : MonoBehaviour
	{
		public abstract void Initialise(APolicyData a_settings);
		public abstract void HandlePlanUpdate(APolicyData a_updateData, Plan a_plan);
		public abstract void HandlePreKPIUpdate(APolicyData a_updateData);
		public abstract void HandlePostKPIUpdate(APolicyData a_updateData);
		public virtual void UpdateAfterEditing(Plan a_plan) { }
		public abstract void GetRequiredApproval(APolicyData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel);
		public abstract APolicyData FormatPlanData(Plan a_plan);
		public abstract void RemoveFromPlan(Plan a_plan);
		public virtual bool HasError(APolicyData a_planData) => false;
	}
}