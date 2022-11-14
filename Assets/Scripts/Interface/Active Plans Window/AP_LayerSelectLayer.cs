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
		AP_LayerSelectLayer[] m_dependencies;
		List<AP_LayerSelectLayer> m_dependentOnUs;
		Action<AbstractLayer, bool> m_callback;
		bool m_ignoreCallback;

		public bool IsOn => m_toggle.isOn;

		public void Initialise(AbstractLayer a_layer, Action<AbstractLayer, bool> a_callback)
		{
			m_dependentOnUs = new List<AP_LayerSelectLayer>();
			m_layer = a_layer;
			m_name.text = m_layer.ShortName;
			m_toggle.onValueChanged.AddListener(OnToggleChanged);
			m_callback = a_callback;
		}

		public void LoadDependencies(AP_LayerSelect a_layerSelect)
		{
			if(m_layer.Dependencies != null)
			{
				m_dependencies = new AP_LayerSelectLayer[m_layer.Dependencies.Length];
				for (int i = 0; i < m_layer.Dependencies.Length; i++)
				{
					AP_LayerSelectLayer layerObject = a_layerSelect.GetLayerObjectForLayer(m_layer.Dependencies[i]);
					m_dependencies[i] = layerObject;
					layerObject.RegisterDependency(this);
				}
			}
		}

		void OnToggleChanged(bool a_value)
		{
			if(!m_ignoreCallback)
				m_callback(m_layer, a_value);
		}

		public void ResetValue()
		{
			m_ignoreCallback = true;
			m_toggle.isOn = false;
			m_toggle.interactable = true;
			m_ignoreCallback = false;
		}

		public void SetValue(bool a_value)
		{
			m_toggle.isOn = a_value;
			if (m_dependencies != null)
			{
				foreach (AP_LayerSelectLayer layer in m_dependencies)
					layer.DependentLayerChanged(a_value);
			}
		}

		public void DependentLayerChanged(bool a_value)
		{
			if(a_value)
			{
				m_toggle.interactable = false;
				m_toggle.isOn = true;
			}
			else
			{
				foreach(AP_LayerSelectLayer layer in m_dependentOnUs)
				{
					if (layer.IsOn)
						return;
				}
				m_toggle.interactable = true;
			}
		}

		public void RegisterDependency(AP_LayerSelectLayer a_other)
		{
			m_dependentOnUs.Add(a_other);
		}
	}
}
