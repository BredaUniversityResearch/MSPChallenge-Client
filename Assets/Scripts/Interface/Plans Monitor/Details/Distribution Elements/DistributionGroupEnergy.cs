using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using System;
using TMPro;

public class DistributionGroupEnergy : DistributionGroup<DistributionItemEnergy>
{
    [Header("Energy Specific")]
    //public DistributionEnergySources energySources;
    public Image stateIndicator;
    public Sprite addedSprite, removedSprite, changedSprite, greenEnergyIcon, greyEnergyIcon;
    public CustomInputField gridNameField;
    public Transform productionIconParent;
    public GameObject productionIconPrefab;
	public Image greenGreyEnergyImage;
    public TextMeshProUGUI totalPower;
    public CustomButton occulusButton;

	[SerializeField]
	private ValueConversionCollection valueConversionCollection = null;

	private Dictionary<int, GameObject> productionIcons;

	[HideInInspector]   public long sourcePower;
    [HideInInspector]   public EnergyGrid energyGrid;
    private EnergyGrid.GridPlanState originalGridState;
    private long socketMaximum;
	private HashSet<int> producingCountries;
    private bool ignoreDistributionUpdate = false;

    private void Start()
    {
        gridNameField.onValueChanged.AddListener((a) => CheckIfChanged());
		occulusButton.onClick.AddListener(() =>
		{
			InterfaceCanvas.Instance.plansMonitor.FadeAndHighlightUntilMouseMove();
			energyGrid.HighlightSockets();
			CameraManager.Instance.ZoomToBounds(energyGrid.GetGridRect());
		});
        Initialise();
    }

    public override void ApplySliderValues(Plan plan, int index)
    {
        foreach (DistributionItem item in items)
        {
            if (item.changed)
            {
                if (energyGrid.plan.ID != plan.ID) //Older distribution was changed: duplicate it to the new plan
                    energyGrid = plan.DuplicateEnergyGridToPlan(energyGrid);
                energyGrid.energyDistribution.distribution[item.Country].expected = item.GetDistributionValueLong();
            }
        }
        if (gridNameField.text != energyGrid.name)
        {
            if (energyGrid.plan.ID != plan.ID) //Older distribution was changed: duplicate it to the new plan
            {
                energyGrid = plan.DuplicateEnergyGridToPlan(energyGrid);
                energyGrid.name = gridNameField.text;
            }
            else
            {
                energyGrid.name = gridNameField.text;
                energyGrid.SetName(gridNameField.text);
            }
        }
    }

	public override void SetName(string name)
	{
		gridNameField.text = name;
	}

	public override void SetSliderValues(EnergyGrid grid, EnergyGrid.GridPlanState state)
    {
		energyGrid = grid;
        originalGridState = state;
        socketMaximum = grid.maxCountryCapacity;
		sourcePower = 0;
		greenGreyEnergyImage.sprite = grid.IsGreen ? greenEnergyIcon : greyEnergyIcon; 
		gridNameField.interactable = (state != EnergyGrid.GridPlanState.Removed);
		SetName(grid.name);

		//Reset production icons if we already existed
		distributionFillBar.DestroyAllFills();
		if(productionIcons != null)
			foreach (KeyValuePair<int, GameObject> pair in productionIcons)
				Destroy(pair.Value);

		//Recreate collections
		productionIcons = new Dictionary<int, GameObject>();
		producingCountries = new HashSet<int>();
		Dictionary<int, string> socketNames = grid.GetSocketNamesPerCountry();

        //Create items for all countries and set the socket limits
        ignoreDistributionUpdate = true; //Ignore distr updates while creating (it sorts, messing up order)
        int i = 0;
		foreach (KeyValuePair<int, CountryEnergyAmount> kvp in grid.energyDistribution.distribution)
		{
			DistributionItemEnergy item = null;

			//Create new or use existing
			if (i < items.Count)
			{
				item = (DistributionItemEnergy)items[i];
				item.graphic.color = TeamManager.GetTeamByTeamID(kvp.Key).color;
				item.Country = kvp.Key;
			}
			else
			{
				item = CreateItem(kvp.Key);
			}

			//Disable interactivity for removed grids
			//item.SetSliderInteractability(interactable);

            //Set socket maximum
            item.SetMaximum(socketMaximum);
            item.SetItemSocketMaximum(kvp.Value.maximum);
			item.SetAvailableRange(-kvp.Value.maximum, kvp.Value.maximum);

			//Set socket names
			if (socketNames.ContainsKey(kvp.Key))
			{
				string shortname = socketNames[kvp.Key];
				if (shortname.Length > 16)
					shortname = shortname.Substring(0, 16) + "...";
				item.SetValueText(shortname);
				item.SetValueTooltip(socketNames[kvp.Key]);
			}
			else
				item.SetValueText("None");

			//Set country source input
			sourcePower += kvp.Value.sourceInput;
			item.productionText.text = valueConversionCollection.ConvertUnit(kvp.Value.sourceInput, ValueConversionCollection.UNIT_WATT).FormatAsString();
			if (kvp.Value.sourceInput != 0)
			{
				producingCountries.Add(kvp.Key);
				SetCountryProductionIcon(kvp.Key, true);
			}

			//Set initial expected value
            item.SetOldValue(kvp.Value.expected);
			item.SetValue(kvp.Value.expected);
            i++;
        }

        //Remove unused items
        for (int j = items.Count - 1; j >= i; j--)
        {
            Destroy(items[j].gameObject);
            items.RemoveAt(j);
        }

        ignoreDistributionUpdate = false;
        UpdateStateIndicator(false, true);
        UpdateEntireDistribution();
        SortItems();
    }

