using System.Collections;
using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;

namespace MSP2050.Scripts
{
	public class WebViewWindow : MonoBehaviour
	{
		public GenericWindow window;
		public Browser browser;
		public GameObject editButton;
		string currentURL;
		bool editing;

		private void Start()
		{
			gameObject.SetActive(false);
		}

		public void CreateWebViewWindow(string URL)
		{
			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			window.contentLayout.preferredWidth = (Screen.width - 200f) / scale;
			window.contentLayout.preferredHeight = (Screen.height - 200f) / scale;
			editing = false;
			currentURL = URL;
			gameObject.SetActive(true);
			browser.Url = currentURL;
			editButton.SetActive(Main.IsDeveloper);
			StartCoroutine(RepositionOnFrameEnd(new Vector3(100f, -100f)));
		}
	
		IEnumerator RepositionOnFrameEnd(Vector3 position)
		{
			yield return new WaitForEndOfFrame();
			window.SetPosition(new Vector2(position.x, position.y));
		}

		public void Close()
		{
			gameObject.SetActive(false);
		}

		public void Refresh()
		{
			browser.Reload(true);
		}

		public void Edit()
		{
			if(editing)
				browser.Url = currentURL;
			else
				browser.Url = currentURL + "&action=edit";
			editing = !editing;
		}
	}
}
