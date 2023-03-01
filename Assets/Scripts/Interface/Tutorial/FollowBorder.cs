using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class FollowBorder : MonoBehaviour
	{
		[SerializeField] float m_speed;

		private RectTransform m_rect;
		private RectTransform m_target;
		int m_nextIndex = 1;
		Vector3[] m_points;
		Vector3 m_worldPosition;

		public void SetTarget(RectTransform a_target)
		{
			m_target = a_target;
			m_points = new Vector3[4];
			m_target.GetWorldCorners(m_points);
			if (Vector3.SqrMagnitude(m_points[0] - m_points[1]) + Vector3.SqrMagnitude(m_points[0] - m_points[2]) < 0.01f)
				gameObject.SetActive(false);
			m_rect = GetComponent<RectTransform>();
			m_worldPosition = m_points[0];
		}

		void Update()
		{
			float remainingDistance = m_speed * Time.deltaTime;
			while(true)
			{
				float toNext = Vector3.Distance(m_worldPosition, m_points[m_nextIndex]);
				if(toNext > remainingDistance)
				{
					m_worldPosition += (m_points[m_nextIndex] - m_worldPosition).normalized * remainingDistance;
					break;
				}
				else
				{
					m_worldPosition = m_points[m_nextIndex];
					remainingDistance -= toNext;
					m_nextIndex++;
					if (m_nextIndex >= 4)
						m_nextIndex = 0;
				}
			}
			m_rect.anchoredPosition = m_worldPosition;
			//m_rect.position = CameraManager.Instance.gameCamera.WorldToScreenPoint(m_worldPosition);
		}
	}
}
