using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WikiLinkBrowserHandler : MonoBehaviour
{
	void Start()
	{
		CradleImpactTool.CradleGraphManager.OnWikiLinkClick += OnLinkClicked;
	}

	private void OnLinkClicked(string a_article)
	{
		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		string url = Main.MspGlobalData != null ? Main.MspGlobalData.wiki_base_url : "https://knowledge.mspchallenge.info/wiki/";
		url = Path.Combine(url, a_article);
		InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(url);
	}
}
