using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class GenericBoundsField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_nameField;
        [SerializeField] RectTransform m_contentContainer;
        [SerializeField] GenericTextField m_blX;
        [SerializeField] GenericTextField m_blY;
        [SerializeField] GenericTextField m_tlX;
        [SerializeField] GenericTextField m_tlY;
        [SerializeField] Button m_selectBoundsButton;
        [SerializeField] float m_spacePerStep;

        bool m_ignoreCallback;
        Action<Vector4> m_changeCallback;
        float m_maxBoundsSize;
        Vector4 m_currentValue;

		public Vector4 CurrentValue => m_currentValue;

		public void Initialise(string a_name, int a_nameSizeSteps, float a_maxBoundsSize, Action<Vector4> a_changeCallback)
        {
            m_nameField.text = a_name;
            RectTransform nameRect = m_nameField.GetComponent<RectTransform>();
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a_nameSizeSteps * m_spacePerStep);
            m_contentContainer.anchorMin = new Vector2((a_nameSizeSteps + 2) * m_spacePerStep, 0f);
            m_maxBoundsSize = a_maxBoundsSize;
			m_changeCallback = a_changeCallback;
            m_blX.Initialise("Bottom left X", a_nameSizeSteps, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
            m_blY.Initialise("Bottom left Y", a_nameSizeSteps, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
            m_tlX.Initialise("Top left X", a_nameSizeSteps, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
            m_tlY.Initialise("Top left Y", a_nameSizeSteps, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
			m_selectBoundsButton.onClick.AddListener(OnBoundsButtonClicked);
		}

        public void SetContent(Vector4 a_value)
        {
            m_currentValue = a_value;
            UpdateTextFieldToBounds();
		}

		public void SetInteractable(bool a_interactable)
		{
			m_blX.SetInteractable(a_interactable);
			m_blY.SetInteractable(a_interactable);
			m_tlX.SetInteractable(a_interactable);
			m_tlY.SetInteractable(a_interactable);
            m_selectBoundsButton.interactable = a_interactable;
		}

        void UpdateTextFieldToBounds()
        {
            m_ignoreCallback = true;
            //TODO

            m_ignoreCallback = false;
		}

        void OnTextFieldChanged(string a_newValue)
        {
            if (m_ignoreCallback)
                return;
            m_currentValue = new Vector4(GetCoordinate(m_blX), GetCoordinate(m_blY), GetCoordinate(m_tlX), GetCoordinate(m_tlY));
			m_changeCallback.Invoke(m_currentValue);
		}

        float GetCoordinate(GenericTextField a_textField)
        {
            //TODO: Get coordinate from text
            return 0f;
        }

        void OnBoundsButtonClicked()
        {
            //TODO
        }
    }
}
