using UnityEngine;
using UnityEngine.EventSystems;

public class ResizeHandle : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
	public enum RescaleDirection { Horizontal, Vertical, Both }
	public RescaleDirection rescaleDirection;

	public delegate void OnHandleDragged(PointerEventData data, RectTransform handleRect, RescaleDirection direction);
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
			onHandleDragged(data, rect, rescaleDirection);
	}

    public void OnPointerEnter(PointerEventData eventData)
    {
        previousCursorType = Main.CursorType;
        Main.CursorType = FSM.CursorType.Rescale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Main.CursorType = previousCursorType;
    }
}
