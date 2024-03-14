using UnityEngine;

namespace MSP2050.Scripts
{
	public class CameraManager: MonoBehaviour
	{
		public Camera gameCamera;
		public CameraZoom cameraZoom;
		public CameraPan cameraPan;
		public CameraClamp cameraClamp;

		[HideInInspector]
		public bool canIZoom = false;

		public static CameraManager Instance
		{
			get;
			private set;
		}

		public void Awake()
		{
			if (Instance != null)
			{
				Debug.LogError("CameraManager Instance is not null. Do we have multiple camera managers in the current scene?");
			}

			Instance = this;

			cameraClamp.OnClampedDelegate = OnCameraClamped;
		}

		private void OnDestroy()
		{
			if (Instance != this)
			{
				Debug.LogError("CameraManager Instance is not a reference to 'this'. Do we have multiple camera managers in the current scene?");
			}
			Instance = null;
		}

		private void OnCameraClamped()
		{
			if (cameraZoom != null)
			{
				cameraZoom.Clamp();
			}
		}

		public void ZoomToBounds(Rect bounds, float boundRelativeSize = 1.5f)
		{
			float speed = 0.75f;
			float boundHeight = bounds.size.y;
			float boundAspect = bounds.width / bounds.height;

			//Alter bound height to match width with the camera aspect
			if (boundAspect > gameCamera.aspect)      
				boundHeight = boundHeight * (boundAspect / gameCamera.aspect);       
        
			cameraPan.StartAutoPan(bounds.center, speed);
			cameraZoom.StartAutoZoom(boundHeight * 0.5f * boundRelativeSize, speed);
		}

		public void ZoomOutCompletely()
		{
			cameraZoom.StartMaxZoomOut(1f);
		}

		public void UpdateBounds()
		{
			//Have to do an update AND next frame. Update is for when we are loading in, next frame is for handling resolution changes.
			cameraZoom.UpdateBounds(); 
			cameraZoom.ForceUpdateBoundsNextFrame();
			cameraClamp.UpdateBounds();
		}

		private void SetNewArea(BoxCollider2D collider)
		{
			cameraZoom.SetNewArea(collider);
			cameraClamp.SetNewArea(collider);
		}

		public void GetNewPlayArea()
		{
			AbstractLayer layer = LayerManager.Instance.FindFirstLayerContainingName("_PLAYAREA");

			if (layer == null)
			{
				Debug.LogError("No play area loaded, defaulting to North Sea");
				return;
			}

			Rect zoomRect = layer.GetEntity(layer.GetEntityCount() - 1).GetEntityBounds();
			BoxCollider2D zoomCollider = layer.LayerGameObject.AddComponent<BoxCollider2D>();
			zoomCollider.size = zoomRect.size;
			zoomCollider.offset = zoomRect.center;

			Rect fullRect = layer.GetLayerBounds();
			BoxCollider2D fullCollider = layer.LayerGameObject.AddComponent<BoxCollider2D>();
			fullCollider.size = fullRect.size;
			fullCollider.offset = fullRect.center;

			SetNewArea(zoomCollider);
			UpdateBounds();

			WorldSpaceCanvas.ResizeToPlayArea(fullCollider);
		}
	}
}
