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

		public void SetContent(string a_name, Sprite a_icon, UnityAction<bool> a_callback)
		{
			m_toggle.onValueChanged.RemoveAllListeners();
			m_toggle.onValueChanged.AddListener(a_callback);
			m_nameText.text = a_name;
			m_icon.sprite = a_icon;
		}
	}
}
