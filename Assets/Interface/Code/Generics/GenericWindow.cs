using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class GenericWindow : MonoBehaviour
{
    public RectTransform windowTransform;
	public LayoutElement contentLayout;
    public TextMeshProUGUI title;
    public Transform contentLocation;
    //public bool editEnabled;
    //public bool isPropertyWindow;
    public bool shouldCenter;

    // Buttons
    public Button exitButton;
    public Button cancelButton;
    public Button acceptButton;

	public ResizeHandle resizeHandle;
	public DragHandle dragHandle;
	public List<IOnResizeHandler> secondaryResizeHandlers;

	[Header("Prefabs")]
    public GameObject genericContentPrefab;
    public GameObject modalBackgroundPrefab;

    public delegate void CloseWindow();
    public CloseWindow CloseWindowDelegate = null;

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

	private List<GenericContent> genericContent = new List<GenericContent>();
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
	}

    void OnEnable()
    {
        if (shouldCenter)
        {
	        CenterWindow();
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
		//40f = topbar size
		SetPosition(new Vector2((corners[1].x - corners[2].x) * 0.5f * (1f / scale), (corners[1].y - corners[0].y - 40f) * 0.5f * (1f / scale)));
	}

    //protected void Update()
    //{
    //    if (isPropertyWindow && !EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonUp(0))
    //    {
    //        Destroy();
    //    }
    //}

    /// <summary>
    /// Set window title
    /// </summary>
    public void SetTitle(string text) {
        title.text = text;
    }

    /// <summary>
    /// Set window position
    /// </summary>
    public void SetPosition(Vector2 pos) {
        //LayoutRebuilder.ForceRebuildLayoutImmediate(windowTransform);
        windowTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// Get window position
    /// </summary>
    public Vector2 GetPosition()
    {
        return windowTransform.anchoredPosition;
    }

    /// <summary>
    /// Set window width
    /// </summary>
    public void SetWidth(float width) {
        windowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(width, 150f, 800f));
    }

    /// <summary>
    /// Get window width
    /// </summary>
    public Vector2 GetSize()
    {
        return windowTransform.sizeDelta;
    }

    /// <summary>
    /// Destroy this
    /// </summary>
    public void Destroy() {
        
        if (modalBackground) {
            Destroy(modalBackground.gameObject);
        }

        if (CloseWindowDelegate != null)
        {
            CloseWindowDelegate();
        }

        InterfaceCanvas.Instance.DestroyGenericWindow(this);
    }

    /// <summary>
    /// Remove from list and destroy a content window
    /// </summary>
    public void DestroyGenericContent(GenericContent content) {
        genericContent.Remove(content);
        Destroy(content.gameObject);
    }

    /// <summary>
    /// Hide the content
    /// </summary>
    public void Hide() 
	{
		if (OnAttemptHideWindow == null || OnAttemptHideWindow())
		{
			gameObject.SetActive(false);
		}
	}

	public void Hide(bool unityCallbackToggleValue)
	{
		if (!unityCallbackToggleValue)
		{
			Hide();
		}
	}

	/// <summary>
    /// Create a content window
    /// </summary>
    public GenericContent CreateContentWindow() {

        GenericContent content = GenerateContentWindow();

        return content;
    }

    ///// <summary>
    ///// Create a content window
    ///// </summary>
    ///// <param name="title">The title of the content window</param>
    //public GenericContent CreateContentWindow(string title) {

    //    GenericContent content = GenerateContentWindow();

    //    content.SetTitle(title);

    //    return content;
    //}

    ///// <summary>
    ///// Create a content window
    ///// </summary>
    ///// <param name="title">The title of the content window</param>
    ///// <param name="maxHeight">The maximum height of the window</param>
    //public GenericContent CreateContentWindow(string title, float maxHeight) {

    //    GenericContent content = GenerateContentWindow();

    //    content.SetTitle(title);
    //    content.SetPrefHeight(maxHeight);

    //    return content;
    //}

    ///// <summary>
    ///// Create a content window
    ///// </summary>
    ///// <param name="showTitle">Show the content title</param>
    //public GenericContent CreateContentWindow(bool showTitle) {

    //    GenericContent content = GenerateContentWindow();

    //    content.ShowTitle(showTitle);

    //    return content;
    //}

    ///// <summary>
    ///// Create a content window
    ///// </summary>
    ///// <param name="showTitle">Show the content title</param>
    ///// <param name="maxHeight">The maximum height of the window</param>
    //public GenericContent CreateContentWindow(bool showTitle, float maxHeight) {

    //    GenericContent content = GenerateContentWindow();

    //    content.ShowTitle(showTitle);
    //    content.SetPrefHeight(maxHeight);

    //    return content;
    //}

    /// <summary>
    /// Generates the content window
    /// </summary>
    private GenericContent GenerateContentWindow() {

        // Instantiate prefab
        GameObject go = Instantiate(genericContentPrefab);

        // Store component
        GenericContent content = go.GetComponent<GenericContent>();

        // Add to list
        genericContent.Add(go.GetComponent<GenericContent>());

        // Assign parent
        go.transform.SetParent(contentLocation, false);

        return content;
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
			//return Vector2.Scale(
			//	new Vector2((Screen.width / 2f) - ((windowTransform.rect.width * scale) / 2f), 
			//		(-Screen.height / 2f) + ((windowTransform.rect.height * scale) / 2f)), 
			//	new Vector2(1 / scale, 1 / scale));
			Vector3[] corners = new Vector3[4];
			windowTransform.GetWorldCorners(corners);
			return new Vector2((corners[1].x - corners[2].x) * 0.5f * (1f / scale), (corners[1].y - corners[0].y) * 0.5f * (1f / scale));
			//return new Vector2(-(windowTransform.rect.width) /2f, (windowTransform.rect.height) / 2f);
		}
    }

	public void HandleResize(PointerEventData data, RectTransform handleRect, ResizeHandle.RescaleDirection direction)
	{
		Vector3[] corners = new Vector3[4];
		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		windowTransform.GetWorldCorners(corners);

		//Vertical
		if (direction == ResizeHandle.RescaleDirection.Vertical || direction == ResizeHandle.RescaleDirection.Both)
		{
			float containerSize = (corners[1].y - corners[0].y) - contentLayout.preferredHeight * scale; //Size of the area of the window not part of contentlayout
			float target = corners[1].y - (data.position.y - handleRect.sizeDelta.y * 0.5f);
			float max = corners[1].y;
			//float max = corners[1].y  + Screen.height * 0.5f - containerSize;
			contentLayout.preferredHeight = Mathf.Max(0, Mathf.Min(target, max) - containerSize) / scale;
		}

		//Horizontal
		if (direction == ResizeHandle.RescaleDirection.Horizontal || direction == ResizeHandle.RescaleDirection.Both)
		{
			float containerSize = (corners[2].x - corners[1].x) - contentLayout.preferredWidth * scale; //Size of the area of the window not part of contentlayout
			float target = -(corners[1].x - (data.position.x /*- handleRect.sizeDelta.x * 0.5f*/));
			float max = Screen.width - corners[1].x;
			//float max = corners[1].x + Screen.width * 0.5f  - containerSize;
			contentLayout.preferredWidth = Mathf.Max(0, Mathf.Min(target, max) - containerSize) / scale;
		}

		if(secondaryResizeHandlers != null)
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
		contentLayout.preferredWidth = Mathf.Min(unscaledWidth, topRight.x - bottomLeft.x) / scale;
		contentLayout.preferredHeight = Mathf.Min(unscaledHeight, topRight.y - bottomLeft.y) / scale;

		if (updatePosition)
		{
			//Force rebuild the layout so the position update will be correct.
			LayoutRebuilder.ForceRebuildLayoutImmediate(windowTransform);

			transform.position = new Vector3(
				Mathf.Clamp(transform.position.x, 0f, Screen.width - (windowTransform.rect.width * scale)),
				Mathf.Clamp(transform.position.y, (windowTransform.rect.height * scale), Screen.height - (41f * scale)),
				transform.position.z);
		}

		if (secondaryResizeHandlers != null)
			foreach (IOnResizeHandler handler in secondaryResizeHandlers)
				handler.OnResize();
	}

	public void SetMinWindowWidth(float width)
	{
		contentLayout.minWidth = width;
	}

	public void HandleDrag(PointerEventData eventData, RectTransform handleRect)
	{
		transform.position += (Vector3)eventData.delta;

		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		transform.position = new Vector3(
			Mathf.Clamp(transform.position.x, 0f, Screen.width - (windowTransform.rect.width * scale)),
			Mathf.Clamp(transform.position.y, (windowTransform.rect.height * scale), Screen.height - (41f * scale)),
			transform.position.z);
	}

    public IEnumerator LimitPositionEndFrame()
    {
        yield return new WaitForEndOfFrame();
        LimitPosition();
    }

    public void LimitPosition()
    {
        float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, 0f, Screen.width - (windowTransform.rect.width * scale)),
            Mathf.Clamp(transform.position.y, (windowTransform.rect.height * scale), Screen.height - (41f * scale)),
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