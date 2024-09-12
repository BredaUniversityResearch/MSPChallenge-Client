using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class PolicyLogicEcoGear : APolicyLogic
	{
		static PolicyLogicEcoGear m_instance;
		public static PolicyLogicEcoGear Instance => m_instance;

		//Editing backups
		bool m_wasEcoGearPlanBeforeEditing;
		PolicyPlanDataEcoGear m_backup;
		AP_EcoGear m_apEcoGear;

		public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
		{
			base.Initialise(a_settings, a_definition);
			m_instance = this;
		}

		public override void AddToPlan(Plan a_plan)
		{
			a_plan.AddPolicyData(new PolicyPlanDataEcoGear(this));
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
				//Convert received format into client format
				PolicyPlanDataEcoGear planData = new PolicyPlanDataEcoGear(this);
				if(update != null)
				{
					foreach(EcoGearSetting setting in update.items)
					{
						foreach(int fleetID in setting.fleets)
						{
							planData.m_values[fleetID] = setting.enabled;
						}
					}
				}
				a_plan.SetPolicyData(planData);
			}
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.Policies.Remove(PolicyManager.ECO_GEAR_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan)
		{
			if (a_plan == null)
			{
				m_wasEcoGearPlanBeforeEditing = false;
				m_backup = null;
			}
			else if (a_plan.TryGetPolicyData<PolicyPlanDataEcoGear>(PolicyManager.ECO_GEAR_POLICY_NAME, out var data))
			{
				m_wasEcoGearPlanBeforeEditing = true;
				m_backup = new PolicyPlanDataEcoGear(this);
				foreach (var kvp in data.m_values)
					m_backup.m_values[kvp.Key] = kvp.Value;
			}
			else
			{
				m_wasEcoGearPlanBeforeEditing = false;
			}
		}

		public override void RestoreBackupForPlan(Plan a_plan)
		{
			if (m_wasEcoGearPlanBeforeEditing)
			{
				a_plan.SetPolicyData(m_backup);
			}
			else
			{
				RemoveFromPlan(a_plan);
			}
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch)
		{
			if (a_plan.TryGetPolicyData<PolicyPlanDataEcoGear>(PolicyManager.ECO_GEAR_POLICY_NAME, out var data))
			{
				SetGeneralPolicyData(a_plan, new EmptyPolicyPlanData(PolicyManager.ENERGY_POLICY_NAME), a_batch);

			}
			else if (m_wasEcoGearPlanBeforeEditing)
			{
				DeleteGeneralPolicyData(a_plan, PolicyManager.ECO_GEAR_POLICY_NAME, a_batch);
			}
		}

		public override void StopEditingPlan(Plan a_plan)
		{
			m_backup = null;
		}

		public override void EditedPlanTimeChanged(Plan a_plan)
		{
			if (m_apEcoGear != null && m_apEcoGear.IsOpen)
			{
				m_apEcoGear.RefreshContent(a_plan);
			}
		}

		public override void PreviousPlanChangedInfluence(Plan a_plan)
		{
			if (m_apEcoGear != null && m_apEcoGear.IsOpen)
			{
				m_apEcoGear.RefreshContent(a_plan);
			}
		}

		public void RegisterAPEcoGear(AP_EcoGear a_apEcoGear)
		{
			m_apEcoGear = a_apEcoGear;
		}

		public Dictionary<int, bool> GetEcoGearSettingBeforePlan(Plan a_plan)
		{
			List<Plan> plans = PlanManager.Instance.Plans;
			Dictionary<int, bool> result = new Dictionary<int, bool>(); //fleet_id, eco_gear

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
