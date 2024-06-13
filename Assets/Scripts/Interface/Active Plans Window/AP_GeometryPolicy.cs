using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class AP_GeometryPolicy : MonoBehaviour
	{
		[SerializeField] private Button m_barButton = null;
		[SerializeField] private ToggleMixedValue m_policyToggle = null;
		[SerializeField] private TextMeshProUGUI m_nameText = null;
		//[SerializeField] private Image m_icon = null;

		public AP_GeometryTool.GeometryPolicyChangeCallback m_changedCallback;

		private EntityPropertyMetaData m_parameter;
		private AGeometryPolicyWindow m_geometryPolicyWindow;
		private PolicyDefinition m_policyDefinition;
		private Dictionary<Entity, string> m_policyData;
		private List<Entity> m_geometry;
		bool m_ignoreCallbacks;

		void Start()
		{
			m_barButton.onClick.AddListener(OnBarButtonClick);
			m_policyToggle.m_onValueChangeCallback = OnToggleChange;
		}

		void OnBarButtonClick()
		{
			if (m_ignoreCallbacks)
				return;
			m_geometryPolicyWindow.OpenToGeometry(m_policyDefinition, m_policyData, m_geometry, OnPolicyValuesChanged);
		}

		void OnToggleChange(bool a_newValue)
		{
			if (m_ignoreCallbacks)
				return;
			if(a_newValue)
			{
				string emptyPolicy = JsonConvert.SerializeObject(Activator.CreateInstance(m_policyDefinition.m_planUpdateType));
				foreach (Entity e in m_geometry)
				{
					if (!m_policyData.TryGetValue(e, out string value) || value == null)
						m_policyData[e] = emptyPolicy;
				}
			}
			else
			{
				foreach (Entity e in m_geometry)
				{
					if(m_policyData.ContainsKey(e))
						m_policyData[e] = null;
				}
			}
		}

		public void SetValue(Dictionary<Entity,string> a_values, List<Entity> a_geometry, AGeometryPolicyWindow a_geometryPolicyWindow)
		{
			m_geometryPolicyWindow = a_geometryPolicyWindow;
			m_policyData = a_values;
			m_geometry = a_geometry;

			UpdateToggleState();
		}

		void OnPolicyValuesChanged(Dictionary<Entity, string> a_values)
		{
			//Note: a_values only contains changed values
			foreach(var kvp in a_values)
			{
				m_policyData[kvp.Key] = kvp.Value;
			}
			UpdateToggleState();
			m_changedCallback.Invoke(m_parameter, a_values);
		}

		void UpdateToggleState()
		{
			m_ignoreCallbacks = true;
			if (m_policyData.Count == 0)
			{
				m_policyToggle.Value = false;
			}
			else if (m_policyData.Count != m_geometry.Count)
			{
				m_policyToggle.Value = null;
			}
			else
			{
				bool? result = null;
				bool first = true;
				foreach(var kvp in m_policyData)
				{
					if(first)
					{
						result = !string.IsNullOrEmpty(kvp.Value);
						first = false;
					}
					else if (string.IsNullOrEmpty(kvp.Value) == result.Value) //Check if any policy data is set to null
					{
						result = null;
						break;
					}
				}
				m_policyToggle.Value = result;
			}
			m_ignoreCallbacks = false;
		}

		public void SetInteractable(bool value, bool reset = true)
		{
			m_barButton.interactable = value;
			m_policyToggle.Interactable = value;

			if (reset)
			{
				m_ignoreCallbacks = true;
				m_policyToggle.Value = false;
				m_ignoreCallbacks = false;
			}
		}

		public void SetToPolicy(EntityPropertyMetaData a_parameter)
		{
			m_parameter = a_parameter;
			if (!PolicyManager.Instance.TryGetDefinition(a_parameter.PolicyType, out m_policyDefinition))
				Debug.LogError("No policy definition found for expected policy type: " + a_parameter.PolicyType);

			m_nameText.text = a_parameter.DisplayName;
			SetInteractable(false);
			//if (!string.IsNullOrEmpty(a_parameter.SpriteName))
			//	m_icon.sprite = Resources.Load<Sprite>(a_parameter.SpriteName);
			//else
			//	m_icon.gameObject.SetActive(false);
		}
	}
}