    private void UpdateStateIndicator(bool hasChanged, bool force = false)
    {
        if (originalGridState == EnergyGrid.GridPlanState.Normal)
        {
            if (hasChanged)
            {
                stateIndicator.gameObject.SetActive(true);
                stateIndicator.sprite = changedSprite;
            }
            else
            {
                stateIndicator.gameObject.SetActive(false);
            }
        }
        else if (force)
        {
            switch (originalGridState)
            {
                case EnergyGrid.GridPlanState.Added:
                    stateIndicator.gameObject.SetActive(true);
                    stateIndicator.sprite = addedSprite;
                    break;
                case EnergyGrid.GridPlanState.Removed:
                    stateIndicator.gameObject.SetActive(true);
                    stateIndicator.sprite = removedSprite;
                    break;
                case EnergyGrid.GridPlanState.Changed:
                    stateIndicator.gameObject.SetActive(true);
                    stateIndicator.sprite = changedSprite;
                    break;
            }
        }
    }

    protected override bool CheckIfChanged()
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

            changed = changed || gridNameField.text != energyGrid.name;

            UpdateStateIndicator(changed);

            if (!changed)
                changeIndicator.DOFade(0f, 0.2f);
        }
        return changed;
    }

    public override void SetSliderInteractability(bool value)
    {
		interactable = originalGridState != EnergyGrid.GridPlanState.Removed && value;
		foreach (DistributionItem item in items)
		{
			item.SetSliderInteractability(interactable);
		}
		if (gridNameField != null)
		{
			gridNameField.interactable = interactable;
		}
	}

    public override void UpdateDistributionItem(DistributionItem updatedItem, float val)
    {
        UpdateEntireDistribution();
    }

    public override void UpdateEntireDistribution()
    {
        if (ignoreDistributionUpdate)
            return;

        //Total input
        long totalInput = 0;
        foreach (DistributionItemEnergy entry in items)
        {
            if (entry.GetDistributionValueLong() <= 0)
            {
                totalInput += Math.Abs(entry.GetDistributionValueLong());
            }
        }
        totalInput += sourcePower;
		totalPower.text = valueConversionCollection.ConvertUnit(totalInput, ValueConversionCollection.UNIT_WATT).FormatAsString();

		//Remaining
		long remaining = totalInput;
        foreach (DistributionItemEnergy entry in items)
        {
            if (entry.GetDistributionValueLong() > 0)
            {
                remaining -= Math.Abs(entry.GetDistributionValueLong());
            }
        }

        // Output max with given slider value
        foreach (DistributionItemEnergy entry in items)
        {
            //Min of socketmax and currentvalue+remaining
            entry.SetAvailableMaximum(entry.GetDistributionValueLong() + remaining);

            // Text
            entry.ToText(entry.GetDistributionValueLong(), 1f);
        }

        // New output max after slider value was applied (setting available range also clamps values)
        foreach (DistributionItemEnergy entry in items)
        {
            //Min of socketmax and currentvalue+remaining
            entry.SetAvailableMaximum(entry.GetDistributionValueLong() + remaining);
        }

        // Output fills and send icons
        foreach (DistributionItemEnergy entry in items)
        {
            // Input
            if (entry.GetDistributionValueLong() < 0)
            {
				SetCountryProductionIcon(entry.Country, true);
                distributionFillBar.SetFill(entry.Country, 0);
            }
            // Output
            else if (entry.GetDistributionValueLong() > 0)
            {
				SetCountryProductionIcon(entry.Country, false);
                distributionFillBar.SetFill(entry.Country, entry.GetDistributionValueLong());
            }
			// Neither
            else if (entry.GetDistributionValueLong() == socketMaximum)
            {
				SetCountryProductionIcon(entry.Country, false);
                distributionFillBar.SetFill(entry.Country, 0.0f);
            }
        }

        // Empty fill
        distributionFillBar.CreateEmptyFill(totalInput, false);

        CheckIfChanged();
		distributionFillBar.SortFills();
    }

	void SetCountryProductionIcon(int country, bool on)
	{
		bool exists = productionIcons.ContainsKey(country);
		if (on)
		{
			if (!exists)
			{
				GameObject temp = Instantiate(productionIconPrefab);
				temp.GetComponent<Image>().color = TeamManager.GetTeamByTeamID(country).color;
				temp.transform.SetParent(productionIconParent);
				productionIcons.Add(country, temp);
			}
			else
				productionIcons[country].SetActive(true);
		}
		else if(exists && !producingCountries.Contains(country))
			productionIcons[country].SetActive(false);
	}
}
