using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_PolicySelectEntry : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] TextMeshProUGUI m_name;


		APolicyLogic m_policy;

		public void Initialise(APolicyLogic a_policy, Action<APolicyLogic, bool> a_callback)
		{
			m_policy = a_policy;
			if(PolicyManager.Instance.TryGetDefinition(a_policy.m_definition.m_name, out var definition))
				m_name.text = definition.m_displayName;
			m_toggle.onValueChanged.AddListener((b) => a_callback(m_policy, b));
		}

		public void SetValue(bool a_value)
		{
			m_toggle.isOn = a_value;
		}
	}
}
