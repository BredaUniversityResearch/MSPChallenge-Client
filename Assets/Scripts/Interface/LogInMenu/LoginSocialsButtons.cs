using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class LoginSocialsButtons : MonoBehaviour
	{
		private const string GetSocialsListURL = "https://community.mspchallenge.info/api.php?action=parse&page=Social_media_links&format=json&prop=externallinks|sections";

		[SerializeField] private Button m_youtubeButton;
		[SerializeField] private Button m_websiteButton;
		[SerializeField] private Button m_linkedinButton;
		[SerializeField] private Button m_twitterButton;

		void Start()
		{
			ServerCommunication.DoExternalAPICall<WikiSocialsListResult>(GetSocialsListURL, OnWikiSocialsQuerySuccess, OnWikiSocialsQueryFail);
			gameObject.SetActive(false);
		}

		void OnWikiSocialsQuerySuccess(WikiSocialsListResult a_data)
		{
			if (a_data != null && a_data.parse != null)
			{
				for (int i = 0; i < a_data.parse.sections.Length; i++)
				{
					string url = a_data.parse.externallinks[i];
					switch (a_data.parse.sections[i].line)
					{
						case "Website":
							m_websiteButton.onClick.AddListener(() => Application.OpenURL(url));
							break;
						case "Twitter":
							m_twitterButton.onClick.AddListener(() => Application.OpenURL(url));
							break;
						case "LinkedIN":
							m_linkedinButton.onClick.AddListener(() => Application.OpenURL(url));
							break;
						case "Youtube":
							m_youtubeButton.onClick.AddListener(() => Application.OpenURL(url));
							break;
					}
				}
			}
			gameObject.SetActive(true);
		}

		void OnWikiSocialsQueryFail(ServerCommunication.ARequest a_request, string a_message)
		{
			Debug.Log("Failed to get socials list");
		}
	}

	[Serializable]
	public class WikiSocialsListResult
	{
		public WikiSocialsListContent parse;
	}

	[Serializable]
	public class WikiSocialsListContent
	{
		public string[] externallinks;
		public WikiSocialsSection[] sections;
	}

	[Serializable]
	public class WikiSocialsSection
	{
		public string line;
	}
}
