using System;
using System.Collections.Generic;
using System.Xml.Schema;
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
        [SerializeField] GameObject m_selectBoundsObject;
        [SerializeField] Toggle m_selectBoundsToggle;
        [SerializeField] Button m_zoomToBoundsButton;
        [SerializeField] float m_spacePerStep;
        [SerializeField] RectTransform m_previewObject;

		bool m_ignoreCallback;
        Action<Vector4> m_changeCallback;
        float m_maxBoundsSize;
        Vector4 m_currentValue;

		public Vector4 CurrentValue => m_currentValue;

		private void OnEnable()
		{
            m_previewObject.gameObject.SetActive(true);
		}

		private void OnDisable()
		{
			m_previewObject.gameObject.SetActive(false);
		}

		private void Update()
		{
            UpdatePreview();
		}

		public void Initialise(string a_name, int a_nameSizeSteps, int a_subNameSizeSteps, float a_maxBoundsSize, Action<Vector4> a_changeCallback)
        {
            m_nameField.text = a_name;
            RectTransform nameRect = m_nameField.GetComponent<RectTransform>();
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a_nameSizeSteps * m_spacePerStep);
            m_contentContainer.offsetMin = new Vector2((a_nameSizeSteps + 2) * m_spacePerStep, 0f);
            m_maxBoundsSize = a_maxBoundsSize;
			m_changeCallback = a_changeCallback;
            m_blY.Initialise("Bottom Left Y", a_subNameSizeSteps, null, "Coordinate", TMP_InputField.ContentType.DecimalNumber, -1, OnTextFieldChanged);
            m_blX.Initialise("Bottom Left X", a_subNameSizeSteps, null, "Coordinate", TMP_InputField.ContentType.DecimalNumber, -1, OnTextFieldChanged);
            m_tlX.Initialise("Top Right X", a_subNameSizeSteps, null, "Coordinate", TMP_InputField.ContentType.DecimalNumber, -1, OnTextFieldChanged);
            m_tlY.Initialise("Top Right Y", a_subNameSizeSteps, null, "Coordinate", TMP_InputField.ContentType.DecimalNumber, -1, OnTextFieldChanged);
			m_selectBoundsToggle.onValueChanged.AddListener(OnSelectBoundsToggled);
			m_selectBoundsToggle.isOn = false;
			m_zoomToBoundsButton.onClick.AddListener(OnZoomToBoundsButtonClicked);
		}

        public void SetContent(Vector4 a_value)
        {
            m_currentValue = a_value;
            m_selectBoundsToggle.isOn = false;
            UpdateTextFieldToBounds();
		}

		public void SetInteractable(bool a_interactable)
		{
			m_blX.SetInteractable(a_interactable);
			m_blY.SetInteractable(a_interactable);
			m_tlX.SetInteractable(a_interactable);
			m_tlY.SetInteractable(a_interactable);
			m_selectBoundsObject.SetActive(a_interactable);
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
            float xMin = GetCoordinate(m_blX, bounds.xMin);
            float yMin = GetCoordinate(m_blY, bounds.yMin);
            float xMax = GetCoordinate(m_tlX, bounds.xMax);
            float yMax = GetCoordinate(m_tlY, bounds.yMax);

            if(xMax < xMin)
            {
                float t = xMin;
                xMin = xMax;
                xMax = t;
            }
			if (yMax < yMin)
			{
				float t = yMin;
				yMin = yMax;
				yMax = t;
			}
			m_currentValue = new Vector4(xMin, yMin, xMax, yMax);
            if(m_changeCallback != null)
			    m_changeCallback.Invoke(m_currentValue);
		}

        float GetCoordinate(GenericTextField a_textField, float a_default)
        {
            float result = a_default;
            float.TryParse(a_textField.CurrentValue, out result);
            return result;
        }

        void OnSelectBoundsToggled(bool a_value)
        {
            if (a_value)
                Main.Instance.InterruptFSMState((fsm) => new BoundsSelectState(fsm, this));
            else
                Main.Instance.CancelFSMInterruptState();

		}

        void OnZoomToBoundsButtonClicked()
        {
			CameraManager.Instance.ZoomToBounds(
                new Rect(
                    m_currentValue.x * Main.SCALE_FACTOR, 
                    m_currentValue.y * Main.SCALE_FACTOR, 
                    (m_currentValue.z - m_currentValue.x) * Main.SCALE_FACTOR, 
                    (m_currentValue.w - m_currentValue.y) * Main.SCALE_FACTOR), 
                1f);
		}

        void UpdatePreview()
        {
            if (m_previewObject == null)
                return;

            Vector3 from = CameraManager.Instance.gameCamera.WorldToScreenPoint(new Vector3(
                m_currentValue.x * Main.SCALE_FACTOR, 
                m_currentValue.y * Main.SCALE_FACTOR));
			Vector3 to = CameraManager.Instance.gameCamera.WorldToScreenPoint(new Vector3(
                m_currentValue.z * Main.SCALE_FACTOR, 
                m_currentValue.w * Main.SCALE_FACTOR));
            float scale = InterfaceCanvas.Instance.canvas.scaleFactor;

            Vector3 min = Vector3.Min(from, to) - Vector3.one;
            Vector3 max = Vector3.Max(from, to) + Vector3.one;
            // (1 pixel offset because the box in the sprite has a 1 pixel offset from the sprite border)

            m_previewObject.anchoredPosition = min / scale;
			m_previewObject.sizeDelta = (max - min) / scale;
            //TODO: put this in immersive sessions window instead
        }
	}
}
