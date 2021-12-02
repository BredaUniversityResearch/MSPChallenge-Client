using System.Collections.Generic;
using UnityEngine;

//Class for holding all layer toggles shown in the layer selector UI for a specific layer subcategory. 
public class LayerSubCategoryToggleGroup: MonoBehaviour
{
	[SerializeField]
	private GameObject layerTogglePrefab = null;

	[SerializeField, Tooltip("Container element that will house all the layer toggles.")]
	private RectTransform layerToggleContainer = null;

	private Dictionary<AbstractLayer, GenericLayer> layerToggles = new Dictionary<AbstractLayer, GenericLayer>(8);
	public LayerButton subcategoryButton;

	public string DisplayName
	{
		get;
		set;
	}

	public void CreateLayerToggle(AbstractLayer layer, string subcategoryID, LayerInterface parentInterface)
	{
		// Adding all the layer tabs
		GenericLayer layerTab = CreateLayerToggle(layer.GetShortName(), true);
		layerTab.icon.sprite = parentInterface.GetIcon(subcategoryID);
		layerTab.SubCategory = subcategoryID;


		layerTab.toggle.isOn = LayerManager.LayerIsVisible(layer);
		layerTab.toggle.onValueChanged.AddListener((toggleValue) =>
		{
			if (UIManager.ignoreLayerToggleCallback)
				return;

			UIManager.ignoreLayerToggleCallback = true;
			if (toggleValue)
				LayerManager.ShowLayer(layer);
			else
				LayerManager.HideLayer(layer);
			UIManager.ignoreLayerToggleCallback = false;
		});

		AddTooltip tooltip = layerTab.toggle.gameObject.AddComponent<AddTooltip>();
		tooltip.text = layer.Tooltip;

		//layerTab.barButton.onClick.AddListener(() =>
		//{
		//	if (layerTab.toggle.isOn)
		//	{
		//		LayerManager.HideLayer(layer);
		//	}
		//	else
		//	{
		//		LayerManager.ShowLayer(layer);
		//	}
		//});

		if (layer.Toggleable == false && Main.IsDeveloper == false)
		{
			layerTab.gameObject.SetActive(false);
		}

		layerToggles.Add(layer, layerTab);
	}

	private GenericLayer CreateLayerToggle(string layerName, bool closeable = false)
	{
		// Instantiate prefab
		GameObject go = Instantiate(layerTogglePrefab, layerToggleContainer);
		GenericLayer layer = go.GetComponent<GenericLayer>();
		layer.SetTitle(layerName);
		return layer;
	}

	public void SetLayerToggle(AbstractLayer layer, bool value)
	{
		GenericLayer layerElement;
		if (layerToggles.TryGetValue(layer, out layerElement))
		{
			layerElement.toggle.isOn = value;
		}
	}

	public void ToggleAllLayers()
	{
		int layerCounter = 0;
		bool toggleToVisibility = false;
		foreach (KeyValuePair<AbstractLayer, GenericLayer> layerEntry in layerToggles)
		{
			if (layerCounter == 0)
			{
				toggleToVisibility = !layerEntry.Value.toggle.isOn;
			}

			++layerCounter;

			layerEntry.Value.toggle.isOn = toggleToVisibility;
		}
	}

	public void SetVisible(bool state)
	{
		gameObject.SetActive(state);
		subcategoryButton.SetSelectedVisuals(state);
	}

	public void SortLayerToggles()
	{
		List<GenericLayer> toggles = new List<GenericLayer>(layerToggles.Count);
		foreach (var kvp in layerToggles)
			toggles.Add(kvp.Value);
		toggles.Sort((a, b) => a.title.text.CompareTo(b.title.text));
		for (int i = 0; i < toggles.Count; i++)
			toggles[i].transform.SetSiblingIndex(i);
	}
}