using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler /*IEndDragHandler*/
{
	[SerializeField]
	private RectTransform rectTrans;

	private Vector2 dragStartWindowOffset;

	public void Reset()
	{
		rectTrans = GetComponent<RectTransform>();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		dragStartWindowOffset = rectTrans.position;
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 newPosition = dragStartWindowOffset + (eventData.position - eventData.pressPosition);

		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		newPosition = new Vector2(
			Mathf.Clamp(newPosition.x, 0f, Screen.width - (rectTrans.rect.width * scale)),
			Mathf.Clamp(newPosition.y, (rectTrans.rect.height * scale), Screen.height - (41f * scale)));

		rectTrans.position = new Vector2(Mathf.Round(newPosition.x), Mathf.Round(newPosition.y));
	}

	//public void OnEndDrag(PointerEventData eventData) {

	//}
}