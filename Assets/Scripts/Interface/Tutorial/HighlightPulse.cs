using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class HighlightPulse : MonoBehaviour
	{
		[SerializeField] float m_minSize;
		[SerializeField] float m_maxSize;
		[SerializeField] float m_speed;
		[SerializeField] float m_interval;
		[SerializeField] AnimationCurve m_alphaCurve;
		[SerializeField] Image m_image;

		private RectTransform m_rect;
		private float m_currentSize;
		private float m_remainingInterval = -1f;
		private Transform m_target;

		public void SetTarget(Transform a_target)
		{
			m_target = a_target;
		}

		void Start()
		{
			m_rect = GetComponent<RectTransform>();
			m_currentSize = m_minSize;
		}

		void Update()
		{
			if (m_remainingInterval > 0f)
			{
				m_remainingInterval -= Time.deltaTime;
			}
			else
			{
				transform.position = m_target.position;
				m_currentSize += Time.deltaTime * m_speed;
				if (m_currentSize > m_maxSize)
				{
					m_currentSize = m_minSize;
					m_image.color = Color.clear;
					m_remainingInterval = m_interval;
				}
				else
				{
					m_rect.sizeDelta = new Vector2(m_currentSize, m_currentSize);
					m_image.color = new Color(1f, 1f, 1f, m_alphaCurve.Evaluate((m_currentSize - m_minSize) / (m_maxSize - m_minSize)));
				}
				
			}
		}
	}
}
