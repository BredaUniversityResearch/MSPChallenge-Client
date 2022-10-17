using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public abstract class APolicyLogic : MonoBehaviour
	{
		public enum EPolicyUpdateStage { General, PreKPI, PostKPI }
		public PolicyDefinition m_definition;

		public virtual void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
		{
			m_definition = a_definition;
		}

		public abstract void Destroy();
		public abstract void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel);
		public abstract APolicyData FormatPlanData(Plan a_plan);
		public abstract bool FormatGeneralData(out APolicyData a_data);
		public abstract void AddToPlan(Plan a_plan);
		public abstract void RemoveFromPlan(Plan a_plan);
		public virtual void StartEditingPlan(Plan a_plan) { }
		public virtual void CalculateEffectsOfEditing(Plan a_plan) { }
		public virtual void UpdateAfterEditing(Plan a_plan) { }
		public virtual bool HasError(APolicyPlanData a_planData) => false;
		public virtual bool ShowPolicyToggled(APolicyPlanData a_planData) => true;
		public virtual void SetPolicyToggled(Plan a_plan, bool a_value)
		{
			if (a_value)
				AddToPlan(a_plan);
			else
				RemoveFromPlan(a_plan);
		}

		//Update order: 0
		public abstract void HandleGeneralUpdate(APolicyData a_updateData, EPolicyUpdateStage a_stage);
		//Update order: 1
		public abstract void HandlePlanUpdate(APolicyData a_updateData, Plan a_plan, EPolicyUpdateStage a_stage);
	}
}