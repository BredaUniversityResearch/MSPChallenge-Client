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
			m_confirmButton.onClick.AddListener(OnConfirm);
			m_cancelButton.onClick.AddListener(OnCancel);
		}

		public void OpenToGeometry(List<Entity> a_geometry, RectTransform a_parentWindow)
		{
			//TODO: align content
			if(Main.InEditMode)
			{ 
				//Align to geometry tool
			}
			else
			{
				//Align to properties window
			}
			SetContent(a_geometry);
		}


		void OnCancel()
		{ 
			
		}

		void OnConfirm()
		{
		
		}

		protected abstract void SetContent(List<Entity> a_geometry);
		protected abstract void ApplyContent(List<Entity> a_geometry);
	}
}
