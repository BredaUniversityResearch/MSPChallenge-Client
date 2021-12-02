using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenBorderGradient : MonoBehaviour
{
	private static ScreenBorderGradient singleton;

	public static ScreenBorderGradient instance
	{
		get
		{
			if (singleton == null)
			{
				singleton = FindObjectOfType<ScreenBorderGradient>();
			}
			return singleton;
		}
	}

	[SerializeField]
	private Image gradientImage = null;

	private void Start()
	{
		singleton = this;
		SetEnabled(false);
	}

	public void SetEnabled(bool enabledState)
	{
		gradientImage.enabled = enabledState;
	}
}