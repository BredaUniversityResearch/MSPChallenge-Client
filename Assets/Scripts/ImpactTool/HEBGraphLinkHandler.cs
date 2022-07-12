using System.IO;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class HEBGraphLinkHandler : MonoBehaviour
	{
		[SerializeField] HEBGraph.HEBGraph m_graph;

		private void Start()
		{
			m_graph.m_linkClickCallback += OnLinkClick;
		}

		void OnLinkClick(string a_link)
		{
			string url = Path.Combine(Main.MspGlobalData != null ? Main.MspGlobalData.wiki_base_url : "https://knowledge.mspchallenge.info/wiki/", a_link);
			InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(url);
		}
	}
}

