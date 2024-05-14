using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public abstract class GeometryPolicyWindow : MonoBehaviour
	{
		[SerializeField] GameObject m_buttonContainer;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;

		private void Start()
		{
			//TODO: Set button callbacks
		}

		public void OpenToGeometry(List<Entity> a_geometry, RectTransform a_parentWindow)
		{
			//TODO: align content
		}

		protected abstract void SetContent(List<Entity> a_geometry);

		void OnCancel()
		{ }

		void OnConfirm()
		{ }
	}
}
