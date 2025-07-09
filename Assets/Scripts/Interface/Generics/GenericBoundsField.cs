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
            m_blX.Initialise("Bottom left X", 0, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
            m_blY.Initialise("Bottom left Y", 0, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
            m_tlX.Initialise("Top left X", 0, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
            m_tlY.Initialise("Top left Y", 0, OnTextFieldChanged, "Coordinate", TMP_InputField.ContentType.DecimalNumber);
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
            m_blX.SetContent(m_currentValue.x.ToString(), true);
            m_blY.SetContent(m_currentValue.y.ToString(), true);
            m_tlX.SetContent(m_currentValue.z.ToString(), true);
            m_tlY.SetContent(m_currentValue.w.ToString(), true);
			m_ignoreCallback = false;
		}

        void OnTextFieldChanged(string a_newValue)
        {
            if (m_ignoreCallback)
                return;

            Rect bounds = CameraManager.Instance.zoomRect;
            m_currentValue = new Vector4(
                GetCoordinate(m_blX, bounds.xMin), 
                GetCoordinate(m_blY, bounds.yMin), 
                GetCoordinate(m_tlX, bounds.xMax), 
                GetCoordinate(m_tlY, bounds.yMax));
			m_changeCallback.Invoke(m_currentValue);
		}

        float GetCoordinate(GenericTextField a_textField, float a_default)
        {
            float result = a_default;
            float.TryParse(a_textField.CurrentValue, out result);
            return result;
        }

        void OnBoundsButtonClicked()
        {
            //TODO
        }
    }
}
