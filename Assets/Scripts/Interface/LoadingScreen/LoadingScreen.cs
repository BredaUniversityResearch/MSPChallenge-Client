using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoadingScreen : MonoBehaviour
	{
		[SerializeField]
		private Text editionText = null;
		[SerializeField]
		private Text loadingPercentageText = null;
		[SerializeField]
		private Text loadingNameText = null;
		[SerializeField]
		private Image loadingScreenImage = null;
		[SerializeField]
		private GameObject loadingBar = null;
		private RectTransform loadingBarRect;

		public Image mspIcon;

		private float progress = 0; // 0 - 100
		private float increment = 0;
		private bool doneLoading = false;
		private bool startedLoading = false;
		private bool isLoading = false;
		private string loadingBarText = "";

		public void Awake()
		{
			loadingNameText.gameObject.SetActive(Main.IsDeveloper);
			loadingBarRect = loadingBar.GetComponent<RectTransform>();
			SetLoadingBarPercentage(0.0f);
		}

		protected void Start()
		{
			//IMPORTANT NOTE to self, this is only true when project is run through LoginScene
			if (SessionManager.Instance.MspGlobalData != null)
			{
				//MSP Icon Swap
				RegionInfo region = InterfaceCanvas.Instance.regionSettings.GetRegionInfo(SessionManager.Instance.MspGlobalData.region);
				mspIcon.sprite = region.sprite;
				editionText.text = region.editionPostFix;
			}
		}

		protected void Update()
		{
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
			loadingBarRect.anchorMax = new Vector2(percentage / 100.0f, loadingBarRect.anchorMax.y);
			loadingBarRect.offsetMax = new Vector2(1f, 0);
			loadingBarRect.offsetMin = new Vector2(1f, 0);
		}

		public void CreateLoadingBar(int amountOfThingsToLoad, string loadingItemName)
		{
			increment = 100.0f / (float)amountOfThingsToLoad;
			//inputBlocker.enabled = true;
			startedLoading = true;
			isLoading = true;
			loadingBarText = loadingItemName;
			ShowHideLoadScreen(true);
		}

		public void OnFinishedLoading()
		{
			CameraManager.Instance.cameraZoom.ZoomOrthoCamera(CameraManager.Instance.gameCamera.ScreenToWorldPoint(Input.mousePosition), CameraManager.Instance.gameCamera.orthographicSize * 0.01f);
			//inputBlocker.enabled = false;
			doneLoading = true;
		}

		public void SetNextLoadingItem(string loadingItemName)
		{
			loadingBarText = loadingItemName;
			progress += increment;
		}

		private void UpdateLoadingScreenText()
		{
			loadingPercentageText.text = "Loading: " + progress.ToString("n0") + "%";
			loadingNameText.text = "Now Loading: " + loadingBarText;
		}
	}
}
