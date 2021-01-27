using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DoubleClickButton : CustomButton
{
	/* NOTE:
	 * Initially I rewrote this to either emit a click or double click event, but for the cases now this should it should be fine to
	 * emit both events sequentially regardless of whether or not we have a double click. Uncommenting all code and removing the onClick
	 * invocation in the OnPointerClick function should restore the functionality.
	 */

	//private const float doubleClickTimeInterval = 0.3f; // Grabbed from decompiling StandaloneInputModule clickCount max delay

	public ButtonClickedEvent onDoubleClick;

	//private float lastClickGameTime = -1.0f;

	public override void OnPointerClick(PointerEventData eventData)
	{
		//base.OnPointerClick(eventData);
		if (eventData.clickCount == 1)
		{
			onClick.Invoke();
			//lastClickGameTime = Time.realtimeSinceStartup;
		}
		else
		{
			onDoubleClick.Invoke();
			//lastClickGameTime = -1.0f;
		}
	}

	//private void Update()
	//{
	//	if (lastClickGameTime > 0.0f && (Time.realtimeSinceStartup - lastClickGameTime) > doubleClickTimeInterval)
	//	{
	//		onClick.Invoke();
	//		lastClickGameTime = -1.0f;
	//	}
	//}
}
