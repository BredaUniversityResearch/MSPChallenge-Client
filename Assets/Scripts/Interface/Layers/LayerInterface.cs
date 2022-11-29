using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MSP2050.Scripts
{
	public class LayerInterface : MonoBehaviour
	{
		[SerializeField] private List<Sprite> m_icons; // Initialised in the inspector!!!
		[SerializeField] private GameObject m_subcategoryPrefab;
        [SerializeField] private GameObject m_categoryPrefab;
        [SerializeField] private GameObject m_layerPrefab;

        [SerializeField] private Transform m_categoryParent;
        [SerializeField] private Transform m_layerParent;
        [SerializeField] private GameObject m_layerSelectWindow;
		[SerializeField] private ToggleGroup m_subcategoryToggleGroup;
		[SerializeField] SearchBar m_searchBar;

        // To get which layer corresponds to which genericLayer
        private Dictionary<string, LayerCategoryBar> m_categories = new Dictionary<string, LayerCategoryBar>();
		private Dictionary<string, LayerSubCategoryBar> m_subCategories = new Dictionary<string, LayerSubCategoryBar>();
		private Dictionary<string, Sprite> m_categoryIcons;
		private List<LayerToggleBar> m_toggleBars = new List<LayerToggleBar>();
		//private Dictionary<AbstractLayer, LayerToggleBar> m_shownLayerBars = new Dictionary<AbstractLayer, LayerToggleBar>();

		private MenuBarToggle m_menuLayerToggle;

        protected void Start()
		{
			m_menuLayerToggle = InterfaceCanvas.Instance.menuBarLayers;

			// This is for the menu bar toggle so that it acts correctly
			m_menuLayerToggle.GetComponent<Toggle>().isOn = true;

			SetIcons();
		}

        private void OnDisable()
        {
            if (InterfaceCanvas.Instance.menuBarLayers.toggle.isOn)
            {
                InterfaceCanvas.Instance.menuBarLayers.toggle.isOn = false;
            }
			m_layerSelectWindow.SetActive(false);
        }


		public void AddLayerToInterface(AbstractLayer layer)
		{
			if (layer.FileName.StartsWith("_PLAYAREA") || !layer.Toggleable)
			{
				return;
			}

			string categoryName = LayerManager.Instance.MakeCategoryDisplayString(layer.Category);

			// Creating/Getting the group
			LayerCategoryBar categoryBar = null;
			if (!m_categories.TryGetValue(categoryName, out categoryBar))
			{
				LayerCategoryBar newCategoryGroup = Instantiate(m_categoryPrefab, m_categoryParent).GetComponent<LayerCategoryBar>();
				newCategoryGroup.SetContent(categoryName);
				m_categories.Add(categoryName, newCategoryGroup);
				categoryBar = newCategoryGroup;
			}

			if (!m_subCategories.ContainsKey(layer.SubCategory))
			{
				string subCategoryName = LayerManager.Instance.MakeCategoryDisplayString(layer.SubCategory);
				LayerSubCategoryBar newSubcategory = Instantiate(m_subcategoryPrefab, categoryBar.ContentParent).GetComponent<LayerSubCategoryBar>();
				m_subCategories.Add(layer.SubCategory, newSubcategory);
				newSubcategory.SetContent(subCategoryName, layer.SubCategory, GetIcon(layer.SubCategory), OnSubcategoryClick, m_subcategoryToggleGroup);
			}
		}

		void OnSubcategoryClick(bool a_value, string a_subcategory)
		{
			if (a_value)
			{
				m_layerSelectWindow.SetActive(true);
				List<AbstractLayer> layers = LayerManager.Instance.GetLayersInSubcategory(a_subcategory);
				int activeBars = 0;
				for(int i = 0; i < layers.Count; i++)
				{
					if (layers[i].Toggleable || Main.IsDeveloper)
					{
						if (activeBars < m_toggleBars.Count)
						{
							m_toggleBars[i].SetContent(layers[i]);
						}
						else
						{
							LayerToggleBar newLayerBar = Instantiate(m_layerPrefab, m_layerParent).GetComponent<LayerToggleBar>();
							newLayerBar.SetContent(layers[i]);
							m_toggleBars.Add(newLayerBar);
						}
						activeBars++;
					}

				}
				for(; activeBars < m_toggleBars.Count; activeBars++)
				{
					m_toggleBars[activeBars].gameObject.SetActive(false);
				}
			}
		}

		public Sprite GetIcon(string category)
		{
			if (m_categoryIcons.TryGetValue(category, out var result))
			{
				return result;
			}
			return null;
		}

		private void SetIcons()
		{
			m_categoryIcons = new Dictionary<string, Sprite>();

			if (m_icons != null)
			{
				for (int i = 0; i < m_icons.Count; i++)
				{
					m_categoryIcons.Add(m_icons[i].name, m_icons[i]);
				}
			}
			else
			{
				Debug.LogError("Icons for layer categories are not assigned on " + gameObject.name);
			}
		}

		//public void OnShowLayer(AbstractLayer layer)
		//{
		//	SetToggle(layer, true);
		//	InterfaceCanvas.Instance.activeLayers.AddLayer(layer);
		//}

		//public void OnHideLayer(AbstractLayer layer)
		//{
		//	SetToggle(layer, false);
		//	InterfaceCanvas.Instance.activeLayers.ToggleLayer(layer, false);
		//}

		public void SetLayerVisibilityLock(AbstractLayer layer, bool value)
		{
			InterfaceCanvas.Instance.activeLayers.SetLayerVisibilityLocked(layer, value);
		}
	}
}