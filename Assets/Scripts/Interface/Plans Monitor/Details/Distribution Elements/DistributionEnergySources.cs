using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DistributionEnergySources : MonoBehaviour {

    [Header("Variables")]
    public long sourcesTotal;

    [Header("UI")]
    public Text text;

    [Header("Sources")]
    public GameObject distributionCover;
    public GameObject distribution;

    [Header("Prefabs")]
    public GameObject itemPrefab;
    public Transform contentLocation;
    public Dictionary<Color, KPIGroupBarItem> itemsDict = new Dictionary<Color, KPIGroupBarItem>();

    [Header("Parent Reference")]
    public DistributionGroupEnergy group;

    private void Start()
    {
        // Default settings
        distributionCover.SetActive(true);
        distribution.SetActive(!distributionCover.activeSelf);
    }

    public KPIGroupBarItem CreateItem(Color col, string valText, float val = 0f)
    {
        // Generate item
        GameObject go = Instantiate(itemPrefab);
        KPIGroupBarItem item = go.GetComponent<KPIGroupBarItem>();
        itemsDict.Add(col, item);
        go.transform.SetParent(contentLocation, false);
        
        // Set values
        item.teamGraphic.color = col;
        item.numbers.text = valText;// val.Abbreviated();
        item.value = val;

        // Determine if active
        item.gameObject.SetActive(val != 0f);

        // Set value & text
        group.sourcePower = SourcesTotal();
        //text.text = "Generated: " + valText;//sources.Abbreviated();

        return item;
    }

    public void SetSourcesTotalText(string valText)
    {
        text.text = "Generated: " + valText;
    }

    public void DestroyItem(Color col)
    {
        Destroy(itemsDict[col].gameObject);
        itemsDict.Remove(col);

        SourcesTotal();
    }

    public void DestroyAllItems()
    {
        foreach (KeyValuePair<Color, KPIGroupBarItem> kvp in itemsDict)
            Destroy(kvp.Value.gameObject);
        itemsDict = new Dictionary<Color, KPIGroupBarItem>();
    }

    /// <summary>
    /// Total of all the sources listed in the items
    /// </summary>
    public long SourcesTotal()
    {
        sourcesTotal = 0;

        //foreach (KeyValuePair<Color, KPIGroupBarItem> entry in itemsDict) {
        //    sourcesTotal += entry.Value.value;
        //}

        return sourcesTotal;
    }

    public void BarPressed()
    {
        // Flip the active state
        distributionCover.SetActive(!distributionCover.activeSelf);
        distribution.SetActive(!distribution.activeSelf);
    }
    
}