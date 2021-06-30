using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class KPIGroupBar : MonoBehaviour
{
    [Header("References")]
	public TextMeshProUGUI title;
	public TextMeshProUGUI barValueText;
	public GameObject childContainer;
	public RectTransform foldTrans;
	public Image foldGraphic;
	public Image groupCountryImage;
    public Toggle barToggle;

	[SerializeField]
	private DistributionFillBar input = null;
    public Button occulusButton;

	[Header("Prefabs")]
	public GameObject itemPrefab;
	public GameObject countryIconPrefab;
	public Transform contentLocation;

	[Header("ValueConverter")]
	[SerializeField]
	private ValueConversionCollection valueConversionCollection = null;

	private List<KPIGroupBarItem> items = new List<KPIGroupBarItem>();

	private void Awake()
	{
		barToggle.onValueChanged.AddListener(SetExpandedInternal);
	}

	private KPIGroupBarItem CreateItem(int teamID, float val, string valueText)
	{
		Color col;
		if (teamID < 0)
			col = Color.white;
		else
			col = TeamManager.GetTeamByTeamID(teamID).color;

		// Generate item
		GameObject go = Instantiate(itemPrefab);
		KPIGroupBarItem item = go.GetComponent<KPIGroupBarItem>();
		items.Add(item);
		go.transform.SetParent(contentLocation, false);

		// Set values
		item.teamGraphic.color = col;
		item.team = teamID;
		UpdateItem(item, val, valueText);

		foldGraphic.enabled = true;
		return item;
	}

	private KPIGroupBarItem CreateItem(int teamID, float val, bool createFillWeights = true)
	{
		KPIGroupBarItem item = CreateItem(teamID, val, val.Abbreviated());

		// Create fill
		if (createFillWeights)
		{
			CalculateEcologyFillWeights();
		}

		return item;
	}

	private void UpdateItem(KPIGroupBarItem item, float value, string valueText)
	{
		item.value = value;
		item.numbers.text = valueText;

		if (input != null)
		{
			input.SetFill(item.team, value);
		}
	}

	private KPIGroupBarItem CreateEnergyItem(string title, string valueText)
	{
		// Generate item
		GameObject go = Instantiate(itemPrefab);
		KPIGroupBarItem item = go.GetComponent<KPIGroupBarItem>();
		items.Add(item);
		go.transform.SetParent(childContainer.transform, false);

		// Set values
		item.teamGraphic.gameObject.SetActive(false);
		item.numbers.text = valueText;
		item.title.text = title;

		foldGraphic.enabled = true;
		return item;
	}

	private KPIGroupBarItem CreateEnergyItem(int teamID, string valueText)
	{
		// Generate item
		GameObject go = Instantiate(itemPrefab);
		KPIGroupBarItem item = go.GetComponent<KPIGroupBarItem>();
		items.Add(item);
		go.transform.SetParent(childContainer.transform, false);

		// Set values
		item.teamGraphic.color = TeamManager.GetTeamByTeamID(teamID).color;
		item.numbers.text = valueText;
		item.title.gameObject.SetActive(false);

		foldGraphic.enabled = true;
		return item;
	}

    private KPIGroupBarItem SetItemAtIndexTo(int index, string title, string valueText)
    {
        if (items.Count <= index)
            return CreateEnergyItem(title, valueText);

        // Set item to values
        KPIGroupBarItem item = items[index];
        item.numbers.text = valueText;
        item.title.text = title;
        item.title.gameObject.SetActive(true);
        item.teamGraphic.gameObject.SetActive(false);

        foldGraphic.enabled = true;
        return item;
    }

    private KPIGroupBarItem SetItemAtIndexTo(int index, int teamID, string valueText)
    {
        if (items.Count <= index)
            return CreateEnergyItem(teamID, valueText);

        // Set item to values
        KPIGroupBarItem item = items[index];
        item.teamGraphic.color = TeamManager.GetTeamByTeamID(teamID).color;
        item.numbers.text = valueText;
        item.title.gameObject.SetActive(false);
        item.teamGraphic.gameObject.SetActive(true);

        foldGraphic.enabled = true;
        return item;
    }

    public void DestroyItem(KPIGroupBarItem item)
	{
		// Ecology
		if (input != null)
		{
			input.DestroyFill(item.team);
		}

		items.Remove(item);
		Destroy(item.gameObject);

		if (items.Count <= 0)
		{

			foldGraphic.enabled = false;
			SetExpanded(false);
		}
	}

	public void CalculateEcologyFillWeights()
	{
		for (int i = 0; i < items.Count; i++)
		{
			input.SetFill(items[i].team, items[i].value);
		}

		SortItems();
		SortFills();
	}

	public void SortItems()
	{
		for (int i = 0; i < items.Count; i++)
		{
			int siblingIndex = 0;
			foreach (Team team in TeamManager.GetTeams())
			{
				if (items[i].teamGraphic.color == team.color)
				{
					items[i].transform.SetSiblingIndex(siblingIndex);
				}
				++siblingIndex;
			}
		}
	}

	private void SortFills()
	{
		input.SortFills();
	}

    private void SetExpandedInternal(bool expanded)
    {
        childContainer.SetActive(expanded);
        Vector3 rot = foldTrans.eulerAngles;
        foldTrans.eulerAngles = expanded ? new Vector3(rot.x, rot.y, 90f) : new Vector3(rot.x, rot.y, 0f);
    }

    public void SetExpanded(bool expanded)
    {
        barToggle.isOn = expanded;
    }

	private float MaxOutputEcology()
	{
		float maxOutput = 0f;
		for (int i = 0; i < items.Count; i++)
		{
			maxOutput += items[i].value;
		}
		return maxOutput;
	}

	public void UpdateFishingValues(Dictionary<int, float> values)
	{
		foreach (KeyValuePair<int, float> kvp in values)
		{
			KPIGroupBarItem item = items.Find(obj => obj.team == kvp.Key);
			if (item == null)
			{
				CreateItem(kvp.Key, kvp.Value);
			}
			else
			{
				UpdateItem(item, kvp.Value, kvp.Value.Abbreviated());
			}
		}

		input.CreateEmptyFill(FishingDistributionDelta.MAX_SUMMED_FISHING_VALUE, true);
	}

	public void SetToEnergyValues(EnergyGrid grid, int country)
	{
        occulusButton.onClick.RemoveAllListeners();
        occulusButton.onClick.AddListener(() => grid.ShowGridOnMap());
		GameObject dots = contentLocation.GetChild(0).gameObject;
		groupCountryImage.color = TeamManager.GetTeamByTeamID(country).color;
		int numberCountryIcons = 0;
		float totalUsedPower = 0;
        int nextItemIndex = 0;

		foreach (KeyValuePair<int, CountryEnergyAmount> kvp in grid.energyDistribution.distribution)
		{
			if (grid.actualAndWasted == null)
				continue;
			float target = kvp.Value.expected;
			float received = grid.actualAndWasted.socketActual.ContainsKey(kvp.Key) ? grid.actualAndWasted.socketActual[kvp.Key] : 0;

			if (kvp.Key == country)
			{
				//Our team, put it in the group bar
				string formatString;
				if (kvp.Value.expected < 0)
				{
					formatString = "Sent {0} / {1} target";
				}
				else
				{
					formatString = "Got {0} / {1} target";
					totalUsedPower += received;
				}
				barValueText.text = string.Format(formatString, valueConversionCollection.ConvertUnit(received, ValueConversionCollection.UNIT_WATT).FormatAsString(), 
					valueConversionCollection.ConvertUnit(target, ValueConversionCollection.UNIT_WATT).FormatAsString());
			}
			else
			{
				//Other team, create an entry
				string formatString;
				if (kvp.Value.expected < 0)
				{
					//Send
					formatString = "Sent {0} / {1} target";
				}
				else
				{
					//Receive
					formatString = "Got {0} / {1} target";
					totalUsedPower += received;
				}

				SetItemAtIndexTo(nextItemIndex, kvp.Key, string.Format(formatString, valueConversionCollection.ConvertUnit(received, ValueConversionCollection.UNIT_WATT).FormatAsString(), 
					valueConversionCollection.ConvertUnit(target, ValueConversionCollection.UNIT_WATT).FormatAsString()));
                nextItemIndex++;
			
				//Create group distribution icons for countries
				if (numberCountryIcons < 3)
				{
					Image temp = Instantiate(countryIconPrefab).GetComponent<Image>();
					temp.color = TeamManager.GetTeamByTeamID(kvp.Key).color;
					temp.transform.SetParent(contentLocation);
				}
				numberCountryIcons++;
			}
		}

        //Create summary
        SetItemAtIndexTo(nextItemIndex, "Total  ", string.Format("Used {0} / {1} ({2})", valueConversionCollection.ConvertUnit(totalUsedPower, ValueConversionCollection.UNIT_WATT).FormatAsString(), 
			valueConversionCollection.ConvertUnit(grid.AvailablePower, ValueConversionCollection.UNIT_WATT).FormatAsString(), (totalUsedPower / (float)grid.AvailablePower).ToString("P1")));
        nextItemIndex++;

        //Clear unused items
        for (int i = nextItemIndex; i < items.Count; i++)
            Destroy(items[i]);
        items.RemoveRange(nextItemIndex, items.Count - nextItemIndex);

        dots.SetActive(numberCountryIcons > 3);
		dots.transform.SetAsLastSibling();
		contentLocation.gameObject.SetActive(numberCountryIcons > 0);
	}
}