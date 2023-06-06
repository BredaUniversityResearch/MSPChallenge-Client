using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class PolicyLogicFishing : APolicyLogic
	{
		FishingDistributionDelta m_fishingBackup;
		bool m_wasFishingPlanBeforeEditing;


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
					a_plan.AddPolicyData(new PolicyPlanDataFishing(this)
					{
						fishingDistributionDelta = updateData.fishing != null ? new FishingDistributionDelta(updateData.fishing) : new FishingDistributionDelta() //If null, it cant pick the right constructor automatically
					});
				}
			}
		}

		public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage) 
		{ }

		public override void StopEditingPlan(Plan a_plan) 
		{ }

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.Policies.Remove(PolicyManager.FISHING_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan) 
		{
			if(a_plan == null)
			{
				m_wasFishingPlanBeforeEditing = false;
			}
			else if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var data))
			{
				m_wasFishingPlanBeforeEditing = true;
				m_fishingBackup = data.fishingDistributionDelta;
				data.fishingDistributionDelta = data.fishingDistributionDelta.Clone();
			}
			else
			{
				m_wasFishingPlanBeforeEditing = false;
			}
		}

		public override void RestoreBackupForPlan(Plan a_plan) 
		{
			if (m_wasFishingPlanBeforeEditing)
			{
				if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var data))
				{
					data.fishingDistributionDelta = m_fishingBackup;
				}
				else
				{
					a_plan.AddPolicyData(new PolicyPlanDataFishing(this) { fishingDistributionDelta = m_fishingBackup });
				}
			}
			else
			{
				RemoveFromPlan(a_plan);
			}
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch) 
		{
			if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var data))
			{
				if (!m_wasFishingPlanBeforeEditing)
					SubmitPolicyActivity(a_plan, PolicyManager.FISHING_POLICY_NAME, true, a_batch);
				data.fishingDistributionDelta.SubmitToServer(a_plan.GetDataBaseOrBatchIDReference(), a_batch);
			}
			else if(m_wasFishingPlanBeforeEditing)
			{
				SubmitPolicyActivity(a_plan, PolicyManager.FISHING_POLICY_NAME, false, a_batch);
				JObject dataObject = new JObject();
				dataObject.Add("plan", a_plan.GetDataBaseOrBatchIDReference());
				a_batch.AddRequest(Server.DeleteFishingFromPlan(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel)
		{
			if (a_requiredApprovalLevel < EApprovalType.AllCountries)
			{
				PolicyPlanDataFishing planData = (PolicyPlanDataFishing)a_planData;

				foreach (KeyValuePair<string, Dictionary<int, float>> fishingFleets in planData.fishingDistributionDelta.GetValuesByFleet())
				{
					foreach (KeyValuePair<int, float> fishingValues in fishingFleets.Value)
					{
						if (a_approvalReasons.TryGetValue(fishingValues.Key, out var reasons))
							reasons.Add(new ApprovalReasonFishingPolicy(fishingFleets.Key));
						else
							a_approvalReasons.Add(fishingValues.Key, new List<IApprovalReason> { new ApprovalReasonFishingPolicy(fishingFleets.Key) });

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
			a_plan.AddPolicyData(new PolicyPlanDataFishing(this) { fishingDistributionDelta = new FishingDistributionDelta() });
		}
	}
}