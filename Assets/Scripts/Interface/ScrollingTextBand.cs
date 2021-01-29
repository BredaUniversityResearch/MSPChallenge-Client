using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollingTextBand : MonoBehaviour {

	public float scrollSpeed;
	public RectTransform textParent;
	bool scrolling;
	float childSize;
	[SerializeField] float currentOffset;

	private static ScrollingTextBand singleton;
	public static ScrollingTextBand instance
	{
		get
		{
			if (singleton == null)
			{
				singleton = FindObjectOfType<ScrollingTextBand>();
			}
			return singleton;
		}
	}

	void Start ()
	{
		singleton = this;
		childSize = textParent.GetChild(0).GetComponent<LayoutElement>().minWidth;
		SetEnabled(false);
	}

	void Update () {
		if (scrolling)
		{
			currentOffset += scrollSpeed * Time.deltaTime;
			if (currentOffset > childSize)
				currentOffset -= childSize;
			//group.padding.left += (int)currentOffset;
			textParent.anchoredPosition = new Vector2(-currentOffset, textParent.anchoredPosition.y);
		}
	}

	public bool Scrolling
	{
		set
		{
			currentOffset = 0;
			scrolling = value;
		}
		get { return scrolling; }
	}

	public void SetEnabled(bool value)
	{
		gameObject.SetActive(value);
		Scrolling = value;
	}
}
