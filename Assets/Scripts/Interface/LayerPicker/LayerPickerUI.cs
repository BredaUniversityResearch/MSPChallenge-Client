using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class LayerPickerUI : MonoBehaviour
{
	[SerializeField]
	private Toggle toggleObject = null;

	[SerializeField]
	private Dropdown dropdownObject = null;

	[SerializeField]
	private Button toggleButtonObject = null;

	private static LayerPickerUI instance;

	private List<int> selectedLayerIDs;
	private Dictionary<AbstractLayer, Toggle> buttons;

	private Dropdown groupDropdown;

	private Dictionary<string, GameObject> categoryGroup;

	private List<string> areaGroup;

	private bool allEnabled = true;

	private int heirachyIndex = 0;

	protected void Start()
	{
		if (LayerImporter.IsCurrentlyImportingLayers)
		{
			HideUI();
			return;
		}

		instance = this;
		selectedLayerIDs = new List<int>();
		categoryGroup = new Dictionary<string, GameObject>();
		areaGroup = new List<string>();
		buttons = new Dictionary<AbstractLayer, Toggle>();

		heirachyIndex = this.transform.GetSiblingIndex();

		groupDropdown = Instantiate(dropdownObject) as Dropdown;
		groupDropdown.gameObject.transform.SetParent(this.transform);
		groupDropdown.gameObject.transform.SetSiblingIndex(heirachyIndex + 1);

	}

	public static void HideUI()
	{
		if (instance != null)
		{
			instance.gameObject.SetActive(false);
		}
	}

	private void addToGroupDropdown(string name)
	{
		List<string> dropdown = new List<string>();
		dropdown.Add(name);

		groupDropdown.RefreshShownValue();

		groupDropdown.AddOptions(dropdown);

		groupDropdown.onValueChanged.AddListener((value) =>
		{
			string tmpName = groupDropdown.options[value].text;

			if (value != 0) // If the dropdown is not on the first element ("All")
			{
				// Disable everything
				foreach (AbstractLayer layer in LayerManager.GetAllValidLayers())
				{
					buttons[layer].gameObject.SetActive(false);
					buttons[layer].isOn = false;
				}

				// layers that match this type
				foreach (AbstractLayer layer in LayerManager.GetAllValidLayers().Intersect(LayerManager.GetAllValidLayersOfGroup(tmpName)))
				{
					buttons[layer].gameObject.SetActive(true);
					buttons[layer].isOn = true;
				}
			}
			else
			{

				foreach (AbstractLayer layer in LayerManager.GetAllValidLayers())
				{
					buttons[layer].gameObject.SetActive(true);
					buttons[layer].isOn = true;
				}
			}

			allEnabled = true;

			InterfaceCanvas.Instance.SetRegionWithName(tmpName);
			//if (tmpName == "northsee")
			//{
			//	InterfaceCanvas.Instance.SetRegion(RegionEdition.NorthSea);
			//}
			//else if (tmpName == "balticline")
			//{
			//	InterfaceCanvas.Instance.SetRegion(RegionEdition.Baltic);
			//}
			//if (tmpName == "simcelt")
			//{
			//	InterfaceCanvas.Instance.SetRegion(RegionEdition.Clyde);
			//}

			groupDropdown.RefreshShownValue();

		});

		areaGroup.Add(name);
	}

	private void checkIfAllTogglesAreEnabled()
	{
		// If all toggles are selected, set allenabled to true
		// if none are selected, set allenabled to false
	}

	private void createUI()
	{
		List<AbstractLayer> validLayers = LayerManager.GetAllValidLayers();

		foreach (AbstractLayer layer in validLayers)
		{
			AddLayer(layer);
		}
	}

	/// <summary>
	/// Adds a layer to the layer picker UI
	/// </summary>
	/// <param name="layer">layer to Add to the UI</param>
	private void AddLayer(AbstractLayer layer)
	{
		AbstractLayer tmpLayer = layer;
		// Category name of this nayer
		string groupName = tmpLayer.Category;

		// Check if a group of that name exsits
		GameObject group = getCategoryGroup(groupName);
		if (group == null)// if it doesnt,  create a new one
		{
			group = createCategoryGroup(groupName);
			categoryGroup.Add(groupName, group);
		}
		// if the dropdown doesnt have that group, add it
		if (!doesGroupExist(tmpLayer.Group))
		{
			addToGroupDropdown(tmpLayer.Group);
		}

		// Add a toggle object and set the text
		Toggle toggle = Instantiate(toggleObject);
		toggle.gameObject.transform.SetParent(this.transform);
		toggle.GetComponentInChildren<Text>().text = tmpLayer.GetShortName();

		// Set default value
		toggleLayer(toggle.isOn, tmpLayer);

		// toggle layer sets if this layer will be loaded or not
		toggle.onValueChanged.AddListener(delegate { toggleLayer(toggle.isOn, tmpLayer); checkIfAllTogglesAreEnabled(); });

		// by default set layers with a Short Name to on
		if (tmpLayer.ShortName != "")
		{
			toggle.isOn = true;
		}

		// dont allow toggling the player area
		if (tmpLayer.FileName.StartsWith("_PLAYAREA"))
		{
			toggle.interactable = false;
			toggle.isOn = true;
		}

		// tooltip
		string layerInfo = "Category: " + tmpLayer.Category + ", SubCategory: " + tmpLayer.SubCategory;
		AddTooltip tooltip = toggle.gameObject.AddComponent<AddTooltip>();
		tooltip.text = layerInfo;
		buttons.Add(tmpLayer, toggle);

		toggle.isOn = GetLayerActiveState(tmpLayer, toggle.isOn);
	}

	private bool GetLayerActiveState(AbstractLayer layer, bool defaultValue)
	{
		bool result = defaultValue;
		if (PlayerPrefs.HasKey(layer.FileName))
		{
			result = PlayerPrefs.GetInt(layer.FileName) != 0;
		}

		return result;
	}

	private void SetLayerActiveState(AbstractLayer layer, bool active)
	{
		PlayerPrefs.SetInt(layer.FileName, active ? 1 : 0);
	}

	private GameObject getCategoryGroup(string categoryName)
	{
		if (categoryGroup.ContainsKey(categoryName))
		{
			return categoryGroup[categoryName];
		}
		return null;
	}

	private bool doesGroupExist(string groupName)
	{
		return areaGroup.Contains(groupName);
	}

	private GameObject createCategoryGroup(string groupName)
	{
		bool groupEnabled = true;

		string tmpName = groupName;

		GameObject group = (GameObject)Instantiate(toggleButtonObject.gameObject);
		group.transform.SetParent(this.transform);
		group.transform.SetSiblingIndex(heirachyIndex + 1);

		Button button = group.GetComponent<Button>();
		Text text = button.GetComponentInChildren<Text>();

		TriggerDelegates trigger = group.AddComponent<TriggerDelegates>();
		trigger.OnMouseEnterDelegate = () =>
		{
			highlightCategoryGroup(true, groupName);
		};
		trigger.OnMouseExitDelegate = () =>
		{
			highlightCategoryGroup(false, groupName);
		};

		text.text = "Toggle " + tmpName;

		button.onClick.AddListener(delegate
		{
			toggleCategoryGroup(tmpName, groupEnabled);
			groupEnabled = !groupEnabled;
		});

		return group;
	}

	public void ShowToggleButtons(bool show)
	{
		foreach (var kvp in categoryGroup)
		{
			kvp.Value.SetActive(show);
		}
	}

	public void ToggleAll()
	{
		allEnabled = !allEnabled;
		// DO a check if they are either all selected or all disabled
		foreach (var kvp in buttons)
		{
			if (kvp.Value.gameObject.activeInHierarchy) // Dont enable disabled tickboxes
			{
				if (!kvp.Key.FileName.Contains("_PLAYAREA")) // dont change the playareas
				{
					kvp.Value.isOn = allEnabled;
				}
			}
		}
	}

	private void toggleCategoryGroup(string groupName, bool show)
	{
		foreach (var kvp in buttons)
		{
			if (kvp.Key.Category == groupName)
			{
				if (kvp.Value.gameObject.activeInHierarchy) // Dont enable disabled tickboxes
					kvp.Value.isOn = show;
			}
		}
	}

	private void highlightCategoryGroup(bool highlight, string groupName)
	{
		Color normalColour = Util.HexToColor("#323232FF"); //gray colour
		foreach (var kvp in buttons)
		{
			if (highlight)
			{
				if (kvp.Key.Category == groupName)
				{
					kvp.Value.GetComponentInChildren<Text>().color = normalColour;
				}
				else
				{
					kvp.Value.GetComponentInChildren<Text>().color = Color.gray;
				}
			}
			else
			{
				kvp.Value.GetComponentInChildren<Text>().color = normalColour;
			}
		}
	}

	public void LoadSelectedLayers()
	{
		this.GetComponent<Image>().enabled = false;
		foreach (var kvp in buttons)
		{
			SetLayerActiveState(kvp.Key, kvp.Value.isOn);
			kvp.Value.gameObject.SetActive(false);
		}
		PlayerPrefs.Save();
		groupDropdown.gameObject.SetActive(false);

		LayerImporter.ImportLayers(selectedLayerIDs);
		gameObject.SetActive(false);
	}
	
	private void toggleLayer(bool active, AbstractLayer layer)
	{
		if (active)
		{
			selectedLayerIDs.Add(layer.ID);
		}
		else
		{
			if (selectedLayerIDs.Contains(layer.ID))
			{
				selectedLayerIDs.Remove(layer.ID);
			}
		}
	}

	public static void CreateUI()
	{
		instance.createUI();
	}
}
