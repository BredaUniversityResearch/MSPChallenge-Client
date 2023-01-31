using UnityEngine;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class ResizeHandle : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public enum RescaleDirectionHor { Left, None, Right }
		public enum RescaleDirectionVer { Up, None, Down }

		public RescaleDirectionHor rescaleDirectionHor = RescaleDirectionHor.Right;
		public RescaleDirectionVer rescaleDirectionVer = RescaleDirectionVer.Down;

		public delegate void OnHandleDragged(PointerEventData data, RectTransform handleRect, RescaleDirectionHor hor, RescaleDirectionVer ver);
		public OnHandleDragged onHandleDragged;

		RectTransform rect;
		FSM.CursorType previousCursorType;

		private void Start()
		{
			rect = GetComponent<RectTransform>();
		}

		public void OnDrag(PointerEventData data)
		{
			if (onHandleDragged != null)
				onHandleDragged(data, rect, rescaleDirectionHor, rescaleDirectionVer);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			previousCursorType = Main.Instance.CursorType;
			Main.Instance.CursorType = FSM.CursorType.Rescale;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Main.Instance.CursorType = previousCursorType;
		}
	}
}
