using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// CameraZoom
/// </summary>
public class CameraZoom : MonoBehaviour
{
	public static Vector3 LastZoomLocation = Vector3.zero;
	public float maximumZoomLevel = 200.0f; //was 18, thats what she said

	[SerializeField]
	private BoxCollider2D borders = null;

	//Changing these doesn't change zoom level
	private float minZoom = 1.0f; 
	private float maxZoom = 60.0f;
    public float currentZoom { get; private set; } //0 = zoomed in, 1 = zoomed out

	[SerializeField]
	private float speed = 30.0f;

	[SerializeField]
	private Camera cameraComponent = null;
	private float previousCamAspect;

	private bool autoZoom;
	private float sourceZoom;
	private float targetZoom;
	private IEasingFunction autoZoomEase = new EaseOut(3.0f);
	
	private Coroutine rescaleCoroutine = null;

	protected void Start()
	{
		UpdateBounds();

		autoZoom = false;
        if (Main.MspGlobalData != null)
        {
            maximumZoomLevel = float.Parse(Main.MspGlobalData.maxzoom, Localisation.NumberFormatting);
        }
        else
        {
            Main.OnGlobalDataLoaded += () => { maximumZoomLevel = float.Parse(Main.MspGlobalData.maxzoom, Localisation.NumberFormatting); };
        }
    }

	public void SetNewArea(BoxCollider2D collider)
	{
		borders.size = new Vector2(collider.size.x, collider.size.y);
		borders.offset = new Vector2(collider.offset.x, collider.offset.y);
	}

	public void UpdateBounds()
	{
		float maxXZoom = (borders.size.x / 2) / cameraComponent.aspect;
		float maxYZoom = (borders.size.y / 2);
		maxZoom = Mathf.Min(maxXZoom, maxYZoom);
		minZoom = maxZoom / maximumZoomLevel;

		targetZoom = maxZoom;
		cameraComponent.orthographicSize = targetZoom;
		UIManager.GetMapScale().SetScale(cameraComponent.orthographicSize);
	}

	public void ForceUpdateBoundsNextFrame()
	{
		previousCamAspect = 0.0f;
	}

	protected void Update()
	{
		if (cameraComponent.aspect != previousCamAspect)
		{
			UpdateBounds();

			previousCamAspect = cameraComponent.aspect;
		}

		if (EventSystem.current.IsPointerOverGameObject())
		{
			if (!CameraManager.Instance.canIZoom)
				return;
		}

		float wheel = Input.GetAxis("Mouse ScrollWheel");

		if (wheel != 0)
		{
			ZoomOrthoCamera(cameraComponent.ScreenToWorldPoint(Input.mousePosition), (wheel * speed * cameraComponent.orthographicSize) /**Time.deltaTime*/ * 0.01f);
			autoZoom = false;
		}
	}

	public void StartMaxZoomOut(float speed)
	{
		StopAllCoroutines();
		autoZoom = true;
		sourceZoom = cameraComponent.orthographicSize;
		targetZoom = maxZoom;
		StartCoroutine(AutoZoomCoroutine(speed));
	}

	public void StartAutoZoom(float targetZoomLevel, float speed)
	{
		//if (Mathf.Approximately(destination.center.x, 0) && Mathf.Approximately(destination.center.y, 0))
		//{
		//	return;
		//}

		StopAllCoroutines();
		autoZoom = true;
		sourceZoom = cameraComponent.orthographicSize;
		targetZoom = Mathf.Clamp(targetZoomLevel, minZoom, maxZoom);
		StartCoroutine(AutoZoomCoroutine(speed));
	}

	public void EndAutoZoom()
	{
		UpdateScaleNow(cameraComponent);
		autoZoom = false;
	}

	private IEnumerator AutoZoomCoroutine(float speed)
	{
		float time = 0;
		while (autoZoom)
		{
			if (time < 1)
			{
				cameraComponent.orthographicSize = Mathf.Lerp(sourceZoom, targetZoom, autoZoomEase.Evaluate(time));
                currentZoom = (cameraComponent.orthographicSize - minZoom) / (maxZoom - minZoom);
                time += Time.deltaTime / speed;
			}
			else
			{
				cameraComponent.orthographicSize = targetZoom;
                currentZoom = (cameraComponent.orthographicSize - minZoom) / (maxZoom - minZoom);
				time = 1;
                EndAutoZoom();
			}
			VisualizationUtil.UpdateDisplayScale(cameraComponent);
			//Don't start the DelayedUpdateScale coroutine as this will never be triggered...
			UpdateUIScale();

			yield return null;
		}
		yield return null;
	}

	public void Clamp()
	{
		cameraComponent.orthographicSize = Mathf.Clamp(cameraComponent.orthographicSize, minZoom, maxZoom);
        currentZoom = (cameraComponent.orthographicSize - minZoom) / (maxZoom - minZoom);
	}

	// http://answers.unity3d.com/questions/384753/ortho-camera-zoom-to-mouse-point.html
	public void ZoomOrthoCamera(Vector3 zoomTowards, float amount)
	{
		LastZoomLocation = zoomTowards;
		float multiplier = (1.0f / cameraComponent.orthographicSize * amount);

		if (cameraComponent.orthographicSize > minZoom && cameraComponent.orthographicSize < maxZoom)
		{
			transform.position += (zoomTowards - transform.position) * multiplier;
		}

		cameraComponent.orthographicSize -= amount;

		Clamp();

		//VisualizationUtil.UpdateDisplayScale();
		if (rescaleCoroutine == null)
		{
			rescaleCoroutine = StartCoroutine(DelayedUpdateScale());
		}
		UpdateUIScale();
	}

	public void UpdateUIScale()
	{
		VisualizationUtil.UpdateDisplayScale(cameraComponent);
		IssueManager.instance.RescaleIssues();
		UIManager.GetMapScale().SetScale(cameraComponent.orthographicSize);
        FSM.CameraZoomChanged();
	}

	private void UpdateScaleNow(Camera targetCamera)
	{
		VisualizationUtil.UpdateDisplayScale(targetCamera);
		LayerManager.UpdateLayerScales(targetCamera);
	}

	private IEnumerator DelayedUpdateScale()
	{
		yield return new WaitForSeconds(0.4f);
		UpdateScaleNow(cameraComponent);
		rescaleCoroutine = null;
		yield return null;
	}

	public float GetMaxZoom()
	{
		return maxZoom;
	}

    /// <summary>
    /// Set the zoom level, using the current zoom bounds
    /// Expects a 0-1 input value
    /// </summary>
    public void SetZoomLevel(float newZoomLevel)
    {
        cameraComponent.orthographicSize = (maxZoom - minZoom) * Mathf.Clamp01(newZoomLevel) + minZoom;
        Clamp();
        UpdateUIScale();
        UpdateScaleNow(cameraComponent);
    }
}