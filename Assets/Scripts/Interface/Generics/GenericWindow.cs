using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class GenericWindow : MonoBehaviour
	{
		const float BORDER_OFFSET = 16f;
		const float LEFT_OFFSET = 64f;

		public RectTransform windowTransform;
		public LayoutElement contentLayout;
		public TextMeshProUGUI title;
		public bool shouldCenter;
		public bool shouldMaximize;

		// Buttons
		public Button exitButton;
		public Button helpButton;

		public ResizeHandle resizeHandle;
		public DragHandle dragHandle;
		public List<IOnResizeHandler> secondaryResizeHandlers;

		[Header("Prefabs")]
		public GameObject modalBackgroundPrefab;
		public GameObject helpWindowPrefab;

		public delegate bool AttemptHideWindowDelegate();
		private AttemptHideWindowDelegate onAttemptHideWindowDelegate;
		public AttemptHideWindowDelegate OnAttemptHideWindow
		{
			get => onAttemptHideWindowDelegate;
			set
			{
				Debug.Assert(onAttemptHideWindowDelegate == null, "Cannot overwrite hide window delegate. When this happens we need to see if we can change this to an event");
				onAttemptHideWindowDelegate = value;
			}
		}

		private GameObject modalBackground;

		void Start()
		{
			if (resizeHandle != null)
			{
				resizeHandle.onHandleDragged = HandleResize;
			}
			if (dragHandle != null)
			{
				dragHandle.onHandleDragged = HandleDrag;
			}
			if (exitButton != null)
			{
				exitButton.onClick.AddListener(Hide);
			}
			if (helpButton != null)
            {
				helpButton.onClick.AddListener(HelpWindow);
            }
		}

		void OnEnable()
		{
			if (shouldCenter)
			{
				CenterWindow();
			}
			if(shouldMaximize)
			{
				Vector3[] corners = new Vector3[4];
				Vector3[] contentCorners = new Vector3[4];
				float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
				windowTransform.GetWorldCorners(corners);
				contentLayout.GetComponent<RectTransform>().GetWorldCorners(contentCorners);
				float containerSizeVer = (corners[1].y - corners[0].y) - (contentCorners[1].y - contentCorners[0].y); 
				float containerSizeHor = (corners[2].x - corners[1].x) - (contentCorners[2].x - contentCorners[1].x); 
				contentLayout.preferredWidth = (Screen.width - containerSizeHor) / InterfaceCanvas.Instance.canvas.scaleFactor - LEFT_OFFSET - BORDER_OFFSET;
				contentLayout.preferredHeight = (Screen.height - containerSizeVer) / InterfaceCanvas.Instance.canvas.scaleFactor - 2 * BORDER_OFFSET;
			}
		}

		public void CenterWindow()
		{
			StartCoroutine(CenterNextFrame());
		}

		IEnumerator CenterNextFrame()
		{
			yield return null;
			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			Vector3[] corners = new Vector3[4];
			windowTransform.GetWorldCorners(corners);
			windowTransform.anchoredPosition = new Vector2(Mathf.Round(((corners[1].x - corners[2].x) / scale + (LEFT_OFFSET - BORDER_OFFSET))*0.5f),
				Mathf.Round((corners[1].y - corners[0].y) * 0.5f / scale));
		}
		
		public void SetTitle(string text) {
			title.text = text;
		}

		public void SetPosition(Vector2 pos) {
			windowTransform.anchoredPosition = pos;
			LimitPosition();
		}

		public void Hide() 
		{
			if (OnAttemptHideWindow == null || OnAttemptHideWindow())
			{
				gameObject.SetActive(false);
			}
		}

		public void HelpWindow()
		{
			HelpWindowsManager.Instance.InstantiateHelpWindow(helpWindowPrefab);
		}

		public void Hide(bool unityCallbackToggleValue)
		{
			if (!unityCallbackToggleValue)
			{
				Hide();
			}
		}

		/// <summary>
		/// Create a modal background that prevents interacting with other siblings
		/// </summary>
		public void CreateModalBackground() {

			// Instantiate prefab
			modalBackground = Instantiate(modalBackgroundPrefab);

			// Assign background parent
			modalBackground.transform.SetParent(InterfaceCanvas.Instance.transform, false);

			//// Set it to be behind the edit window
			modalBackground.transform.SetSiblingIndex(windowTransform.GetSiblingIndex());
		}

		/// <summary>
		/// Destroy modal window if present
		/// </summary>
		public void DestroyModalWindow() {
			if (modalBackground) {
				Destroy(modalBackground);
			} else {
				Debug.Log("No modalwindow to remove");
			}
		}

		/// <summary>
		/// The center of the window
		/// </summary>
		public Vector2 centered
		{
			get {
				float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
				Vector3[] corners = new Vector3[4];
				windowTransform.GetWorldCorners(corners);
				return new Vector2((corners[1].x - corners[2].x) * 0.5f * (1f / scale), (corners[1].y - corners[0].y) * 0.5f * (1f / scale));
			}
		}

		public void HandleResize(PointerEventData data, RectTransform handleRect, ResizeHandle.RescaleDirectionHor hor, ResizeHandle.RescaleDirectionVer ver)
		{
			Vector3[] corners = new Vector3[4];
			Vector3[] contentCorners = new Vector3[4];
			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			windowTransform.GetWorldCorners(corners);
			contentLayout.GetComponent<RectTransform>().GetWorldCorners(contentCorners);

			//Vertical
			if (ver == ResizeHandle.RescaleDirectionVer.Down)
			{
				float containerSize = (corners[1].y - corners[0].y) - (contentCorners[1].y - contentCorners[0].y); //Size of the area of the window not part of contentlayout
				float target = corners[1].y - (data.position.y /*- handleRect.sizeDelta.y * 0.5f*/) - containerSize;
				float max = corners[1].y - containerSize - BORDER_OFFSET;
				contentLayout.preferredHeight = Mathf.Round(Mathf.Max(0, Mathf.Min(target, max)) / scale);
			}
			else if (ver == ResizeHandle.RescaleDirectionVer.Up)
			{
				float containerSize = (corners[1].y - corners[0].y) - (contentCorners[1].y - contentCorners[0].y); //Size of the area of the window not part of contentlayout
				float target = data.position.y - corners[0].y - containerSize;
				float max = Screen.height - Mathf.Max(corners[0].y, BORDER_OFFSET) - containerSize - BORDER_OFFSET;
				contentLayout.preferredHeight = Mathf.Round(Mathf.Max(0, Mathf.Min(target, max)) / scale);
			}

			//Horizontal
			if (hor == ResizeHandle.RescaleDirectionHor.Right)
			{
				float containerSize = (corners[2].x - corners[1].x) - (contentCorners[2].x - contentCorners[1].x); //Size of the area of the window not part of contentlayout
				float target = data.position.x - corners[0].x - containerSize;
				float max = Screen.width - corners[0].x - containerSize - BORDER_OFFSET;
				contentLayout.preferredWidth = Mathf.Round( Mathf.Max(0, Mathf.Min(target, max)) / scale);
			}
			else if (hor == ResizeHandle.RescaleDirectionHor.Left)
			{
				float containerSize = (corners[2].x - corners[1].x) - (contentCorners[2].x - contentCorners[1].x); //Size of the area of the window not part of contentlayout
				float target =  corners[2].x - data.position.x - containerSize;
				float max = corners[1].x - containerSize - BORDER_OFFSET;
				contentLayout.preferredWidth = Mathf.Round(Mathf.Max(0, Mathf.Min(target, max)) / scale);
			}

			if (secondaryResizeHandlers != null)
				foreach (IOnResizeHandler handler in secondaryResizeHandlers)
					handler.OnResize();
		}

		public void HandleResolutionOrScaleChange(float oldScale ,bool updatePosition)
		{
			//Be aware that this function is only called on very specific instances of windows.

			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;

			Vector3[] corners = new Vector3[4];
			windowTransform.GetWorldCorners(corners);
			Vector3 bottomLeft = corners[0];
			Vector3 topRight = corners[2];

			float unscaledWidth = contentLayout.preferredWidth * oldScale;
			float unscaledHeight = contentLayout.preferredHeight * oldScale;	
			contentLayout.preferredWidth = Mathf.Round(Mathf.Min(unscaledWidth, topRight.x - bottomLeft.x) / scale);
			contentLayout.preferredHeight = Mathf.Round(Mathf.Min(unscaledHeight, topRight.y - bottomLeft.y) / scale);

			if (updatePosition)
			{
				//Force rebuild the layout so the position update will be correct.
				LayoutRebuilder.ForceRebuildLayoutImmediate(windowTransform);
				LimitPosition();
			}

			if (secondaryResizeHandlers != null)
				foreach (IOnResizeHandler handler in secondaryResizeHandlers)
					handler.OnResize();
		}

		public void HandleDrag(PointerEventData eventData, RectTransform handleRect)
		{
			transform.position += (Vector3)eventData.delta;
			LimitPosition();
		}

		public IEnumerator LimitPositionEndFrame()
		{
			yield return new WaitForEndOfFrame();
			LimitPosition();
		}

		public IEnumerator LimitSizeAndPositionEndFrame()
		{
			yield return new WaitForEndOfFrame();
			if (resizeHandle != null)
			{
				Vector3[] corners = new Vector3[4];
				Vector3[] contentCorners = new Vector3[4];
				float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
				windowTransform.GetWorldCorners(corners);
				contentLayout.GetComponent<RectTransform>().GetWorldCorners(contentCorners);			

				if (resizeHandle.rescaleDirectionHor != ResizeHandle.RescaleDirectionHor.None && windowTransform.rect.width * scale > Screen.width - (LEFT_OFFSET + BORDER_OFFSET)*scale)
				{
					float containerSizeHor = (corners[2].x - corners[1].x) - (contentCorners[2].x - contentCorners[1].x);
					contentLayout.preferredWidth = (Screen.width - containerSizeHor) / InterfaceCanvas.Instance.canvas.scaleFactor - LEFT_OFFSET - BORDER_OFFSET;
				}
				if (resizeHandle.rescaleDirectionVer != ResizeHandle.RescaleDirectionVer.None && windowTransform.rect.height * scale > Screen.height - 2 * BORDER_OFFSET * scale)
				{
					float containerSizeVer = (corners[1].y - corners[0].y) - (contentCorners[1].y - contentCorners[0].y);
					contentLayout.preferredHeight = (Screen.height - containerSizeVer) / InterfaceCanvas.Instance.canvas.scaleFactor - 2 * BORDER_OFFSET;
				}
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(windowTransform);
			LimitPosition();
		}

		public void LimitPosition()
		{
			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			transform.position = new Vector3(
				Mathf.Round(Mathf.Clamp(transform.position.x, LEFT_OFFSET * scale, Screen.width - ((windowTransform.rect.width + BORDER_OFFSET) * scale))),
				Mathf.Round(Mathf.Clamp(transform.position.y, ((windowTransform.rect.height + BORDER_OFFSET) * scale), Screen.height - (BORDER_OFFSET * scale))),
				transform.position.z);
		}

		public void RegisterResizeHandler(IOnResizeHandler handler)
		{
			if(secondaryResizeHandlers == null)
				secondaryResizeHandlers = new List<IOnResizeHandler>();
			secondaryResizeHandlers.Add(handler);
		}

		public void UnRegisterResizeHandler(IOnResizeHandler handler)
		{
			if (secondaryResizeHandlers != null)
				secondaryResizeHandlers.Remove(handler);
		}
	}
}