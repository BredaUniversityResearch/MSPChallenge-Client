using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class LoadingScreen : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI loadingPercentageText = null;
		[SerializeField] private TextMeshProUGUI loadingNameText = null;

		[SerializeField] private RectTransform m_bgRect;
		[SerializeField] private RectTransform m_mask;
		[SerializeField] private RectTransform m_maskedImage;

		//public Image mspIcon;
		private bool m_initialized;
		private float progress = 0; // 0 - 100
		private float increment = 0;
		private bool doneLoading = false;
		private bool startedLoading = false;
		private bool isLoading = false;
		private string loadingBarText = "";

		public void Awake()
		{
			SetLoadingBarPercentage(0.0f);
		}

		//protected void Start()
		//{
		//	//IMPORTANT NOTE to self, this is only true when project is run through LoginScene
		//	if (SessionManager.Instance.MspGlobalData != null)
		//	{
		//		//MSP Icon Swap
		//		RegionInfo region = InterfaceCanvas.Instance.regionSettings.GetRegionInfo(SessionManager.Instance.MspGlobalData.region);
		//		//mspIcon.sprite = region.sprite;
		//		//editionText.text = region.editionPostFix;
		//	}
		//}

		protected void Update()
		{
			if (!m_initialized)
			{
				m_initialized = true;
				UpdateImageSizes(); //Has to be done in update so canvas has initialized
			}

			SetLoadingBarPercentage(progress);
			if (doneLoading)
			{
				ShowHideLoadScreen(false);
				doneLoading = false;
				isLoading = false;
			}
			if (startedLoading)
			{
				ShowHideLoadScreen(true);
				startedLoading = false;
			}
			if (isLoading)
			{
				UpdateLoadingScreenText();
			}
		}

		public void ShowHideLoadScreen(bool aVisible)
		{
			gameObject.SetActive(aVisible);
		}

		public void SetLoadingBarPercentage(float percentage)
		{
			float t = percentage / 200f;
			m_mask.anchorMin = new Vector2(0.5f - t, 0f);
			m_mask.anchorMax = new Vector2(0.5f + t, 1f);
		}

		public void CreateLoadingBar(int amountOfThingsToLoad, string loadingItemName)
		{
			increment = 100.0f / (float)amountOfThingsToLoad;
			startedLoading = true;
			isLoading = true;
			loadingBarText = loadingItemName;
			ShowHideLoadScreen(true);
		}

		public void OnFinishedLoading()
		{
			CameraManager.Instance.cameraZoom.ZoomOrthoCamera(CameraManager.Instance.gameCamera.ScreenToWorldPoint(Input.mousePosition), CameraManager.Instance.gameCamera.orthographicSize * 0.01f);
			doneLoading = true;
		}

		public void SetNextLoadingItem(string loadingItemName)
		{
			loadingBarText = loadingItemName;
			progress += increment;
		}

		private void UpdateLoadingScreenText()
		{
			loadingPercentageText.text = progress.ToString("n0") + "%";
			loadingNameText.text = "Now loading: " + loadingBarText;
		}

		public void UpdateImageSizes()
		{
			Image bg = m_bgRect.GetComponent<Image>();
			RectTransform canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
			float bgAspect = (float)bg.sprite.texture.width / (float)bg.sprite.texture.height;
			float canvasAspect = canvasRect.sizeDelta.x / canvasRect.sizeDelta.y;

			float height, width;
			if (bgAspect > canvasAspect)
			{
				//Match height
				height = canvasRect.sizeDelta.y;
				width = height * bgAspect;
			}
			else
			{
				//Match width
				width = canvasRect.sizeDelta.x;
				height = width / bgAspect;
			}

			m_bgRect.sizeDelta = new Vector2(width, height);
			m_maskedImage.sizeDelta = new Vector2(width, height);
		}
	}
}
