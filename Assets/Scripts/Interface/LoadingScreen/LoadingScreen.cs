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
		[SerializeField] private float m_loadingBarLerpSpeed = 0.1f;

		//public Image mspIcon;
		private bool m_initialized;
		private float progress = 0; // 0 - 100
		private float increment = 0;
		private bool startedLoading = false;
		private bool isLoading = false;
		private string loadingBarText = "";
		private float currentBarProgress = 0;

		public void Awake()
		{
			SetLoadingBarPercentage(0.0f);
		}

		protected void Update()
		{
			if (!m_initialized)
			{
				m_initialized = true;
				UpdateImageSizes(); //Has to be done in update so canvas has initialized
			}

			currentBarProgress += (progress - currentBarProgress) * m_loadingBarLerpSpeed;
			SetLoadingBarPercentage(currentBarProgress);
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
			Destroy(gameObject);
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
