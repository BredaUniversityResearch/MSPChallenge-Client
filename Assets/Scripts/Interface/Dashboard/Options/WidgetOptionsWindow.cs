using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class WidgetOptionsWindow : MonoBehaviour
	{
		[SerializeField] Transform m_optionParent;
		[SerializeField] GameObject m_toggleOptionPrefab;

		public void AddToggle(string a_name, bool a_value, Action<bool> a_callback)
		{ 
			WidgetOptionToggle toggle = Instantiate(m_toggleOptionPrefab, m_optionParent).GetComponent<WidgetOptionToggle>();
			toggle.Initialise(a_name, a_value, a_callback);
		}
	}
}