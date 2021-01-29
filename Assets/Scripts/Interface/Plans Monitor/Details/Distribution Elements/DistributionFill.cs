using UnityEngine;
using UnityEngine.UI;

public class DistributionFill : MonoBehaviour {

	[SerializeField]
    private Image graphic = null;
	[SerializeField]
    private LayoutElement layout = null;

	public int TeamID
	{
		get;
		set;
	}

	public float CurrentValue
	{
		get;
		set;
	}

	public void SetFillRelativeSize(float relativeSize)
	{
		gameObject.SetActive(relativeSize > 1.0f);
		layout.flexibleWidth = relativeSize;
	}

	public void SetColor(Color color)
	{
		graphic.color = color;
	}

	public void SetVisible(bool visible)
	{
		graphic.enabled = visible;
	}
}