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
		[SerializeField] private RawImage m_contentImage;
		[SerializeField] private CustomButton m_readMoreButton;
		[SerializeField] private float m_imageMarginRight;
		[SerializeField] private float m_noImageMarginRight;

		private string m_moreInfoLink;

		void Start()
		{
			m_readMoreButton.onClick.AddListener(ReadMorePressed);
		}

		public void SetContent(LoginNewsData a_data)
		{
			m_titleText.text = a_data.date + " " + a_data.title;
			m_contentText.text = a_data.content;
			m_moreInfoLink = a_data.more_info_link;
			m_contentImage.gameObject.SetActive(false);
			m_contentText.margin = new Vector4(m_contentText.margin.x, m_contentText.margin.y, m_noImageMarginRight, m_contentText.margin.w);
			if (!string.IsNullOrEmpty(a_data.image_link))
				StartCoroutine(DownloadImage(a_data.image_link));
		}

		IEnumerator DownloadImage(string a_url)
		{
			UnityWebRequest request = UnityWebRequestTexture.GetTexture(a_url);
			yield return request.SendWebRequest();
			if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
				Debug.LogWarning(request.error);
			else
			{
				Texture2D texture = ((DownloadHandlerTexture) request.downloadHandler).texture;
				m_contentImage.texture = texture;
				m_contentImage.gameObject.SetActive(true);
				m_contentImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / (float)texture.height;
				m_contentText.margin = new Vector4(m_contentText.margin.x, m_contentText.margin.y, m_imageMarginRight, m_contentText.margin.w);
			}
		}

		void ReadMorePressed()
		{
			Application.OpenURL(m_moreInfoLink);
		}
	}

	public class LoginNewsData
	{
		public string title;
		public string date;
		public string content;
		public string image_link;
		public string more_info_link;
	}
}