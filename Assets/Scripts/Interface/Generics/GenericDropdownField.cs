using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace MSP2050.Scripts
{
    public class GenericDropdownField : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_nameField;
        [SerializeField] RectTransform m_contentContainer;
        [SerializeField] TMP_Dropdown m_dropdown;
        [SerializeField] float m_spacePerStep;

        bool m_ignoreCallback;
        Action<int> m_changeCallback;

		public string CurrentValueText => m_dropdown.options[m_dropdown.value].text;
		public int CurrentValue => m_dropdown.value;

		public void Initialise(string a_name, int a_nameSizeSteps, Action<int> a_changeCallback, List<string> a_options)
        {
            m_nameField.text = a_name;
            RectTransform nameRect = m_nameField.GetComponent<RectTransform>();
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a_nameSizeSteps * m_spacePerStep);
            m_contentContainer.anchorMin = new Vector2((a_nameSizeSteps + 2) * m_spacePerStep, 0f);

            SetOptions(a_options, 0);
			m_dropdown.onValueChanged.AddListener(OnValueChanged);
            m_changeCallback = a_changeCallback;
		}

        public void SetContent(int a_index, bool a_ignoreCallback = true)
        {
            if (a_ignoreCallback)
                m_ignoreCallback = true;
			m_dropdown.value = a_index;
			m_ignoreCallback = false;
		}

		public void SetInteractable(bool a_interactable)
		{
			m_dropdown.interactable = a_interactable;
		}

		public void SetOptions(List<string> a_options, int a_selectedIndex)
        {
            m_ignoreCallback = true;
			m_dropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(a_options.Count);
            foreach (string s in a_options)
            {
                options.Add(new TMP_Dropdown.OptionData(s));
            }
            m_dropdown.options = options;
            m_dropdown.value = a_selectedIndex;
            m_ignoreCallback = false;
		}

        void OnValueChanged(int a_value)
        {
            if (m_ignoreCallback || m_changeCallback == null)
                return;

			m_changeCallback.Invoke(a_value);

		}
    }
}
