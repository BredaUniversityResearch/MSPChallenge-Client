using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
    public class AP_SandExtraction : AP_PopoutWindow
    {
        //Reference to slider and input box for sand extraction
        [SerializeField]
        GameObject m_sandExtractionPolicyPrefab;

        int m_sandExtractionValue;
        int m_settingsBeforePlan;

        public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
        {
            //Here we open the window and set the content.
            //If there is already a value set in the plan, we set the slider and input box to that value.
            //If there is no a set value, it is taken the value used most recently in a sand extraction plan.
            //Logic for this in MSP-5079
            base.OpenToContent(a_content, a_toggle, a_APWindow);

            RefreshContent(a_content, false);
        }

        public void RefreshContent(Plan a_content, bool a_applyCurrent = true)
        {
            if (m_APWindow.Editing && a_applyCurrent)
            {
                //Apply content already changed before updating
                ApplyContent();
            }

            m_settingsBeforePlan = PolicyLogicSandExtraction.Instance.GetSandExtractionSettingBeforePlan(a_content);
            PolicyPlanDataSandExtraction planData;
            a_content.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out planData);

            if (planData.m_value != 0)
            {
                m_sandExtractionValue = planData.m_value;
            }
            else
            {
                m_sandExtractionValue = m_settingsBeforePlan;
            }

        }

        //Here we apply the value set in the UI to the actual plan.
        public override void ApplyContent()
        {
            if (m_APWindow.Editing)
            {
                //Take the value from slider/input
                //Then plan.setpolicydata and apply the value to the policy/plan.
                //Check the AP_EcoGear from Kevin.

                m_sandExtractionValue = m_sandExtractionPolicyPrefab.GetComponent</*InputTextBox*/>().Value;

                m_plan.SetPolicyData(new PolicyPlanDataSandExtraction(PolicyLogicSandExtraction.Instance) { policy_type = PolicyManager.SANDEXTRACTION_POLICY_NAME, m_value = m_sandExtractionValue });
            }
        }
    }
}
