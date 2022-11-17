using TMPro;

namespace MSP2050.Scripts
{
	public class DistributionGroupShipping: DistributionGroup<DistributionItem>
	{
		void Start()
		{
			Initialise();
		}

		public void SetTitle(string layerName, string entityTypeName)
		{
			title.text = $"{layerName} ({entityTypeName})";
		}

		/// <summary>
		/// Update the slider distribution for the given item
		/// </summary>
		public override void UpdateDistributionItem(DistributionItem updatedItem, float currentValue)
		{
			//U wot.
			updatedItem.SetValue(currentValue);
			updatedItem.ToText(updatedItem.GetDistributionValue(), PolicyLogicShipping.shippingDisplayScale, true);

			if (distributionFillBar != null)
			{
				distributionFillBar.SetFill(updatedItem.Country, updatedItem.GetDistributionValue());
			}

			CheckIfChanged();
		}
	}
}

