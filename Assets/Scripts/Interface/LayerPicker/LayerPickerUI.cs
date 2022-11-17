using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LayerPickerUI : MonoBehaviour
	{
		[SerializeField] private Toggle toggleObject = null;
		[SerializeField] private Dropdown dropdownObject = null;
		[SerializeField] private Button toggleButtonObject = null;
		[SerializeField] private Transform contentParent;

		private List<int> selectedLayerIDs;
		private Dictionary<AbstractLayer, Toggle> buttons;
		private Dropdown groupDropdown;
		private Dictionary<string, GameObject> categoryGroup;
		private List<string> areaGroup;
		private bool allEnabled = true;
		private int heirachyIndex = 0;

		public Action<List<int>> onLayersSelected;

		public void HideUI()
		{
			Destroy(gameObject);
		}

		public void CreateUI()
		{
			gameObject.SetActive(true);
			selectedLayerIDs = new List<int>();
			categoryGroup = new Dictionary<string, GameObject>();
			areaGroup = new List<string>();
			buttons = new Dictionary<AbstractLayer, Toggle>();

			heirachyIndex = this.transform.GetSiblingIndex();

			groupDropdown = Instantiate(dropdownObject, contentParent).GetComponent<Dropdown>();
			groupDropdown.gameObject.transform.SetSiblingIndex(heirachyIndex + 1);

			foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
			{
				AddLayer(layer);
			}
		}

		private void AddToGroupDropdown(string name)
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
					foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
					{
						buttons[layer].gameObject.SetActive(false);
						buttons[layer].isOn = false;
					}

					// layers that match this type
					foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers().Intersect(LayerManager.Instance.GetAllLayersOfGroup(tmpName)))
					{
						buttons[layer].gameObject.SetActive(true);
						buttons[layer].isOn = true;
					}
				}
				else
				{

					foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
					{
						buttons[layer].gameObject.SetActive(true);
						buttons[layer].isOn = true;
					}
				}

				allEnabled = true;

				InterfaceCanvas.Instance.SetRegionWithName(tmpName);

				groupDropdown.RefreshShownValue();

			});

			areaGroup.Add(name);
		}

		private void AddLayer(AbstractLayer layer)
		{
			AbstractLayer tmpLayer = layer;
			// Category name of this nayer
			string groupName = tmpLayer.Category;

			// Check if a group of that name exsits
			GameObject group = GetCategoryGroup(groupName);
			if (group == null)// if it doesnt,  create a new one
			{
				group = CreateCategoryGroup(groupName);
				categoryGroup.Add(groupName, group);
			}
			// if the dropdown doesnt have that group, add it
			if (!DoesGroupExist(tmpLayer.Group))
			{
				AddToGroupDropdown(tmpLayer.Group);
			}

			// Add a toggle object and set the text
			Toggle toggle = Instantiate(toggleObject, contentParent);
			toggle.GetComponentInChildren<Text>().text = tmpLayer.GetShortName();

			// Set default value
			ToggleLayer(toggle.isOn, tmpLayer);

			// toggle layer sets if this layer will be loaded or not
			toggle.onValueChanged.AddListener(delegate { ToggleLayer(toggle.isOn, tmpLayer); });

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

		private GameObject GetCategoryGroup(string categoryName)
		{
			if (categoryGroup.ContainsKey(categoryName))
			{
				return categoryGroup[categoryName];
			}
			return null;
		}

		private bool DoesGroupExist(string groupName)
		{
			return areaGroup.Contains(groupName);
		}

		private GameObject CreateCategoryGroup(string groupName)
		{
			bool groupEnabled = true;

			string tmpName = groupName;

			GameObject group = Instantiate(toggleButtonObject.gameObject, contentParent);
			group.transform.SetSiblingIndex(heirachyIndex + 1);

			Button button = group.GetComponent<Button>();
			Text text = button.GetComponentInChildren<Text>();

			TriggerDelegates trigger = group.AddComponent<TriggerDelegates>();
			trigger.OnMouseEnterDelegate = () =>
			{
				HighlightCategoryGroup(true, groupName);
			};
			trigger.OnMouseExitDelegate = () =>
			{
				HighlightCategoryGroup(false, groupName);
			};

			text.text = "Toggle " + tmpName;

			button.onClick.AddListener(delegate
			{
				ToggleCategoryGroup(tmpName, groupEnabled);
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

		//Called by UI button
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

		private void ToggleCategoryGroup(string groupName, bool show)
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

		private void HighlightCategoryGroup(bool highlight, string groupName)
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

		//Called by UI button
		public void LoadSelectedLayers()
		{
			PlayerPrefs.Save();
			onLayersSelected?.Invoke(selectedLayerIDs);
			HideUI();
		}
	
		private void ToggleLayer(bool active, AbstractLayer layer)
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
	}
}
