using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

class KPIDateSelector: MonoBehaviour
{
	[Serializable]
	private class DisplayDateChangedEvent : UnityEvent<int>
	{
	}

	[SerializeField]
	private TextMeshProUGUI dateText = null;

	private bool isUsingLatestMonth = true;
	private int displayingMonth = 0;

	[SerializeField]
	private DisplayDateChangedEvent onDisplayDateChanged = null;


    [SerializeField]
    private Button previousMonthButton = null;
    [SerializeField]
    private Button nextMonthButton = null;

    private void Start()
	{
		SetDisplayTime(0);
		if(nextMonthButton != null)
			nextMonthButton.onClick.AddListener(IncreaseDisplayTime);
		if (previousMonthButton != null)
			previousMonthButton.onClick.AddListener(DecreaseDisplayTime);
	}

	private void Update()
	{
		if (isUsingLatestMonth)
		{
			int currentGameMonth = GameState.GetCurrentMonth();
			if (currentGameMonth != displayingMonth)
			{
				ChangeDisplayTime(currentGameMonth - displayingMonth);
			}
		}
	}

	public void SetDisplayTime(int absoluteMonth)
	{
		ChangeDisplayTime(absoluteMonth - displayingMonth);
	}

	void IncreaseDisplayTime()
	{
		ChangeDisplayTime(1);
	}

	void DecreaseDisplayTime()
	{
		ChangeDisplayTime(-1);
	}

	public void ChangeDisplayTime(int relativeMonths)
	{
		int latestMonth = GameState.GetCurrentMonth();
		if (displayingMonth + relativeMonths >= latestMonth)
		{
			displayingMonth = latestMonth;
			isUsingLatestMonth = true;
		}
		else
		{
			if (displayingMonth + relativeMonths < 0)
			{
				displayingMonth = 0;
			}
			else
			{
				displayingMonth += relativeMonths;
			}

			isUsingLatestMonth = false;
		}

		OnDisplayedTimeChanged();
	}

	private void OnDisplayedTimeChanged()
	{
        int latestMonth = GameState.GetCurrentMonth();
        nextMonthButton.interactable = displayingMonth < latestMonth;
        previousMonthButton.interactable = displayingMonth > 0;

        dateText.text = Util.MonthToText(displayingMonth, true);

		if (onDisplayDateChanged != null)
		{
			onDisplayDateChanged.Invoke(displayingMonth);
		}
	}
}
