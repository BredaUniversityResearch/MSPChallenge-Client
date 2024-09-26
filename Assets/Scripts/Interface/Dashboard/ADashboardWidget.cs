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

		protected DashboardWidgetPosition m_position;
		protected DashboardWidgetPosition m_favPosition;

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
			m_contentContainer.SetActive(false);
			//TODO: set preview outline true, or leave content active?
			//TODO: Remove widget from core structure, collapse if possible
		}

		public void OnDrag(PointerEventData a_data)
		{
			//Detect new location, show preview?
			//When over / overlapping, add insertion preview above row
			//When over available space, add preview for full widget
			DashboardManager.Instance.ShowWidgetMovePreview(this, a_data);
		}

		public void OnDragEnd(PointerEventData a_data)
		{
			m_contentContainer.SetActive(true);
			//TODO: set preview outline false, or leave content active?
			DashboardManager.Instance.OnWidgetMoveRelease(this, a_data);
		}
	}
}