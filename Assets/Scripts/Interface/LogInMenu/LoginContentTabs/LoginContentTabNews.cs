using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTabNews : LoginContentTab
	{
		private const string GetNewsURL = "https://community.mspchallenge.info/api.php?action=query&list=categorymembers&cmtitle=Category%3ANews&cmsort=timestamp&cmdir=desc&cmprop=title%7Ctimestamp%7Ctype&format=json";

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

		private List<LoginNewsEntry> m_newsEntries;

		protected override void Initialize()
		{
			base.Initialize();

			m_newsEntries = new List<LoginNewsEntry>();
			m_searchBar.onValueChanged.AddListener(OnSearchtextChanged);
			CreateEntries(null);
			//TODO: get news entries from web
			//ServerCommunication.DoExternalAPICall<NewsRequestData>(GetNewsURL, OnNewsPageQuerySuccess, OnNewsPageQueryFail);
		}

		void CreateEntries(LoginNewsData[] a_data)
		{
			if (a_data == null)
			{
				m_noNewsEntry.SetActive(true);
				m_latestNewsBar.SetActive(false);
			}
			else
			{
				m_noNewsEntry.SetActive(false);

				foreach (LoginNewsData entryData in a_data)
				{
					LoginNewsEntry newEntry = Instantiate(m_newsEntryPrefab, m_newsEntryParent).GetComponent<LoginNewsEntry>();
					newEntry.SetContent(entryData);
					m_newsEntries.Add(newEntry);
				}

				//TODO: Create latest news entries
			}
		}

		void OnSearchtextChanged(string a_newText)
		{
			foreach(LoginNewsEntry entry in m_newsEntries)
				entry.FilterForSearch(a_newText);
		}

		void OnNewsPageQuerySuccess(NewsRequestData a_data)
		{
			if (a_data != null && a_data.query != null && a_data.query.categorymembers != null)
			{
				foreach (NewsQueryEntry entry in a_data.query.categorymembers)
				{

				}
			}
		}

		void OnNewsPageQueryFail(ServerCommunication.ARequest a_request, string a_message)
		{ }

		void GetContentForPage(string a_title)
		{
			string URL = $"https://community.mspchallenge.info/api.php?action=parse&page={a_title}&format=json&prop=wikitext|images|links|externallinks";
		}
	}

	[Serializable]
	public class NewsRequestData
	{
		public NewsQueryData query;
	}

	[Serializable]
	public class NewsQueryData
	{
		public NewsQueryEntry[] categorymembers;
	}

	[Serializable]
	public class NewsQueryEntry
	{
		public string title;
		public string timestamp;
	}
}
