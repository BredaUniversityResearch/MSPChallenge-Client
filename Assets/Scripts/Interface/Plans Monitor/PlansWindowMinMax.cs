using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class PlansWindowMinMax : MonoBehaviour {

		public Toggle minMaxToggle;
		public GameObject timeline;
		public RectTransform rectTrans;
		public float maximizedLayoutSize, minimizedLayoutSize;
	
		private GenericWindow genericWindow;

		bool maximized = false;
		private bool ignoreToggleChange;

		void Awake()
		{
			genericWindow = GetComponent<GenericWindow>();
			minMaxToggle.onValueChanged.AddListener(OnToggleChange);
		}

		void OnToggleChange(bool value)
		{
			if (ignoreToggleChange)
				return;

			if(value)
				Maximize();
			else
				Minimize();
		}

		public void Minimize()
		{
			// Function
			timeline.gameObject.SetActive(false);
			PlanDetails.IsOpen = false;
			
			// Presentation
			genericWindow.contentLayout.preferredWidth = minimizedLayoutSize;
			maximized = false;

			ignoreToggleChange = true;
			minMaxToggle.isOn = false;
			ignoreToggleChange = false;
		}

		public void Maximize()
		{
			if (maximized)
				return;

			// Function
			timeline.gameObject.SetActive(true);
			PlanDetails.IsOpen = true;

			// Presentation
			genericWindow.contentLayout.preferredWidth = maximizedLayoutSize;
			StartCoroutine(LimitPositionEndFrame());
			maximized = true;

			ignoreToggleChange = true;
			minMaxToggle.isOn = true;
			ignoreToggleChange = false;
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
	}
}