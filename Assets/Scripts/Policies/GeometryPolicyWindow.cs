using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public abstract class AGeometryPolicyWindowContent : MonoBehaviour
	{
		[SerializeField] GameObject m_buttonContainer;
		[SerializeField] Button m_closeButton;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;
		[SerializeField] Transform m_contentParent;

		AGeometryPolicyWindowContent m_content;
		Action<Dictionary<Entity, string>> m_changedCallback;

		private void Start()
		{
			m_confirmButton.onClick.AddListener(OnConfirm);
			m_cancelButton.onClick.AddListener(CloseWindow);
			m_closeButton.onClick.AddListener(CloseWindow);
		}

		public void OpenToGeometry(PolicyDefinition a_policyDefinition, Dictionary<Entity, string> a_values, List<Entity> a_geometry, Action<Dictionary<Entity, string>> a_changedCallback)
		{
			m_changedCallback = a_changedCallback;
			m_content = Instantiate(a_policyDefinition.m_windowPrefab, m_contentParent).GetComponent<AGeometryPolicyWindowContent>();
			if (Main.InEditMode)
			{
				m_buttonContainer.SetActive(true);
				m_closeButton.gameObject.SetActive(false);
			}
			else
			{
				m_buttonContainer.SetActive(false);
				m_closeButton.gameObject.SetActive(true);
			}
			m_content.SetContent(a_values, a_geometry);
		}

		void OnConfirm()
		{
			m_changedCallback.Invoke(m_content.GetChanges());
			CloseWindow();
		}

		void CloseWindow()
		{
			Destroy(m_content.gameObject);
			m_content = null;
			gameObject.SetActive(false);
		}
	}
}
