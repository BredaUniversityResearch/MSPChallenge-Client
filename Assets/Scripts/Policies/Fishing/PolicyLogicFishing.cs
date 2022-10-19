using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class PolicyLogicFishing : APolicyLogic
	{
		private FishingDistributionDelta fishingBackup;

		public override void Destroy()
		{ }

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan, EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				PolicyUpdateFishingPlan updateData = (PolicyUpdateFishingPlan)a_data;
				if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(updateData.policy_type, out PolicyPlanDataFishing planData))
				{
					planData.fishingDistributionDelta = new FishingDistributionDelta(updateData.fishing);
				}
				else
				{
					a_plan.AddPolicyData(new PolicyPlanDataFishing()
					{
						logic = this,
						fishingDistributionDelta = updateData.fishing != null ? new FishingDistributionDelta(updateData.fishing) : new FishingDistributionDelta() //If null, it cant pick the right constructor automatically
					});
				}
			}
		}

		public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage) 
		{ }

		public override APolicyData FormatPlanData(Plan a_plan) 
		{
			//TODO
			return null;
		}

		public override bool FormatGeneralData(out APolicyData a_data)
		{
			//TODO
			a_data = null;
			return false;
		}

		public override void StopEditingPlan(Plan a_plan) 
		{ }

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.m_policies.Remove(PolicyManager.FISHING_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan) 
		{
			fishingBackup = a_plan.fishingDistributionDelta;
			a_plan.fishingDistributionDelta = a_plan.fishingDistributionDelta.Clone();
		}

		public override void RestoreBackupForPlan(Plan a_plan) 
		{
			a_plan.fishingDistributionDelta = fishingBackup;
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch) 
		{
			a_plan.fishingDistributionDelta.SubmitToServer(a_plan.ID, a_batch);
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel)
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

		public override void AddToPlan(Plan a_plan)
		{
			throw new NotImplementedException();
			//TODO
		}
	}
}