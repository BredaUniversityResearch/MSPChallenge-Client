using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace MSP2050.Scripts
{
	public class LoginNewsEntry : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI m_titleText;
		[SerializeField] private TextMeshProUGUI m_contentText;
		[SerializeField] private TextMeshProUGUI m_readMoreText;
		[SerializeField] private RawImage m_contentImage;
		[SerializeField] private CustomButton m_readMoreButton;
		[SerializeField] private float m_imageMarginRight;
		[SerializeField] private float m_noImageMarginRight;

		private string m_moreInfoLink;

		void Start()
		{
			m_readMoreButton.onClick.RemoveAllListeners();
			m_readMoreButton.onClick.AddListener(()=> Application.OpenURL(m_moreInfoLink));
		}

		public void SetContent(LoginNewsData a_data)
		{
			m_titleText.text = a_data.date.ToString("dd/MM/yy") + " " + a_data.title;
			m_contentText.text = a_data.content;
			m_moreInfoLink = a_data.more_info_link;
			m_contentImage.gameObject.SetActive(false);
			m_contentText.margin = new Vector4(m_contentText.margin.x, m_contentText.margin.y, m_noImageMarginRight, m_contentText.margin.w);
			m_readMoreText.margin = new Vector4(m_readMoreText.margin.x, m_readMoreText.margin.y, m_noImageMarginRight, m_readMoreText.margin.w);
			//if (!string.IsNullOrEmpty(a_data.image_link))
			//	StartCoroutine(DownloadImage(a_data.image_link));

			m_readMoreButton.onClick.RemoveAllListeners();
			m_readMoreButton.onClick.AddListener(() => Application.OpenURL(m_moreInfoLink));
		}

		public void SetImage(Texture2D a_texture)
		{
			if (a_texture == null)
				return;
			m_contentImage.texture = a_texture;
			m_contentImage.gameObject.SetActive(true);
			m_contentImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)a_texture.width / (float)a_texture.height;
			m_contentText.margin = new Vector4(m_contentText.margin.x, m_contentText.margin.y, m_imageMarginRight, m_contentText.margin.w);
			m_readMoreText.margin = new Vector4(m_readMoreText.margin.x, m_readMoreText.margin.y, m_imageMarginRight, m_readMoreText.margin.w);
		}

		//public IEnumerator DownloadImage(string a_url)
		//{
		//	UnityWebRequest request = UnityWebRequestTexture.GetTexture(a_url);
		//	yield return request.SendWebRequest();
		//	if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
		//		Debug.LogWarning(request.error);
		//	else
		//	{
		//		Texture2D texture = ((DownloadHandlerTexture) request.downloadHandler).texture;
		//		m_contentImage.texture = texture;
		//		m_contentImage.gameObject.SetActive(true);
		//		m_contentImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / (float)texture.height;
		//		m_contentText.margin = new Vector4(m_contentText.margin.x, m_contentText.margin.y, m_imageMarginRight, m_contentText.margin.w);
		//	}
		//}

		//void ReadMorePressed()
		//{
		//	Application.OpenURL(m_moreInfoLink);
		//}

		public void FilterForSearch(string a_search)
		{
			if (string.IsNullOrEmpty(a_search))
			{
				gameObject.SetActive(true);
				return;
			}

			gameObject.SetActive(m_titleText.text.IndexOf(a_search, StringComparison.OrdinalIgnoreCase) >= 0
			                     || m_contentText.text.IndexOf(a_search, StringComparison.OrdinalIgnoreCase) >= 0);
		}
	}

	public class LoginNewsData
	{
		public string title;
		public DateTime date;
		public string content;
		public string image_link;
		public string more_info_link;
	}
}