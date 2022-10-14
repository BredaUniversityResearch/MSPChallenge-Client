using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_LayerSelectLayer : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] TextMeshProUGUI m_name;
		AbstractLayer m_layer;

		public void Initialise(AbstractLayer a_layer, Action<AbstractLayer, bool> a_callback)
		{
			m_layer = a_layer;
			m_name.text = m_layer.ShortName;
			m_toggle.onValueChanged.AddListener((b) => a_callback(m_layer, b));
		}

		public void SetValue(bool a_value)
		{
			m_toggle.isOn = a_value;
		}
	}
}
