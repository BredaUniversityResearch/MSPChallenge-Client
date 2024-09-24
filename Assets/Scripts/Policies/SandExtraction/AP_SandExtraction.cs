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
        [SerializeField] SandExtractionSlider m_sandExtractionSliderPrefab;
        //[SerializeField] TextMeshProUGUI m_sandExtractionText;

        int m_sandExtractionValue;
        int m_settingsBeforePlan;

        public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
        {
            //Here we open the window and set the content.
            //If there is already a value set in the plan, we set the slider and input box to that value.
            //If there is no a set value, it is taken the value used most recently in a sand extraction plan.
            //Logic for this in MSP-5079
            base.OpenToContent(a_content, a_toggle, a_APWindow);

            m_settingsBeforePlan = PolicyLogicSandExtraction.Instance.GetSandExtractionSettingBeforePlan(a_content);
            if (a_content.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var planData))
            {
                m_sandExtractionValue = planData.m_value;
            }
            else
                m_sandExtractionValue = m_settingsBeforePlan;

            m_sandExtractionSliderPrefab.SetContent(m_sandExtractionValue.ToString(), m_APWindow.Editing);
            //Similarly to EcoGear line 67/62, make sure that the slider is set to the correct value.
            //The prfb itself (can be actually managed directly here) will have logic to not be interactable if the plan is not editable.
        }

        //Here we apply the value set in the UI to the actual plan.
        public override void ApplyContent()
        {
            //Take the value from slider/input
            //Then plan.setpolicydata and apply the value to the policy/plan.
            //Check the AP_EcoGear from Kevin.

            string m_sandExtractionString = m_sandExtractionSliderPrefab.GetComponent<TextMeshProUGUI>().text;
            m_sandExtractionValue = int.Parse(m_sandExtractionString);


            m_plan.SetPolicyData(new PolicyPlanDataSandExtraction(PolicyLogicSandExtraction.Instance) { policy_type = PolicyManager.SANDEXTRACTION_POLICY_NAME, m_value = m_sandExtractionValue });
        }
    }
}
