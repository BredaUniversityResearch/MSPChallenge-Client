using UnityEngine;

namespace MSP2050.Scripts
{
	public class BoxSelect : MonoBehaviour
	{
		private static BoxSelect instance;

		void Start()
		{
			instance = this;
			gameObject.SetActive(false);
		}

		public static void DrawBoxSelection(Vector3 from, Vector3 to)
		{
			instance.gameObject.SetActive(true);

			
			from = CameraManager.Instance.gameCamera.WorldToScreenPoint(from);
			to = CameraManager.Instance.gameCamera.WorldToScreenPoint(to);
			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;

			Vector3 min = Vector3.Min(from, to) - Vector3.one;
			Vector3 max = Vector3.Max(from, to) + Vector3.one;
			// (1 pixel offset because the box in the sprite has a 1 pixel offset from the sprite border)

			RectTransform rt = instance.GetComponent<RectTransform>();
			rt.anchoredPosition = min / scale;
			rt.sizeDelta = (max - min) / scale;
		}

		public static void HideBoxSelection()
		{
			instance.gameObject.SetActive(false);
		}
	}
}
