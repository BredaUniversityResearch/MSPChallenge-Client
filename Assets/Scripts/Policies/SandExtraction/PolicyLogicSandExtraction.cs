using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
    public class PolicyLogicSandExtraction : APolicyLogic
    {
        static PolicyLogicSandExtraction m_instance;
        public static PolicyLogicSandExtraction Instance => m_instance;

        //Editing backups
        bool m_wasSandExtractionPlanBeforeEditing;
        PolicyPlanDataSandExtraction m_backup;

        public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
        {
            base.Initialise(a_settings, a_definition);
            m_instance = this;
        }

        public override void AddToPlan(Plan a_plan)
        {
            a_plan.AddPolicyData(new PolicyPlanDataSandExtraction(this));
        }

        public override void HandlePlanUpdate(APolicyData a_updateData, Plan a_plan, EPolicyUpdateStage a_stage)
        {
            if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
            {
                PolicyUpdateSandExtractionPlan update = (PolicyUpdateSandExtractionPlan)a_updateData;

                PolicyPlanDataSandExtraction planData = new PolicyPlanDataSandExtraction(this);
                if(update != null)
                {
                    planData.m_value = update.m_distanceValue;
                }
                
                a_plan.SetPolicyData(planData);
            }
        }

        public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage)
        { }

        public override void RemoveFromPlan(Plan a_plan)
        {
            a_plan.Policies.Remove(PolicyManager.SANDEXTRACTION_POLICY_NAME);
        }

        public override void StartEditingPlan(Plan a_plan)
        {
            if (a_plan == null)
            {
                m_wasSandExtractionPlanBeforeEditing = false;
                m_backup = null;
            }
            else if (a_plan.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var data))
            {
                m_wasSandExtractionPlanBeforeEditing = true;

                m_backup = new PolicyPlanDataSandExtraction(this);
                m_backup.m_value = data.m_value;
            }
            else
            {
                m_wasSandExtractionPlanBeforeEditing = false;
            }
        }

        public override void StopEditingPlan(Plan a_plan)
        {
            m_backup = null;
        }

        public override void RestoreBackupForPlan(Plan a_plan)
        {
            if (m_wasSandExtractionPlanBeforeEditing)
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
            if (a_plan.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var data))
            {
                if (!m_wasSandExtractionPlanBeforeEditing)
                    SetGeneralPolicyData(a_plan, new PolicyUpdateSandExtractionPlan() { m_distanceValue = data.m_value, policy_type = PolicyManager.SANDEXTRACTION_POLICY_NAME }, a_batch);
                
            }
            else if (m_wasSandExtractionPlanBeforeEditing)
            {
                DeleteGeneralPolicyData(a_plan, PolicyManager.SANDEXTRACTION_POLICY_NAME, a_batch);
            }
        }

        public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
        {
            //TODO CHECK: is it possible to change restriction size for other teams, if so: should it require approval?
        }

        public override void OnPlanLayerRemoved(PlanLayer a_layer)
        {
        }

        public int GetSandExtractionSettingBeforePlan(Plan a_plan)
        {
            List<Plan> plans = PlanManager.Instance.Plans;
            int result = 0;

            //Find the index of the given plan
            int planIndex = 0;
            for (; planIndex < plans.Count; planIndex++)
                if (plans[planIndex] == a_plan)
                    break;

            for (int i = planIndex - 1; i >= 0; i--)
            {
                if (plans[i].InInfluencingState && plans[i].TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var planData))
                {
                    return planData.m_value;
                }
            }
            return result;
        }
    }
}