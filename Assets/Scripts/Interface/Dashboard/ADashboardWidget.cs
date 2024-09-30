using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public abstract class ADashboardWidget : MonoBehaviour
	{
		

		[SerializeField] DashboardWidgetHeader m_header;
		[SerializeField] GameObject m_contentContainer;
		[SerializeField] int m_defaultW;
		[SerializeField] int m_defaultH;

		//Set in individual prefabs
		public DashboardCategory m_category;
		public bool m_startingWidget;

		public DashboardWidgetPosition m_position;
		public DashboardWidgetPosition m_favPosition;

		public int DefaultW => m_defaultW;
		public int DefaultH => m_defaultH;

		private void Awake()
		{
			m_header.m_onDragStart = OnDragStart;
			m_header.m_onDrag = OnDrag;
			m_header.m_onDragEnd = OnDragEnd;
		}

		public virtual void Hide()
		{
			gameObject.SetActive(false);
		}

		public virtual void Show(bool a_favoriteLayout = false)
		{
			gameObject.SetActive(true);
		}

		public void OnDragStart(PointerEventData a_data)
		{
		}

		public void OnDrag(PointerEventData a_data)
		{
			DashboardManager.Instance.ShowWidgetMovePreview(this, a_data);
		}

		public void OnDragEnd(PointerEventData a_data)
		{
			DashboardManager.Instance.OnWidgetMoveRelease(this, a_data);
		}

		public void SetContentActive(bool a_active)
		{
			m_contentContainer.SetActive(a_active);
		}

		public void Reposition(bool a_favoriteLayout = false)
		{
			RectTransform rect = GetComponent<RectTransform>();
			DashboardWidgetPosition pos = a_favoriteLayout ? m_favPosition : m_position;
			rect.sizeDelta = new Vector2(pos.W * DashboardManager.cellsize, pos.H * DashboardManager.cellsize);
			rect.localPosition = new Vector3(pos.X * DashboardManager.cellsize, -pos.Y * DashboardManager.cellsize);
		}

		public void RepositionToPreview(int a_x, int a_y, int a_w, int a_h)
		{
			RectTransform rect = GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(a_w * DashboardManager.cellsize, a_h * DashboardManager.cellsize);
			rect.localPosition = new Vector3(a_x * DashboardManager.cellsize, -a_y * DashboardManager.cellsize);
		}
	}
}