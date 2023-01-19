using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LayerInterface : MonoBehaviour
	{
		[SerializeField] private GameObject m_subcategoryPrefab;
        [SerializeField] private GameObject m_categoryPrefab;
        [SerializeField] private GameObject m_layerPrefab;

        [SerializeField] private Transform m_categoryParent;
        [SerializeField] private VerticalLayoutGroup m_categoryLayoutGroup;
        [SerializeField] private Transform m_layerParent;
        [SerializeField] private GameObject m_layerSelectWindow;
        [SerializeField] private TextMeshProUGUI m_layerSelectCategoryText;
		[SerializeField] private ToggleGroup m_subcategoryToggleGroup;
		[SerializeField] SearchBar m_searchBar; 

        private Dictionary<string, LayerCategoryBar> m_categories = new Dictionary<string, LayerCategoryBar>();
		private Dictionary<string, LayerSubCategoryBar> m_subCategories = new Dictionary<string, LayerSubCategoryBar>();
		private List<LayerToggleBar> m_toggleBars = new List<LayerToggleBar>();

        protected void Start()
		{
			InterfaceCanvas.Instance.menuBarLayers.toggle.isOn = gameObject.activeInHierarchy;
			m_searchBar.m_ontextChange += OnSearchbarChanged;
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
			if (!layer.Toggleable)
			{
				return;
			}

			string categoryName = LayerManager.Instance.MakeCategoryDisplayString(layer.Category);

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
				newSubcategory.SetContent(subCategoryName, layer.SubCategory, LayerManager.Instance.GetSubcategoryIcon(layer.SubCategory), OnSubcategoryClick, m_subcategoryToggleGroup);
			}
			//m_searchBarContainer.SetAsLastSibling();
		}

		void OnSubcategoryClick(bool a_value, string a_subcategory)
		{
			if (a_value)
			{
				m_layerSelectWindow.SetActive(true);
				m_layerSelectCategoryText.text = LayerManager.Instance.MakeCategoryDisplayString(a_subcategory);
				List<AbstractLayer> layers = LayerManager.Instance.GetLayersInSubcategory(a_subcategory);
				int activeBars = 0;
				for(int i = 0; i < layers.Count; i++)
				{
					if (layers[i].Toggleable /*|| Main.IsDeveloper*/)
					{
						SetOrCreateLayerEntry(layers[i], activeBars, m_layerParent);
						activeBars++;
					}

				}
				for(; activeBars < m_toggleBars.Count; activeBars++)
				{
					m_toggleBars[activeBars].gameObject.SetActive(false);
				}
			}
			else
			{
				m_layerSelectWindow.SetActive(false);
			}
		}

		void OnSearchbarChanged(string a_value)
		{
			if(string.IsNullOrEmpty(a_value))
			{
				foreach (var kvp in m_categories)
				{
					kvp.Value.gameObject.SetActive(true);
				}
				foreach (var bar in m_toggleBars)
				{
					bar.gameObject.SetActive(false);
				}
				m_categoryLayoutGroup.padding = new RectOffset(0,0,0,0);
			}
			else
			{
				ForceSubcategoriesClosed();
				foreach (var kvp in m_categories)
				{
					kvp.Value.gameObject.SetActive(false);
				}
				int activeBars = 0;
				foreach(AbstractLayer layer in LayerManager.Instance.GetAllLayers())
				{
					if (layer.Toggleable && layer.ShortName.IndexOf(a_value, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						SetOrCreateLayerEntry(layer, activeBars, m_categoryParent);
						activeBars++;
					}

				}
				for (; activeBars < m_toggleBars.Count; activeBars++)
				{
					m_toggleBars[activeBars].gameObject.SetActive(false);
				}
				m_categoryLayoutGroup.padding = new RectOffset(0, 0, 6, 6);
			}
		}

		//Called from layer select window close button
		public void ForceSubcategoriesClosed()
		{
			m_subcategoryToggleGroup.SetAllTogglesOff();
		}

		void SetOrCreateLayerEntry(AbstractLayer a_layer, int a_index, Transform a_parent)
		{
			if (a_index < m_toggleBars.Count)
			{
				m_toggleBars[a_index].SetContent(a_layer);
				m_toggleBars[a_index].transform.SetParent(a_parent);
			}
			else
			{
				LayerToggleBar newLayerBar = Instantiate(m_layerPrefab, a_parent).GetComponent<LayerToggleBar>();
				newLayerBar.SetContent(a_layer);
				m_toggleBars.Add(newLayerBar);
			}
		}
	}
}