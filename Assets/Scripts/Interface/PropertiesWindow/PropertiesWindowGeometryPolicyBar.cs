using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class PropertiesWindowGeometryPolicyBar : MonoBehaviour
	{
		[SerializeField] private Toggle m_barToggle = null;
		[SerializeField] private Image m_policyToggleImage = null;
		[SerializeField] private TextMeshProUGUI m_nameText = null;
		[SerializeField] private Sprite m_activeSprite = null;
		[SerializeField] private Sprite m_inactiveSprite = null;

		private AGeometryPolicyWindow m_geometryPolicyWindow;
		private PolicyDefinition m_policyDefinition;
		private string m_policyData;
		private Entity m_geometry;
		bool m_ignoreCallbacks;

		void Start()
		{
			m_barToggle.onValueChanged.AddListener(OnBarToggleChange);
		}

		void OnBarToggleChange(bool a_value)
		{
			if (m_ignoreCallbacks)
				return;
			if (a_value)
			{
				m_geometryPolicyWindow.OpenToGeometry(m_policyDefinition, m_policyData, m_geometry, OnGeometryPolicyWindowCloseOrChange);
			}
			else
			{
				m_geometryPolicyWindow.CloseWindow();
			}
		}

		void OnGeometryPolicyWindowCloseOrChange()
		{
			m_ignoreCallbacks = true;
			m_barToggle.isOn = false;
			m_ignoreCallbacks = false;
		}

		public void SetValue(EntityPropertyMetaData a_parameter, string a_value, Entity a_geometry, AGeometryPolicyWindow a_geometryPolicyWindow)
		{
			if (!PolicyManager.Instance.TryGetDefinition(a_parameter.PolicyType, out m_policyDefinition))
				Debug.LogError("No policy definition found for expected policy type: " + a_parameter.PolicyType);
			m_nameText.text = a_parameter.DisplayName;

			m_geometryPolicyWindow = a_geometryPolicyWindow;
			m_policyData = a_value;
			m_geometry = a_geometry;
			m_policyToggleImage.sprite = string.IsNullOrEmpty(a_value) ? m_inactiveSprite : m_activeSprite;
		}
	}
}
