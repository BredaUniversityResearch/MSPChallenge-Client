using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTabNews : LoginContentTab
	{
		private const string GetNewsListURL = "https://community.mspchallenge.info/api.php?action=query&list=categorymembers&cmtitle=Category%3ANews&cmsort=timestamp&cmdir=desc&cmprop=title%7Ctimestamp%7Ctype&format=json";

		[Header("Latest news")]
		[SerializeField] private GameObject m_latestNewsBar;
		[SerializeField] private Transform m_latestNewsEntryParent;
		[SerializeField] private GameObject m_latestNewsEntryPrefab;
		[SerializeField] private GameObject m_latestNewsSpacerPrefab;
		[SerializeField] private Button m_latestNewsReadMoreButton;

		[Header("News tab")]
		[SerializeField] private Transform m_newsEntryParent;
		[SerializeField] private GameObject m_newsEntryPrefab;
		[SerializeField] private GameObject m_noNewsEntry;
		[SerializeField] private CustomInputField m_searchBar;
		[SerializeField] private Button m_clearSearchButton;

		private List<LoginNewsEntry> m_newsEntries;
		private int m_awaitingResults = 0;
		private int m_returnedResults = 0;
		List<LoginNewsData> m_newsData;

		protected override void Initialize()
		{
			base.Initialize();

			m_newsEntries = new List<LoginNewsEntry>();
			m_searchBar.onValueChanged.AddListener(OnSearchtextChanged);
			CreateEntries();
			ServerCommunication.Instance.DoExternalAPICall<WikiNewsListResult>(GetNewsListURL, OnNewsListQuerySuccess, OnNewsListQueryFail);
			m_latestNewsReadMoreButton.onClick.AddListener(OpenNewsTab);
			m_clearSearchButton.onClick.AddListener(ClearSearch);
		}

		void ClearSearch()
		{
			m_searchBar.text = "";
		}

		void CreateEntries()
		{
			if (m_newsData == null || m_newsData.Count == 0)
			{
				m_noNewsEntry.SetActive(true);
				m_latestNewsBar.SetActive(false);
			}
			else
			{
				m_latestNewsBar.SetActive(true);
				m_noNewsEntry.SetActive(false);
				m_newsData.Sort((a, b) => a.date.CompareTo(b.date));

				foreach (LoginNewsData entryData in m_newsData)
				{
					LoginNewsEntry newEntry = Instantiate(m_newsEntryPrefab, m_newsEntryParent).GetComponent<LoginNewsEntry>();
					newEntry.SetContent(entryData);
					if (!string.IsNullOrEmpty(entryData.image_link))
					{
						StartCoroutine(DownloadImage(entryData.image_link, newEntry));
					}
					m_newsEntries.Add(newEntry);
				}

				for (int i = 0; i < 3 && i < m_newsData.Count; i++)
				{
					if (i != 0)
						Instantiate(m_latestNewsSpacerPrefab, m_latestNewsEntryParent);
					GameObject newEntry = Instantiate(m_latestNewsEntryPrefab, m_latestNewsEntryParent);
					newEntry.GetComponent<TextMeshProUGUI>().text = m_newsData[i].title;
					newEntry.GetComponent<Button>().onClick.AddListener(OpenNewsTab);
				}
				m_latestNewsReadMoreButton.transform.SetAsLastSibling();
			}
		}

		IEnumerator DownloadImage(string a_url, LoginNewsEntry a_entry)
		{
			Debug.Log(a_url);
			//UnityWebRequest request = UnityWebRequestTexture.GetTexture(a_url);
			//yield return request.SendWebRequest();
			//if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
			//	Debug.LogWarning(request.error);
			//else
			//{
			//	a_entry.SetImage(((DownloadHandlerTexture)request.downloadHandler).texture);
			//}

			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(a_url))
			//using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("https://cdn.discordapp.com/attachments/358281323007311873/984766405813604352/Kevin.png"))
			{
				yield return uwr.SendWebRequest();

				if (uwr.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning(uwr.error);
				}
				else
				{
					a_entry.SetImage(DownloadHandlerTexture.GetContent(uwr));
				}
			}
		}

		void OpenNewsTab()
		{
			LoginManager.Instance.SetTabActive(LoginManager.ELoginMenuTab.News);
		}

		void OnSearchtextChanged(string a_newText)
		{
			foreach(LoginNewsEntry entry in m_newsEntries)
				entry.FilterForSearch(a_newText);
		}

		void OnNewsListQuerySuccess(WikiNewsListResult a_data)
		{
			if (a_data != null && a_data.query != null && a_data.query.categorymembers != null)
			{
				m_awaitingResults = a_data.query.categorymembers.Length;
				m_newsData = new List<LoginNewsData>();
				foreach (WikiNewsListQueryEntry entry in a_data.query.categorymembers)
				{
					GetContentForPage(entry.title, entry.timestamp);
				}
			}
		}

		void OnNewsListQueryFail(ARequest a_request, string a_message)
		{
			Debug.Log("Failed to get news list");
		}

		void GetContentForPage(string a_title, string a_date)
		{
			string URL = $"https://community.mspchallenge.info/api.php?action=parse&page={a_title}&format=json&prop=text|images";
			//string URL = $"https://community.mspchallenge.info/api.php?action=parse&page=How_to_add_geo-data_to_the_platform&format=json&prop=text|images";
			ServerCommunication.Instance.DoExternalAPICall<WikiNewsEntryResult>(URL, (r) => OnNewsEntryQuerySuccess(r, a_date), OnNewsEntryQueryFail);
		}

		void OnNewsEntryQuerySuccess(WikiNewsEntryResult a_data, string a_date)
		{
			m_newsData.Add(new LoginNewsData()
			{
				title = a_data.parse.title,
				date = DateTime.Parse(a_date, new DateTimeFormatInfo()),
				content = HtmlToRichText(a_data.parse.text.First.First.ToString()),
				more_info_link = $"https://community.mspchallenge.info/wiki/{a_data.parse.title}",
				image_link = a_data.parse.images != null && a_data.parse.images.Length > 0 ? $"https://community.mspchallenge.info/wiki/{a_data.parse.title}#/media/File:{a_data.parse.images[0]}" : null
			});

			m_returnedResults++;
			if(m_returnedResults == m_awaitingResults)
				CreateEntries();
		}

		void OnNewsEntryQueryFail(ARequest a_request, string a_message)
		{
			Debug.Log($"Failed to get news entry content. Message: {a_message}");
			m_returnedResults++;
			if (m_returnedResults == m_awaitingResults)
				CreateEntries();
		}

		private static string HtmlToRichText(string html)
		{
			const string stripFormatting = @"(?!<(\/){0,1}i>)(?!<(\/){0,1}b>)<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
			var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);

			var text = html;

			int cutoffIndex = html.IndexOf("<u>");
			if (cutoffIndex >= 0)
				text = text.Substring(0, cutoffIndex);

			int debugInfoIndex = text.IndexOf("\n<!--");
			if (debugInfoIndex >= 0)
				text = text.Substring(0, debugInfoIndex);

			text = text.Replace("<li>", "- ");
			text = stripFormattingRegex.Replace(text, string.Empty);
			text = System.Net.WebUtility.HtmlDecode(text);//Decode html specific characters

			return text;
		}
	}

	[Serializable]
	public class WikiNewsListResult
	{
		public WikiNewsListQueryData query;
	}

	[Serializable]
	public class WikiNewsListQueryData
	{
		public WikiNewsListQueryEntry[] categorymembers;
	}

	[Serializable]
	public class WikiNewsListQueryEntry
	{
		public string title;
		public string timestamp;
	}

	[Serializable]
	public class WikiNewsEntryResult
	{
		public WikiNewsEntryContent parse;
	}

	[Serializable]
	public class WikiNewsEntryContent
	{
		public string title;
		public string[] images;
		public JToken text;
	}
}
