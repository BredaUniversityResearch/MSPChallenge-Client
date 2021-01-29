using UnityEngine.UI;
using TMPro;

public class DistributionGroupShipping: DistributionGroup<DistributionItem>
{
	public TextMeshProUGUI layerType;

    void Start()
    {
        Initialise();
    }

    public void SetTitle(string layerName, string entityTypeName)
	{
		if (title != null)
		{
			title.text = layerName;
		}

		if (layerType != null)
		{
			layerType.text = entityTypeName;
		}
	}

	/// <summary>
	/// Update the slider distribution for the given item
	/// </summary>
	public override void UpdateDistributionItem(DistributionItem updatedItem, float currentValue)
	{
		//U wot.
		updatedItem.SetValue(currentValue);
		updatedItem.ToText(updatedItem.GetDistributionValue(), PlanManager.shippingDisplayScale, true);

		if (distributionFillBar != null)
		{
			distributionFillBar.SetFill(updatedItem.Country, updatedItem.GetDistributionValue());
		}

		CheckIfChanged();
	}
}

