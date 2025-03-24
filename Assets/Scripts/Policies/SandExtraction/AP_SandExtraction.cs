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

        int m_sandExtractionValue;
        int m_settingsBeforePlan;

        public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
        {
            base.OpenToContent(a_content, a_toggle, a_APWindow);

            // Retrieve the plan data
            if (a_content.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var planData))
            {
                m_sandExtractionValue = planData.m_value;
            }
            else
            {
                m_sandExtractionValue = m_settingsBeforePlan;
            }

            // Set the slider and input field content
            m_sandExtractionSliderPrefab.SetContent(m_sandExtractionValue.ToString(), m_APWindow.Editing);
        }

        //Here we apply the value set in the UI to the actual plan.
        public override void ApplyContent()
        {
            if (!string.IsNullOrEmpty(m_sandExtractionSliderPrefab.m_inputField.text) && int.TryParse(m_sandExtractionSliderPrefab.m_inputField.text, out int parsedValue))
            {
                var policyData = new PolicyPlanDataSandExtraction(PolicyLogicSandExtraction.Instance)
                {
                    policy_type = PolicyManager.SANDEXTRACTION_POLICY_NAME,
                    m_value = parsedValue
                };
                m_plan.SetPolicyData(policyData);
            }
            else
            {
                Debug.LogError("Invalid sand extraction value: " + m_sandExtractionSliderPrefab.m_inputField.text);
            }
        }
    }
}
