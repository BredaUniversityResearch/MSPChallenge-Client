using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public abstract class AbstractDistributionGroup : MonoBehaviour
{
	[HideInInspector] public Distribution parent;

	public abstract void UpdateEntireDistribution();
	public abstract void UpdateDistributionItem(DistributionItem updatedItem, float currentValue);

	public abstract void ApplySliderValues(Plan plan, int index);
	public abstract void SetSliderValues(Dictionary<int, float> planDeltaValues, Dictionary<int, float> initialValues);
	public abstract void SetSliderValues(EnergyGrid grid, EnergyGrid.GridPlanState state);
	public abstract void SetSliderInteractability(bool value);
	public abstract void SetName(string name);
}

