using MSP2050.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandExtractionSlider : MonoBehaviour
{
    public CustomInputField m_inputField; // Reference to your custom input field script
    public CustomSlider m_slider; // Reference to the slider

    void Start()
    {
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
        if (int.TryParse(value, out int intValue))
        {
            // Clamp the value to the maximum allowed (30)
            intValue = (int)Mathf.Clamp(intValue, 0, m_slider.maxValue);

            // Update the slider with the clamped value
            m_slider.SetValueWithoutNotify(intValue);

            // Update the input field with the clamped value (in case it was out of bounds)
            m_inputField.SetValueWithoutNotify(intValue.ToString());
        }
    }

    public void SetContent(string a_value, bool a_interactable)
    {
        m_inputField.text = a_value;

        m_inputField.interactable = a_interactable;
        m_slider.interactable = a_interactable;
    }
}
