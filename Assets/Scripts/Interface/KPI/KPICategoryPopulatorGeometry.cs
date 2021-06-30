using KPI;
using UnityEngine;

//Geometry KPI category populator specialisation. Will put the KPIBars parented to the configured subcategory, or to the default target container configured (Misc)
public class KPICategoryPopulatorGeometry: KPICategoryPopulator
{
	[SerializeField]
	private RectTransform energyCategoryContainer = null;

	[SerializeField]
	private RectTransform ecologyCategoryContainer = null;

	[SerializeField]
	private RectTransform shippingCategoryContainer = null;

	protected override RectTransform GetTargetContainerForCategory(KPICategory category)
	{
		RectTransform result = null;
		AbstractLayer layer = LayerManager.FindLayerByFilename(category.name);
		if (layer != null)
		{
			switch (layer.LayerKPICategory)
			{
			case ELayerKPICategory.Ecology:
				result = ecologyCategoryContainer;
				break;
			case ELayerKPICategory.Energy:
				result = energyCategoryContainer;
				break;
			case ELayerKPICategory.Shipping:
				result = shippingCategoryContainer;
				break;
			case ELayerKPICategory.Miscellaneous:
				result = base.GetTargetContainerForCategory(category);
				break;
			default:
				Debug.LogError("Unimplemented layer KPI category type " + layer.LayerKPICategory);
				goto case ELayerKPICategory.Miscellaneous;
			}

			return result;
		}
		else
		{
			Debug.LogError("Could not find layer for category with name " + category.name);
			result = base.GetTargetContainerForCategory(category);
		}

		return result;
	}
}
