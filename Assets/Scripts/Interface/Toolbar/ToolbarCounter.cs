using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ToolbarCounter : MonoBehaviour
{
	public Image counterImage;
	public TextMeshProUGUI counterText;

	private int counter = 0;

	public void AddValue()
	{
		UpdateCounter(counter + 1);
	}

	public void SetValue(int value)
	{
		UpdateCounter(value);
	}

	private void UpdateCounter(int i)
	{
		counter = i;
		if (i <= 0)
		{
			counterImage.gameObject.SetActive(false);
			counterText.text = i.ToString();
		}
		else
		{
			if (i > 9) i = 9;
			counterImage.gameObject.SetActive(true);
			counterText.text = i.ToString();
		}
	}
}
