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
		[SerializeField] private float m_maskSpeed = 0.05f;
		[SerializeField] private float m_maskSizeY = 0.1f;
		private Canvas m_canvas;
		private bool m_initialized;
		private bool m_active;
		private bool m_movingDown;
		private float m_maskPosition = 0.5f;
		private Vector2 m_maskOrigin;

		void Update()
		{
			if (!m_initialized)
			{
				m_initialized = true;
				UpdateImageSizes(); //Has to be done in update so canvas has initialized
			}

			if (m_active)
			{
				if(m_movingDown)
				{
					m_maskPosition -= m_maskSpeed * Time.deltaTime;
					if (m_maskPosition < 0f)
					{
						m_movingDown = false;
						m_maskPosition = -m_maskPosition + m_maskSizeY;
					}
					else
					{
						m_mask.anchorMin = new Vector2(0f, m_maskPosition);
						m_mask.anchorMax = new Vector2(01f, m_maskPosition + m_maskSizeY);
					}
				}
				else
				{
					m_maskPosition += m_maskSpeed * Time.deltaTime;
					if (m_maskPosition > 1f)
					{
						m_movingDown = true;
						m_maskPosition = 2f - m_maskPosition - m_maskSizeY;
					}
					else
					{
						m_mask.anchorMin = new Vector2(0f, m_maskPosition - m_maskSizeY);
						m_mask.anchorMax = new Vector2(01f, m_maskPosition);
					}
				}
				//RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvas.transform as RectTransform, Input.mousePosition, m_canvas.worldCamera, out var localMousePos);
				//m_mask.anchoredPosition = new Vector2(0f, Mathf.MoveTowards(m_mask.anchoredPosition.y, localMousePos.y, m_maskFollowSpeed * Time.deltaTime));
				//m_maskedImage.anchoredPosition = -m_mask.anchoredPosition;
				m_maskedImage.position = m_maskOrigin;
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
			m_maskOrigin = new Vector2(width / 2f, height / 2f);
		}

		public void SetMaskActive(bool a_active)
		{
			m_active = a_active;
			m_mask.gameObject.SetActive(a_active);
		}
	}
}
