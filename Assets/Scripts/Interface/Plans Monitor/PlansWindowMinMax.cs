using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlansWindowMinMax : MonoBehaviour {

    public Button button;
	public GameObject timeline;
    public Image minIcon, maxIcon;
    //public LayoutGroup layoutPadding;
    public RectTransform rectTrans;
	//public LayoutElement windowLayoutElement;
	public float maximizedLayoutSize, minimizedLayoutSize;
	
	private GenericWindow genericWindow;

	bool maximized = false;

    void Awake()
	{
		genericWindow = GetComponent<GenericWindow>();
	}

	public void Minimize()
	{
		// Function
		timeline.gameObject.SetActive(false);
		PlanDetails.IsOpen = false;
        button.onClick.RemoveListener(Minimize);
        button.onClick.AddListener(Maximize);

        // Feedback
        minIcon.gameObject.SetActive(false);
        maxIcon.gameObject.SetActive(true);
		button.targetGraphic = maxIcon;
		//windowLayoutElement.preferredWidth = minimizedLayoutSize / InterfaceCanvas.instance.canvas.scaleFactor;
		//genericWindow.SetMinWindowWidth(minimizedLayoutSize);
		genericWindow.contentLayout.preferredWidth = minimizedLayoutSize;

		// Presentation
		//UpdatePadding(true);
        maximized = false;
    }

    public void Maximize()
	{
        if (maximized)
            return;

		// Function
		timeline.gameObject.SetActive(true);
		PlanDetails.IsOpen = true;
        button.onClick.RemoveListener(Maximize);
        button.onClick.AddListener(Minimize);

        // Feedback
        minIcon.gameObject.SetActive(true);
        maxIcon.gameObject.SetActive(false);
        button.targetGraphic = minIcon;
		//windowLayoutElement.preferredWidth = maximizedLayoutSize / InterfaceCanvas.instance.canvas.scaleFactor;
		//genericWindow.SetMinWindowWidth(maximizedLayoutSize);
		genericWindow.contentLayout.preferredWidth = maximizedLayoutSize;

		// Presentation
		//UpdatePadding(false);
		StartCoroutine(LimitPositionEndFrame());
		maximized = true;
    }

	IEnumerator LimitPositionEndFrame()
	{
		yield return new WaitForEndOfFrame();
		LimitPosition();
	}

    public void LimitPosition()
    {
        transform.position = new Vector3(
			Mathf.Clamp(transform.position.x, 0f, Screen.width - (rectTrans.rect.width * InterfaceCanvas.Instance.canvas.scaleFactor)),
			transform.position.y,
            transform.position.z);      
    }

	//public void HandleVerticalResize(PointerEventData data, ResizeHandle.RescaleDirection direction)
	//{
	//	Vector3[] corners = new Vector3[4];
	//	float scale = InterfaceCanvas.instance.canvas.scaleFactor;
	//	rect.GetWorldCorners(corners);

	//	float target = corners[1].y - (data.position.y - resizeRect.sizeDelta.y * 0.5f);
	//	float min = 250f / scale;
	//	float max = corners[1].y;
	//	windowLayoutElement.preferredHeight = Mathf.Clamp(target, min, max) / InterfaceCanvas.instance.canvas.scaleFactor;
	//}

	//public void HandleResolutionOrScaleChange()
	//{
	//	//Vector3[] corners = new Vector3[4];
	//	//rect.GetWorldCorners(corners);
	//	//float target = (rect.GetChild(0) as RectTransform).rect.height;
	//	//float max = corners[1].y;
	//	//layoutElement.preferredHeight = Mathf.Min(target, max) / InterfaceCanvas.instance.canvas.scaleFactor;

	//	float scale = InterfaceCanvas.instance.canvas.scaleFactor;
	//	windowLayoutElement.preferredHeight = 250f / scale;
	//	windowLayoutElement.preferredWidth = Mathf.Min((maximized ? maximizedLayoutSize : minimizedLayoutSize), Screen.width) / scale;
	//	transform.position = new Vector3(
	//		Mathf.Clamp(transform.position.x, 0f, Screen.width - (rect.rect.width * scale)),
	//		Mathf.Clamp(transform.position.y, (rect.rect.height * scale), Screen.height - (41f * scale)),
	//		transform.position.z);
	//}
}