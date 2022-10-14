using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LayerInterface : MonoBehaviour
	{
		[SerializeField, Tooltip("Prefab used for housing all the layer toggles belonging to a certain subcategory")]
		private GameObject layerSubcategoryToggleGroupPrefab = null;

		[SerializeField]
		private TextMeshProUGUI layerSubcategoryToggleGroupTitle = null;

		[SerializeField]
		private RectTransform layerSubcategoryToggleGroupContainer = null;

		// To get which layer corresponds to which genericLayer
		private Dictionary<string, LayerCategoryGroup> categories;
		private Dictionary<string, LayerSubCategoryToggleGroup> subCategories;
		private LayerSubCategoryToggleGroup selectedSubCategory = null;

		private GenericWindow layerSelect;

		private LayerPanel panel;

		private MenuBarToggle menuLayerToggle;

		

		private static LayerInterface instance;

		protected void Start()
		{
			panel = InterfaceCanvas.Instance.layerPanel;
			layerSelect = InterfaceCanvas.Instance.layerSelect;
			categories = new Dictionary<string, LayerCategoryGroup>();
			subCategories = new Dictionary<string, LayerSubCategoryToggleGroup>();

			menuLayerToggle = InterfaceCanvas.Instance.menuBarLayers;

			// This is for the menu bar toggle so that it acts correctly
			menuLayerToggle.GetComponent<Toggle>().isOn = true;
			//menuLayerToggle.GetComponent<Toggle>().onValueChanged.AddListener((toggleValue) => { layerSelectOpen = !toggleValue; });
			instance = this;
		}

		public void NotifyLayerSelectClosing()
		{
			if (selectedSubCategory != null)
			{
				selectedSubCategory.SetVisible(false);
				selectedSubCategory = null;
			}
		}

		public static void AddLayerToInterface(AbstractLayer layer)
		{
			instance.addLayerToInterface(layer);
		}

		private void addLayerToInterface(AbstractLayer layer)
		{
			if (layer.FileName.StartsWith("_PLAYAREA") || !layer.Toggleable)
			{
				return;
			}

			string category = layer.Category;
			string subcategoryID = layer.SubCategory;

			string categoryName = LayerManager.Instance.MakeCategoryDisplayString(category);

			// Creating/Getting the group
			LayerCategoryGroup categoryGroup = FindCategory(categoryName);
			if (categoryGroup == null)
			{
				LayerCategoryGroup newCategoryGroup = panel.CreateLayerGroup();
				newCategoryGroup.title.text = categoryName;
				categories.Add(categoryName, newCategoryGroup);
				categoryGroup = newCategoryGroup;
			}

			LayerSubCategoryToggleGroup subCategory = FindSubCategory(subcategoryID);

			if (subCategory == null)
			{
				subCategory = CreateSubCategory(categoryGroup, subcategoryID);
			}

			subCategory.CreateLayerToggle(layer, subcategoryID, this);

			//layerToggles.Add(tmpLayer, layerTab);
		}

		private LayerSubCategoryToggleGroup CreateSubCategory(LayerCategoryGroup categoryGroup, string subCategoryID)
		{
			string subCategoryName = LayerManager.Instance.MakeCategoryDisplayString(subCategoryID);

			LayerButton subcategoryToggleButton = categoryGroup.CreateLayerButton(subCategoryName);

			subcategoryToggleButton.button.onClick.AddListener(() => { ToggleLayerSubcategory(subCategoryID); });
			subcategoryToggleButton.button.onDoubleClick.AddListener(() => { ToggleAllLayersInSubcategory(subCategoryID); });

			// Creating/Getting the icon
			subcategoryToggleButton.icon.sprite = LayerManager.Instance.GetSubcategoryIcon(subCategoryID);
			AddTooltip tooltip = subcategoryToggleButton.gameObject.AddComponent<AddTooltip>();
			tooltip.text = subCategoryName;

			GameObject subcategoryContainer = Instantiate(layerSubcategoryToggleGroupPrefab, layerSubcategoryToggleGroupContainer);
			LayerSubCategoryToggleGroup subCategory = subcategoryContainer.GetComponent<LayerSubCategoryToggleGroup>();
			subCategory.subcategoryButton = subcategoryToggleButton;
			subCategory.SetVisible(false);
			subCategory.DisplayName = subCategoryName;
			subCategories.Add(subCategoryID, subCategory);

			return subCategory;
		}

		private LayerCategoryGroup FindCategory(string name)
		{
			foreach (var kvp in categories)
			{
				if (kvp.Value.title.text == name)
				{
					return kvp.Value;
				}
			}
			return null;
		}

		private LayerSubCategoryToggleGroup FindSubCategory(string subCategory)
		{
			LayerSubCategoryToggleGroup result;
			subCategories.TryGetValue(subCategory, out result);
			return result;
		}

		private void ToggleLayerSubcategory(string subcategory)
		{
			LayerSubCategoryToggleGroup toggleGroup;
			if (subCategories.TryGetValue(subcategory, out toggleGroup))
			{
				if (selectedSubCategory != null)
				{
					selectedSubCategory.SetVisible(false);
				}

				if (selectedSubCategory == toggleGroup)
				{
					layerSelect.gameObject.SetActive(false);
					selectedSubCategory = null;
				}
				else
				{
					layerSelect.gameObject.SetActive(true);
					layerSubcategoryToggleGroupTitle.text = toggleGroup.DisplayName;
					toggleGroup.SetVisible(true);
					selectedSubCategory = toggleGroup;
				}
			}
		}

		private void ToggleAllLayersInSubcategory(string subCategoryID)
		{
			LayerSubCategoryToggleGroup toggleGroup;
			if (subCategories.TryGetValue(subCategoryID, out toggleGroup))
			{
				toggleGroup.ToggleAllLayers();
			}
		}

		private void SetToggle(AbstractLayer layer, bool value)
		{
			if (layer.Toggleable)
			{
				LayerSubCategoryToggleGroup toggleGroup = FindSubCategory(layer.SubCategory);
				if (toggleGroup != null)
				{
					toggleGroup.SetLayerToggle(layer, value);
				}
			}
		}

		public void OnShowLayer(AbstractLayer layer)
		{
			SetToggle(layer, true);
			InterfaceCanvas.Instance.activeLayers.AddLayer(layer);
			//activeLayerTab.UpdateInterfaceVisibility();
		}

		public void OnHideLayer(AbstractLayer layer)
		{
			SetToggle(layer, false);
			InterfaceCanvas.Instance.activeLayers.ToggleLayer(layer, false);
			//activeLayerTab.UpdateInterfaceVisibility();
		}

		public void SetLayerVisibilityLock(AbstractLayer layer, bool value)
		{
			InterfaceCanvas.Instance.activeLayers.SetLayerVisibilityLocked(layer, value);
		}

		public void RefreshActiveLayer(AbstractLayer layer)
		{
			InterfaceCanvas.Instance.activeLayers.RemoveLayer(layer);
			InterfaceCanvas.Instance.activeLayers.AddLayer(layer);
			//activeLayerTab.UpdateInterfaceVisibility(); // This is so that if one the layertab has been closed, itll open again so you can see changes
		}

		public static void SortLayerToggles()
		{
			foreach (var kvp in instance.subCategories)
				kvp.Value.SortLayerToggles();
		}
	}
}