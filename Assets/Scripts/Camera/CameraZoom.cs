using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace MSP2050.Scripts
{
	/// <summary>
	/// CameraZoom
	/// </summary>
	public class CameraZoom : MonoBehaviour
	{
		public static Vector3 m_lastZoomLocation = Vector3.zero;
		[FormerlySerializedAs("maximumZoomLevel")]
		public float m_maximumZoomLevel = 200.0f; //was 18, thats what she said

		[FormerlySerializedAs("borders")]
		[SerializeField]
		private BoxCollider2D m_borders = null;

		//Changing these doesn't change zoom level
		private float m_minZoom = 1.0f; 
		private float m_maxZoom = 60.0f;
		public float CurrentZoom { get; private set; } //0 = zoomed in, 1 = zoomed out

		[FormerlySerializedAs("speed")]
		[SerializeField]
		private float m_speed = 30.0f;

		[FormerlySerializedAs("cameraComponent")]
		[SerializeField]
		private Camera m_cameraComponent = null;
		private float m_previousCamAspect;

		private bool m_autoZoom;
		private float m_sourceZoom;
		private float m_targetZoom;
		private IEasingFunction m_autoZoomEase = new EaseOut(3.0f);
	
		private Coroutine m_rescaleCoroutine = null;

		public event System.Action OnScrollZoom;

		protected void Start()
		{
			UpdateBounds();
			m_autoZoom = false;
			m_maximumZoomLevel = float.Parse(SessionManager.Instance.MspGlobalData.maxzoom, Localisation.NumberFormatting);

		}

		public void SetNewArea(BoxCollider2D a_collider)
		{
			m_borders.size = new Vector2(a_collider.size.x, a_collider.size.y);
			m_borders.offset = new Vector2(a_collider.offset.x, a_collider.offset.y);
		}

		public void UpdateBounds()
		{
			float maxXZoom = (m_borders.size.x / 2) / m_cameraComponent.aspect;
			float maxYZoom = (m_borders.size.y / 2);
			m_maxZoom = Mathf.Min(maxXZoom, maxYZoom);
			m_minZoom = m_maxZoom / m_maximumZoomLevel;

			m_targetZoom = m_maxZoom;
			m_cameraComponent.orthographicSize = m_targetZoom;
			InterfaceCanvas.Instance.mapScale.SetScale(m_cameraComponent.orthographicSize);
		}

		public void ForceUpdateBoundsNextFrame()
		{
			m_previousCamAspect = 0.0f;
		}

		protected void Update()
		{
			if (m_cameraComponent.aspect != m_previousCamAspect)
			{
				UpdateBounds();

				m_previousCamAspect = m_cameraComponent.aspect;
			}

			if (EventSystem.current.IsPointerOverGameObject())
			{
				if (!CameraManager.Instance.canIZoom)
					return;
			}

			float wheel = Input.GetAxis("Mouse ScrollWheel");

			if (wheel == 0)
				return;
			ZoomOrthoCamera(m_cameraComponent.ScreenToWorldPoint(Input.mousePosition), (wheel * m_speed * m_cameraComponent.orthographicSize) * 0.01f);
			m_autoZoom = false;
			OnScrollZoom?.Invoke();
		}

		public void StartMaxZoomOut(float a_speed)
		{
			StopAllCoroutines();
			m_autoZoom = true;
			m_sourceZoom = m_cameraComponent.orthographicSize;
			m_targetZoom = m_maxZoom;
			StartCoroutine(AutoZoomCoroutine(a_speed));
		}

		public void StartAutoZoom(float a_targetZoomLevel, float a_speed)
		{
			StopAllCoroutines();
			m_autoZoom = true;
			m_sourceZoom = m_cameraComponent.orthographicSize;
			m_targetZoom = Mathf.Clamp(a_targetZoomLevel, m_minZoom, m_maxZoom);
			StartCoroutine(AutoZoomCoroutine(a_speed));
		}

		private void EndAutoZoom()
		{
			UpdateScaleNow(m_cameraComponent);
			m_autoZoom = false;
		}

		private IEnumerator AutoZoomCoroutine(float a_speed)
		{
			float time = 0;
			while (m_autoZoom)
			{
				if (time < 1)
				{
					m_cameraComponent.orthographicSize = Mathf.Lerp(m_sourceZoom, m_targetZoom, m_autoZoomEase.Evaluate(time));
					CurrentZoom = (m_cameraComponent.orthographicSize - m_minZoom) / (m_maxZoom - m_minZoom);
					time += Time.deltaTime / a_speed;
				}
				else
				{
					m_cameraComponent.orthographicSize = m_targetZoom;
					CurrentZoom = (m_cameraComponent.orthographicSize - m_minZoom) / (m_maxZoom - m_minZoom);
					time = 1;
					EndAutoZoom();
				}
				VisualizationUtil.Instance.UpdateDisplayScale();
				//Don't start the DelayedUpdateScale coroutine as this will never be triggered...
				UpdateUIScale();

				yield return null;
			}
			yield return null;
		}

		public void Clamp()
		{
			m_cameraComponent.orthographicSize = Mathf.Clamp(m_cameraComponent.orthographicSize, m_minZoom, m_maxZoom);
			CurrentZoom = (m_cameraComponent.orthographicSize - m_minZoom) / (m_maxZoom - m_minZoom);
		}

		// http://answers.unity3d.com/questions/384753/ortho-camera-zoom-to-mouse-point.html
		public void ZoomOrthoCamera(Vector3 a_zoomTowards, float a_amount)
		{
			m_lastZoomLocation = a_zoomTowards;
			float multiplier = (1.0f / m_cameraComponent.orthographicSize * a_amount);

			if (m_cameraComponent.orthographicSize > m_minZoom && m_cameraComponent.orthographicSize < m_maxZoom)
			{
				transform.position += (a_zoomTowards - transform.position) * multiplier;
			}

			m_cameraComponent.orthographicSize -= a_amount;

			Clamp();

			//VisualizationUtil.Instance.UpdateDisplayScale();
			
			//Removed for compatibility with C# 7.3
			//m_rescaleCoroutine ??= StartCoroutine(DelayedUpdateScale());
			
			if(m_rescaleCoroutine == null)
			{
				m_rescaleCoroutine = StartCoroutine(DelayedUpdateScale());
			}
			
			UpdateUIScale();
		}

		public void UpdateUIScale()
		{
			VisualizationUtil.Instance.UpdateDisplayScale();
			IssueManager.Instance.RescaleIssues();
			InterfaceCanvas.Instance.mapScale.SetScale(m_cameraComponent.orthographicSize);
			FSM.CameraZoomChanged();
		}

		private void UpdateScaleNow(Camera a_targetCamera)
		{
			VisualizationUtil.Instance.UpdateDisplayScale();
			LayerManager.Instance.UpdateLayerScales(a_targetCamera);
		}

		private IEnumerator DelayedUpdateScale()
		{
			yield return new WaitForSeconds(0.4f);
			UpdateScaleNow(m_cameraComponent);
			m_rescaleCoroutine = null;
			yield return null;
		}

		public float GetMaxZoom()
		{
			return m_maxZoom;
		}

		/// <summary>
		/// Set the zoom level, using the current zoom bounds
		/// Expects a 0-1 input value
		/// </summary>
		public void SetZoomLevel(float a_newZoomLevel)
		{
			m_cameraComponent.orthographicSize = (m_maxZoom - m_minZoom) * Mathf.Clamp01(a_newZoomLevel) + m_minZoom;
			Clamp();
			UpdateUIScale();
			UpdateScaleNow(m_cameraComponent);
		}
	}
}
