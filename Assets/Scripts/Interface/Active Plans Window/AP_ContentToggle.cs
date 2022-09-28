using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace MSP2050.Scripts
{
	public class AP_ContentToggle : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] TextMeshProUGUI m_nameText;
		[SerializeField] Image m_icon;

		public void Initialise(UnityAction<bool> a_callback, ToggleGroup a_toggleGroup)
		{
			m_toggle.onValueChanged.RemoveAllListeners();
			m_toggle.onValueChanged.AddListener(a_callback);
			m_toggle.group = a_toggleGroup;
		}

		public void SetContent(string a_text, Sprite a_icon)
		{
			m_nameText.text = a_text;
			m_icon.sprite = a_icon;
			gameObject.SetActive(true);
		}

		public void SetContent(string a_text)
		{
			m_nameText.text = a_text;
		}
	}
}
