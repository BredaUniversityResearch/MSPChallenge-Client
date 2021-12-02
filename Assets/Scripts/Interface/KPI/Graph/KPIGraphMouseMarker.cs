using UnityEngine;

class KPIGraphMouseMarker: MonoBehaviour
{
	[SerializeField]
	private RectTransform horizontalLine = null;
	[SerializeField]
	private RectTransform verticalLine = null;

	private bool ignoreMousePosition = false;

	private void Update()
	{
		if (ignoreMousePosition)
		{
			return;
		}

		Rect screenRect = GetScreenRect();
		Vector2 relativeMousePosition = new Vector2(((Input.mousePosition.x - screenRect.x) / screenRect.width), ((Input.mousePosition.y - screenRect.y) / screenRect.height));
		SetPosition(relativeMousePosition);
	}

	public void SetFixedMarkerPoint(Vector2 position, bool shouldIgnoreMousePosition)
	{
		ignoreMousePosition = shouldIgnoreMousePosition;

		if (ignoreMousePosition)
		{
			Rect screenRect = GetScreenRect();
			float canvasScale = InterfaceCanvas.Instance.canvas.scaleFactor;

			SetPosition(new Vector2(position.x / screenRect.width * canvasScale, position.y / screenRect.height * canvasScale));
		}
	}

	private Rect GetScreenRect()
	{
		RectTransform rectTransform = (RectTransform)transform;
		Vector3[] worldCorners = new Vector3[4];
		rectTransform.GetWorldCorners(worldCorners);
		return new Rect(worldCorners[0], worldCorners[2] - worldCorners[0]);
	}

	private void SetPosition(Vector2 localRelativePosition)
	{
		if ((localRelativePosition.x >= 0.0f && localRelativePosition.x <= 1.0f) && (localRelativePosition.y >= 0.0f && localRelativePosition.y <= 1.0f))
		{
			verticalLine.anchorMin = new Vector2(localRelativePosition.x, 0.0f);
			verticalLine.anchorMax = new Vector2(localRelativePosition.x, 1.0f);

			horizontalLine.anchorMin = new Vector2(0.0f, localRelativePosition.y);
			horizontalLine.anchorMax = new Vector2(1.0f, localRelativePosition.y);

			verticalLine.gameObject.SetActive(true);
			horizontalLine.gameObject.SetActive(true);
		}
		else
		{
			verticalLine.gameObject.SetActive(false);
			horizontalLine.gameObject.SetActive(false);
		}
	}
}
