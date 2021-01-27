using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

/// <summary>
/// This class manages the items layed out vertically for energy and ecology
/// GroupDistribution is used to manage the distribution fills that shows the distribution ratio
/// EnergySources is energy specific and shows how much energy is being generated from within the grid per country
/// DistributionItem are the items layed out vertically with the exception of PlanWizardEnergySources
/// A DistributionItem can have a regular slider (0 - 1) or a centered slider (-1 - 1) depending on what reference is set in the inspector
/// </summary>
public class DistributionGroup<T> : AbstractDistributionGroup where T : DistributionItem
{
	[Header("General")]
	// UI
	public TextMeshProUGUI title;
	public RectTransform foldTrans;
	public Image foldGraphic, changeIndicator;
    public Button barButton;

	// Child content
	public GameObject childrenAndLegend;
	public Transform distributionItemLocation;

	// References
	public DistributionFillBar distributionFillBar;

	[Header("Prefabs")]
	public GameObject itemPrefab;
	//public Transform legendObject;

	//Private/Protected
	protected bool changed;
	protected Color outlineCol;
	protected bool interactable = false;
	protected List<T> items = new List<T>();
    private bool initialised = false;

	public virtual void Initialise()
    {
        if (initialised)
            return;
        initialised = true;
        barButton.onClick.AddListener(BarPressed);
    }

	public DistributionItem FindDistributionItemByCountryId(int countryId)
	{
		return items.Find(obj => obj.Country == countryId);
	}

	public override void ApplySliderValues(Plan plan, int index)
	{ Debug.LogError("ApplySliderValues with called on a group that doesn't implement the method."); }
	public override void SetSliderValues(Dictionary<int, float> planDeltaValues, Dictionary<int, float> initialValues)
	{ Debug.LogError("SetSliderValues with ecology parameters called on a group that doesn't implement the method."); }
	public override void SetSliderValues(EnergyGrid grid, EnergyGrid.GridPlanState state)
	{ Debug.LogError("SetSliderValues with energy parameters called on a group that doesn't implement the method."); }
    
	public override void SetName(string name)
	{
		title.text = name;
	}

	/// <summary>
	/// Create an item that can hold a (centered) slider
	/// </summary>
	public T CreateItem(int country, float maxValue)
	{
        // Generate item
        T item = CreateItem(country);
        item.SetMaximum(maxValue);
		item.SetSliderInteractability(interactable);
		return item;
	}

    /// <summary>
    /// Create an item that can hold a (centered) slider
    /// </summary>
    public T CreateItem(int country)
    {
        // Generate item
        Color col = TeamManager.GetTeamByTeamID(country).color;
        GameObject go = Instantiate(itemPrefab);
        T item = go.GetComponent<T>();
        items.Add(item);
        go.transform.SetParent(distributionItemLocation, false);

        // Set values
        item.group = this;
        item.Country = country;
        item.graphic.color = col;
		item.SetSliderInteractability(interactable);

		foldGraphic.enabled = true;

        return item;
    }

    /// <summary>
    /// Update the slider distribution for the given item
    /// </summary>
    public override void UpdateDistributionItem(DistributionItem updatedItem, float currentValue)
	{
		//U wot.
		updatedItem.SetValue(currentValue);
		updatedItem.ToText(updatedItem.GetDistributionValue(), PlanManager.fishingDisplayScale, true);

		if ( distributionFillBar != null)
		{
			distributionFillBar.SetFill(updatedItem.Country, updatedItem.GetDistributionValue());
		}

		CheckIfChanged();
	}

	/// <summary>
	/// Update the entire slider distribution
	/// </summary>
	public override void UpdateEntireDistribution()
	{ }

	/// <summary>
	/// Sorts the (vertical) items based on color
	/// </summary>
	public virtual void SortItems()
	{
		// Sort by color
		items.Sort((lhs, rhs) => lhs.Country.CompareTo(rhs.Country));

		for (int i = 0; i < items.Count; ++i)
		{
			items[i].transform.SetSiblingIndex(i);
		}
	}

	protected virtual bool CheckIfChanged()
	{
		changed = false;
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].changed)
			{
				changed = true;
				changeIndicator.DOFade(1f, 0.2f);
				break;
			}
		}

		if (distributionFillBar != null)
		{
			distributionFillBar.outline.color = changed ? Color.white : outlineCol;
		}

		if (!changed)
			changeIndicator.DOFade(0f, 0.2f);
		return changed;
	}

	public override void SetSliderInteractability(bool value)
	{
		interactable = value;
		foreach (DistributionItem item in items)
			item.SetSliderInteractability(value);
	}

	public void BarPressed()
	{
		if (foldGraphic.enabled)
		{
			bool newExpanded = !childrenAndLegend.activeSelf;
			childrenAndLegend.SetActive(newExpanded);
			//if (legendObject != null)
			//	legendObject.gameObject.SetActive(childrenAndLegend.activeSelf);

			Vector3 rot = foldTrans.eulerAngles;
			foldTrans.eulerAngles = newExpanded ? new Vector3(rot.x, rot.y, 0f) : new Vector3(rot.x, rot.y, 90f);
		}
	}
}
