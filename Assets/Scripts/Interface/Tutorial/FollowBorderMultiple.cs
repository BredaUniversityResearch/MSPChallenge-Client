using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class FollowBorderMultiple : MonoBehaviour, IHighlightObject
	{
		[SerializeField] float m_duration;
		[SerializeField] float m_timeOffset;
		[SerializeField] AnimationCurve m_curve;
		[SerializeField] RectTransform[] m_visuals;

		private RectTransform m_rect;
		private RectTransform m_target;
		Vector3[] m_points;

		float m_timePassed;
		float m_totalDistance;
		float m_width;
		float m_height;
		bool m_wasActive = true;

		public void SetTarget(RectTransform a_target)
		{
			m_target = a_target;
			m_rect = GetComponent<RectTransform>();
		}

		void Update()
		{
			bool active = m_target.gameObject.activeInHierarchy;
			if (m_wasActive != active)
			{
				m_wasActive = active;
				for (int i = 0; i < m_visuals.Length; i++)
				{
					m_visuals[i].gameObject.SetActive(active);
				}			
			}
			if (!active)
				return;

			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			m_points = new Vector3[4];
			m_target.GetWorldCorners(m_points);
			for (int i = 0; i < 4; i++)
				m_points[i] = m_points[i] / scale;
			m_width = Mathf.Abs(m_points[0].x - m_points[2].x);
			m_height = Mathf.Abs(m_points[0].y - m_points[2].y);
			m_totalDistance = m_width * 2f + m_height * 2f;
			if (m_totalDistance < 0.01f)
				return;

			m_timePassed += Time.deltaTime;
			if (m_timePassed > m_duration)
				m_timePassed -= m_duration;

			for(int i = 0; i < m_visuals.Length; i++)
			{
				float t = m_timePassed + m_timeOffset * i;
				if(t > m_duration)
					t -= m_duration;
				t /= m_duration;

				float distance = m_curve.Evaluate(t) * m_totalDistance;
				if(distance < m_height)
				{
					m_visuals[i].anchoredPosition = new Vector2(m_points[0].x, m_points[0].y + distance);
				}
				else if (distance < m_height+m_width)
				{ 
					m_visuals[i].anchoredPosition = new Vector2(m_points[1].x + (distance-m_height), m_points[1].y);
				}
				else if(distance < m_height*2f + m_width)
				{
					m_visuals[i].anchoredPosition = new Vector2(m_points[2].x, m_points[2].y - (distance-m_width-m_height));
				}
				else
				{
					m_visuals[i].anchoredPosition = new Vector2(m_points[0].x + (m_totalDistance - distance), m_points[0].y);
				}
			}
		}
	}
}
