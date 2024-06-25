using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class AGeometryPolicyWindow : MonoBehaviour
	{
		[SerializeField] Button m_closeButton;
		[SerializeField] Transform m_contentParent;
		[SerializeField] TextMeshProUGUI m_windowTitle;

		AGeometryPolicyWindowContent m_content;

		private void Start()
		{
			m_closeButton.onClick.AddListener(CloseWindow);
		}

		public void OpenToGeometry(PolicyDefinition a_policyDefinition, Dictionary<Entity, string> a_values, List<Entity> a_geometry, Action<Dictionary<Entity, string>> a_changedCallback)
		{
			m_windowTitle.text = a_policyDefinition.m_displayName;
			if(m_content != null)
				Destroy(m_content.gameObject);
			m_content = Instantiate(a_policyDefinition.m_windowPrefab, m_contentParent).GetComponent<AGeometryPolicyWindowContent>();
			m_content.SetContent(a_values, a_geometry, a_changedCallback);
			gameObject.SetActive(true);
		}

		public void CloseWindow()
		{
			if(m_content != null)
				Destroy(m_content.gameObject);
			m_content = null;
			gameObject.SetActive(false);
		}
	}
}
