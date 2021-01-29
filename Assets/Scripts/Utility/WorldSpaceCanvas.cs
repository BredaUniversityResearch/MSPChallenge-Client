using UnityEngine;

public class WorldSpaceCanvas : MonoBehaviour
{
	private Canvas canvas;

	private static WorldSpaceCanvas instance;

	protected void Start()
	{
		canvas = this.GetComponent<Canvas>();
		instance = this;
	}

	public void Resize(BoxCollider2D bounds)
	{
		//Make sure the canvas is in front of all other world elements by setting the Z to -100
		canvas.transform.position = new Vector3(bounds.offset.x, bounds.offset.y, -100.0f);
		canvas.GetComponent<RectTransform>().sizeDelta = bounds.size;
	}

	public static void ResizeToPlayArea(BoxCollider2D bounds)
	{
		instance.Resize(bounds);
	}
}