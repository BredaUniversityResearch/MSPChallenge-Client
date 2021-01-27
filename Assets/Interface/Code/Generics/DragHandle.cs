using UnityEngine.EventSystems;
using UnityEngine;

public class DragHandle : MonoBehaviour, IDragHandler
{
	public delegate void OnHandleDragged(PointerEventData data, RectTransform handleRect);
	public OnHandleDragged onHandleDragged;

	RectTransform rect;

	private void Start()
	{
		rect = GetComponent<RectTransform>();
	}

	public void OnDrag(PointerEventData data)
	{
		if (onHandleDragged != null)
			onHandleDragged(data, rect);
	}
}
