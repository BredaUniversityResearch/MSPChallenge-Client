using System.Collections;
using UnityEngine;
using UnityEngine.UI;

class KPIGraphDateMarker: MonoBehaviour, IOnResizeHandler
{
	[SerializeField]
	private RectTransform targetTransform = null;

	[SerializeField]
	private bool shouldFollowCurrentDate = true;

	[SerializeField]
	private RawImage rawImage= null;

	[SerializeField]
	private float imageRepetitions = 100f;

	[SerializeField]
	private GenericWindow matchingWindow;

	private void Start()
	{
		if (shouldFollowCurrentDate)
		{
			GameState.OnCurrentMonthChanged += OnMonthChanged;
		}
        SetDate(GameState.GetCurrentMonth());

		if(matchingWindow != null)
			matchingWindow.RegisterResizeHandler(this);
		StartCoroutine(DelayedResize());
	}

	IEnumerator DelayedResize()
	{
		yield return new WaitForEndOfFrame();
		OnResize();
	}

	private void OnDestroy()
	{
		if (shouldFollowCurrentDate)
		{
			GameState.OnCurrentMonthChanged -= OnMonthChanged;
		}
		if (matchingWindow != null)
			matchingWindow.UnRegisterResizeHandler(this);
	}

	private void OnMonthChanged(int oldCurrentMonth, int newCurrentMonth)
	{
		SetDate(newCurrentMonth);
	}

	//Also called via the UnityEditor
	public void SetDate(int month)
	{
		if (Main.MspGlobalData != null)
		{
			float timePercentage = month / (float)Main.MspGlobalData.session_end_month;

			targetTransform.anchorMin = new Vector2(timePercentage, 0f);
			targetTransform.anchorMax = new Vector2(timePercentage, 1f);
		}
	}

	public void OnResize()
	{
		Vector3[] corners = new Vector3[4];
		targetTransform.GetWorldCorners(corners);
		//rawImage.uvRect.height = (corners[1].y - corners[0].y) / imageRepetitions;
		rawImage.uvRect = new Rect(0, 0, 1f, (corners[1].y - corners[0].y) / imageRepetitions);
	}
}
