using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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
		private Vector2 m_maskImageOffset;

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
				m_maskedImage.position = m_maskImageOffset;
			}
		}

		private void OnRectTransformDimensionsChange()
		{
			StartCoroutine(UpdateUpdateImageSizesDelayedHelper());
		}

		IEnumerator UpdateUpdateImageSizesDelayedHelper()
		{
			yield return new WaitForEndOfFrame();
			UpdateImageSizes();
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

			m_maskImageOffset = new Vector2((canvasRect.sizeDelta.x- width) / 2f, (canvasRect.sizeDelta.y- height) / 2f);
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
