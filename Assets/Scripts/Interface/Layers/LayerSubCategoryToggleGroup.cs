﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColourPalette;

//Class for holding all layer toggles shown in the layer selector UI for a specific layer subcategory. 
namespace MSP2050.Scripts
{
	public class LayerSubCategoryToggleGroup: MonoBehaviour
	{
		[SerializeField]
		private GameObject layerTogglePrefab = null;

		[SerializeField, Tooltip("Container element that will house all the layer toggles.")]
		private RectTransform layerToggleContainer = null;

        //public LayerSubCategory subcategoryButton;

        [SerializeField]
        private DoubleClickButton button;

        [SerializeField]
        private CustomButtonColorSet outlineColourSet;

        [SerializeField]
        private ColourAsset accentColour;

        [SerializeField]
        private Image icon;

        private Dictionary<AbstractLayer, GenericLayer> layerToggles = new Dictionary<AbstractLayer, GenericLayer>(8);

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


			layerTab.toggle.isOn = LayerManager.Instance.LayerIsVisible(layer);
			layerTab.toggle.onValueChanged.AddListener((toggleValue) =>
			{
				if (InterfaceCanvas.Instance.ignoreLayerToggleCallback)
					return;

				InterfaceCanvas.Instance.ignoreLayerToggleCallback = true;
				if (toggleValue)
					LayerManager.Instance.ShowLayer(layer);
				else
					LayerManager.Instance.HideLayer(layer);
				InterfaceCanvas.Instance.ignoreLayerToggleCallback = false;
			});

			AddTooltip tooltip = layerTab.toggle.gameObject.AddComponent<AddTooltip>();
			tooltip.text = layer.Tooltip;
			
			if (!layer.Toggleable && !Main.IsDeveloper)
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
			SetSelectedVisuals(state);
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

        /// <summary>
		/// Hide the button
		/// </summary>
		public void SetVisibility(bool toggle)
        {
            gameObject.SetActive(toggle);
        }

        public void SetSelectedVisuals(bool selected)
        {
            if (selected)
                outlineColourSet.LockToColor(accentColour);
            else
                outlineColourSet.UnlockColor();
        }
    }
}