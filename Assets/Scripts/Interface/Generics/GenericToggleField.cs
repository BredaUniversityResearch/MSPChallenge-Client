using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class GenericToggleField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_nameField;
        [SerializeField] RectTransform m_contentContainer;
        [SerializeField] Toggle m_toggle;
        [SerializeField] float m_spacePerStep;

        bool m_ignoreCallback;
        Action<bool> m_changeCallback;

		public bool CurrentValue => m_toggle.isOn;

		public void Initialise(string a_name, int a_nameSizeSteps, Action<bool> a_changeCallback)
        {
            m_nameField.text = a_name;
            RectTransform nameRect = m_nameField.GetComponent<RectTransform>();
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a_nameSizeSteps * m_spacePerStep);
            m_contentContainer.anchorMin = new Vector2((a_nameSizeSteps + 2) * m_spacePerStep, 0f);

			m_toggle.onValueChanged.AddListener(OnValueChanged);
            m_changeCallback = a_changeCallback;
		}

        public void SetContent(bool a_value, bool a_ignoreCallback = true)
        {
            if (a_ignoreCallback)
                m_ignoreCallback = true;
			m_toggle.isOn = a_value;
			m_ignoreCallback = false;
		}

		public void SetInteractable(bool a_interactable)
		{
			m_toggle.interactable = a_interactable;
		}

		void OnValueChanged(bool a_value)
        {
            if (m_ignoreCallback || m_changeCallback == null)
                return;

			m_changeCallback.Invoke(a_value);

		}
    }
}
