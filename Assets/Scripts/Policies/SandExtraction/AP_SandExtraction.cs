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
            //Logic for this in MSP-5079
            base.OpenToContent(a_content, a_toggle, a_APWindow);

            // Get the sand extraction setting before the plan
            m_settingsBeforePlan = PolicyLogicSandExtraction.Instance.GetSandExtractionSettingBeforePlan(a_content);

            // Use the setting before the plan if no data is available
            if (a_content.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var planData))
            {
                m_sandExtractionValue = planData.m_value;
            }
            else
                m_sandExtractionValue = m_settingsBeforePlan;

            // Set the slider and input field content
            m_sandExtractionSliderPrefab.SetContent(m_sandExtractionValue.ToString(), m_APWindow.Editing);

            //Similarly to EcoGear line 67/62, make sure that the slider is set to the correct value.
            //The prfb itself (can be actually managed directly here) will have logic to not be interactable if the plan is not editable.
        }

        //Here we apply the value set in the UI to the actual plan.
        public override void ApplyContent()
        {
            // Retrieve the final value from the slider
            if (int.TryParse(m_sandExtractionSliderPrefab.m_inputField.text, out int parsedValue))
            {
                // Apply the value to the plan
                m_plan.SetPolicyData(new PolicyPlanDataSandExtraction(PolicyLogicSandExtraction.Instance)
                {
                    policy_type = PolicyManager.SANDEXTRACTION_POLICY_NAME,
                    m_value = parsedValue
                });
            }
            else
            {
                Debug.LogError("Invalid sand extraction value: " + m_sandExtractionSliderPrefab.m_inputField.text);
            }
        }
    }
}
