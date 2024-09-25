using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class DashboardWidgetHeader : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public delegate void DragEvent(PointerEventData a_pointer);
		public DragEvent m_onDrag;
		public DragEvent m_onDragStart;
		public DragEvent m_onDragEnd;


		public void OnDrag(PointerEventData data)
		{
			if (m_onDrag != null)
				m_onDrag(data);
		}

		public void OnBeginDrag(PointerEventData data)
		{
			if (m_onDragStart != null)
				m_onDragStart(data);
		}

		public void OnEndDrag(PointerEventData data)
		{
			if (m_onDragEnd != null)
				m_onDragEnd(data);
		}
	}
}
