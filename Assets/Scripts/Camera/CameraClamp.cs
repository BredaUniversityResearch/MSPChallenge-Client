using UnityEngine;
using System.Collections;

/// <summary>
/// CameraClamp
/// </summary>
public class CameraClamp : MonoBehaviour
{
    [SerializeField]
    private BoxCollider2D borders = null;

    private float cameraMinX, cameraMaxX;
    private float cameraMinY, cameraMaxY;

    private float vertExtent;
    private float horzExtent;

    private float mapX;
    private float mapY;

    private Camera cam;

    public delegate void HasClamped();
    public HasClamped OnClampedDelegate = null;

    protected void Start()
    {
        cam = this.GetComponent<Camera>();

        UpdateBounds();
    }

    public void UpdateBounds()
    {
        mapX = (borders.size.x / 2);
        mapY = (borders.size.y / 2);
    }

    public void SetNewArea(BoxCollider2D collider)
    {
        borders.size = new Vector2(collider.size.x, collider.size.y);
        borders.offset = new Vector2(collider.offset.x, collider.offset.y);
    }

	public void Clamp()
	{
		vertExtent = cam.orthographicSize;
		horzExtent = vertExtent * Screen.width / Screen.height;

		// Calculations assume map is position at the origin
		cameraMinX = (horzExtent - mapX) + borders.offset.x;
		cameraMaxX = (mapX - horzExtent) + borders.offset.x;
		cameraMinY = (vertExtent - mapY) + borders.offset.y;
		cameraMaxY = (mapY - vertExtent) + borders.offset.y;

		Vector3 pos = transform.position;

		pos.x = Mathf.Clamp(pos.x, cameraMinX, cameraMaxX);
		pos.y = Mathf.Clamp(pos.y, cameraMinY, cameraMaxY);

		if (pos != transform.position && OnClampedDelegate != null)
		{
			OnClampedDelegate.Invoke();
		}

		transform.position = pos;
	}

	protected void Update()
	{
		Clamp();
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        Vector3 topleft = new Vector3(cameraMinX, cameraMaxY, 0);
        Vector3 topright = new Vector3(cameraMaxX, cameraMaxY, 0);
        Vector3 bottomleft = new Vector3(cameraMinX, cameraMinY, 0);
        Vector3 bottomright = new Vector3(cameraMaxX, cameraMinY, 0);

        Gizmos.DrawLine(topleft, topright);
        Gizmos.DrawLine(bottomleft, bottomright);
        Gizmos.DrawLine(topleft, bottomleft);
        Gizmos.DrawLine(topright, bottomright);
    }
}
