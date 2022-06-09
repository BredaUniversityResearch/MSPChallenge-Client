using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class LoginBGMask : MonoBehaviour
	{
		[SerializeField] private RectTransform m_bgRect;
		[SerializeField] private RectTransform m_mask;
		[SerializeField] private RectTransform m_maskedImage;
		private Canvas m_canvas;
		private bool m_initialized;
		private bool m_active;

		void Update()
		{
			if (!m_initialized)
			{
				m_initialized = true;
				UpdateImageSizes(); //Has to be done in update so canvas has initialized
			}

			if (m_active)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvas.transform as RectTransform, Input.mousePosition, m_canvas.worldCamera, out var localMousePos);
				m_mask.anchoredPosition = new Vector2(0f, localMousePos.y);
				m_maskedImage.anchoredPosition = -m_mask.anchoredPosition;
			}
		}

		//This is also called when display options change, linked through the inspector callback
		public void UpdateImageSizes()
		{
			m_canvas = GetComponentInParent<Canvas>();
			Image bg = m_bgRect.GetComponent<Image>();
			RectTransform canvasRect = m_canvas.transform as RectTransform;
			float bgAspect = (float)bg.sprite.texture.width / (float)bg.sprite.texture.height;
			float canvasAspect = canvasRect.sizeDelta.x / canvasRect.sizeDelta.y;

			float height, width;
			if (bgAspect > canvasAspect)
			{
				//Match height
				height = canvasRect.sizeDelta.y;
				width = height * bgAspect;
			}
			else
			{
				//Match width
				width = canvasRect.sizeDelta.x;
				height = width / bgAspect;
			}

			m_bgRect.sizeDelta = new Vector2(width, height);
			m_maskedImage.sizeDelta = new Vector2(width, height);
		}

		public void SetMaskActive(bool a_active)
		{
			m_active = a_active;
			m_mask.gameObject.SetActive(a_active);
		}
	}
}
