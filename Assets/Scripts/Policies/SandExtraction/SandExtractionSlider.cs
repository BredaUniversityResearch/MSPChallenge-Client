using MSP2050.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SandExtractionSlider : MonoBehaviour
{
    public CustomInputField m_inputField; // Reference to your custom input field script
    public CustomSlider m_slider; // Reference to the slider

    void Start()
    {
        m_inputField.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;

        // Initialize the slider and input field with the same value
        m_slider.maxValue = 30;
        m_slider.wholeNumbers = true;
        m_inputField.SetValueWithoutNotify(m_slider.value.ToString());

        // Add listeners to handle value changes
        m_slider.onValueChanged.AddListener(OnSliderValueChanged);
        m_inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
    }

    void OnSliderValueChanged(float value)
    {
        m_inputField.SetValueWithoutNotify(value.ToString());
    }

    void OnInputFieldValueChanged(string value)
    {
        // If empty, set to 0
        if (string.IsNullOrEmpty(value))
        {
            m_inputField.SetValueWithoutNotify("0");
            m_slider.SetValueWithoutNotify(0);
            return;
        }

        // Only allow digits
        if (int.TryParse(value, out int intValue))
        {
            intValue = (int)Mathf.Clamp(intValue, 0, m_slider.maxValue);
            m_slider.SetValueWithoutNotify(intValue);
            m_inputField.SetValueWithoutNotify(intValue.ToString());
        }
        else
        {
            // Revert to last valid value if invalid input
            m_inputField.SetValueWithoutNotify(m_slider.value.ToString());
        }
    }

    public void SetContent(string value, bool isEditable)
    {
        // Set the slider and input field values
        if (int.TryParse(value, out int intValue))
        {
            m_slider.value = intValue;
            m_inputField.text = intValue.ToString();
        }
        else
        {
            UnityEngine.Debug.LogError("Invalid value passed to SandExtractionSlider: " + value);
        }

        // Control interactivity based on the editing flag
        m_slider.interactable = isEditable;
        m_inputField.interactable = isEditable;
    }
}
