using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class GenericTextField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_nameField;
        [SerializeField] RectTransform m_contentContainer;
        [SerializeField] TMP_InputField m_inputField;
        [SerializeField] TextMeshProUGUI m_inputFieldPlaceHolder;
        [SerializeField] float m_spacePerStep;

        bool m_ignoreCallback;
        Action<string> m_changeCallback;

        public string CurrentValue => m_inputField.text;

		public void Initialise(string a_name, int a_nameSizeSteps, Action<string> a_changeCallback, string a_placeHolderText, TMP_InputField.ContentType a_contentType = TMP_InputField.ContentType.Standard, int a_characterLimit = -1)
        {
            m_nameField.text = a_name;
            RectTransform nameRect = m_nameField.GetComponent<RectTransform>();
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a_nameSizeSteps * m_spacePerStep);
            m_contentContainer.anchorMin = new Vector2((a_nameSizeSteps + 2) * m_spacePerStep, 0f);

			m_inputFieldPlaceHolder.text = a_placeHolderText;
            m_inputField.onValueChanged.AddListener(OnValueChanged);
            m_inputField.contentType = a_contentType;
            m_inputField.characterLimit = a_characterLimit;
            m_changeCallback = a_changeCallback;
		}

        public void SetContent(string a_text, bool a_ignoreCallback = true)
        {
            if (a_ignoreCallback)
                m_ignoreCallback = true;
            m_inputField.text = a_text;
			m_ignoreCallback = false;

		}

		public void SetInteractable(bool a_interactable)
		{
			m_inputField.interactable = a_interactable;
		}

		void OnValueChanged(string a_value)
        {
            if (m_ignoreCallback || m_changeCallback == null)
                return;

			m_changeCallback.Invoke(a_value);

		}
    }
}
