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

	public void CreateWebViewWindow(string URL)
	{
		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		window.contentLayout.preferredWidth = (Screen.width - 200f) / scale;
		window.contentLayout.preferredHeight = (Screen.height - 200f) / scale;
		editing = false;
		currentURL = $"{URL}?region={Main.MspGlobalData.region}";
		gameObject.SetActive(true);
		browser.Url = currentURL;
		editButton.SetActive(Main.IsDeveloper);
		StartCoroutine(RepositionOnFrameEnd(new Vector3(100f, -100f)));
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
