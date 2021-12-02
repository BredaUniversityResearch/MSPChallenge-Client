using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;

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

	public void CreateWebViewWindow(string URL, Vector3 position, float contentWidth, float contentHeight)
	{
		window.contentLayout.preferredWidth = contentWidth;
		window.contentLayout.preferredHeight = contentHeight;
		CreateWebViewWindow(URL, position);
	}

	public void CreateWebViewWindow(string URL, Vector3 position)
	{
        editing = false;
        currentURL = URL;
        bool reposition = !gameObject.activeInHierarchy;
		gameObject.SetActive(true);
		browser.Url = currentURL;
        editButton.SetActive(Main.IsDeveloper);

        if (reposition)
		{
			StartCoroutine(RepositionOnFrameEnd(position));
		}
	}

	IEnumerator RepositionOnFrameEnd(Vector3 position)
	{
		yield return new WaitForEndOfFrame();

		Rect rect = window.windowTransform.rect;
		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		window.SetPosition(new Vector3(
			Mathf.Clamp(position.x / scale, 0f, (Screen.width - (rect.width * scale)) / scale),
			Mathf.Clamp(position.y / scale, (-Screen.height + (rect.height * scale)) / scale, 0f),
			position.z));
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
