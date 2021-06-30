using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class KPIGroups : MonoBehaviour
{
	[Header("Prefabs")]
	public GameObject barPrefab;
	public Transform contentLocation;
	private Dictionary<string, KPIGroupBar> bars = new Dictionary<string, KPIGroupBar>();
    
	private void Start()
	{
		StartCoroutine(ResizeLayout());
	}
    
	private void CreateBarForGrid(EnergyGrid grid, int country)
	{
		GameObject go = Instantiate(barPrefab);
		KPIGroupBar bar = go.GetComponent<KPIGroupBar>();
		bars.Add(grid.GetDatabaseID().ToString(), bar);
		go.transform.SetParent(contentLocation, false);

		bar.title.text = grid.name;

		//set actual grid data
		bar.SetToEnergyValues(grid, country);

	}

	private void UpdateBarForFishing(string name, Dictionary<int, float> values)
	{
		KPIGroupBar bar;
		if (bars.TryGetValue(name, out bar))
		{
			bar.UpdateFishingValues(values);
		}
		else
		{
			CreateBarForFishing(name, values);
		}
	}

	private void CreateBarForFishing(string name, Dictionary<int, float> values)
	{
		GameObject go = Instantiate(barPrefab);
		KPIGroupBar bar = go.GetComponent<KPIGroupBar>();
		bars.Add(name, bar);
		go.transform.SetParent(contentLocation, false);

		bar.title.text = name;

		//set actual fishing data
		bar.UpdateFishingValues(values);
	}

	private void ClearBars()
	{
		foreach (KPIGroupBar bar in bars.Values)
		{
			Destroy(bar.gameObject);
		}

		bars.Clear();
	}

	public void SetBarsToGrids(List<EnergyGrid> grids, int country)
	{
		ClearBars();
        //Checks if grid is more than just a socket, is relevant for our country
        if (grids != null)
		{
			foreach (EnergyGrid grid in grids)
			{
				if (grid.ShouldBeShown && grid.CountryHasSocketInGrid(country))
				{
					CreateBarForGrid(grid, country);
				}
			}
		}
	}

	public void SetBarsToFishing(FishingDistributionSet distributionDelta)
	{
		foreach (KeyValuePair<string, Dictionary<int, float>> kvp in distributionDelta.GetValues())
		{
			UpdateBarForFishing(kvp.Key, kvp.Value);
		}
	}

	
	// Resizes the content of the scroll rect
	IEnumerator ResizeLayout()
	{
		yield return new WaitForEndOfFrame();

		//layoutSizeRefitter.ignoreLayout = false;
		//LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layoutSizeRefitter.transform);
		//Canvas.ForceUpdateCanvases();
		//layoutSizeRefitter.ignoreLayout = true;
	}
}
