using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KPICountrySelection : MonoBehaviour
{
	[SerializeField]
	private Button button = null;
	[SerializeField]
	private Image image = null;
	[SerializeField]
	private Image selectedOverlay = null;

    public void SetSelected(bool isSelected)
	{
		selectedOverlay.gameObject.SetActive(isSelected);
	}

	public void SetTeamColor(Color teamColor)
	{
		image.color = teamColor;
	}

	public void SetOnClickHandler(UnityAction callback)
	{
		button.onClick.AddListener(callback);
	}
}
