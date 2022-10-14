using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_LayerSelect : AP_PopoutWindow
	{
		[SerializeField] GameObject m_catgoryPrefab;
		[SerializeField] GameObject m_subcatgoryPrefab;
		[SerializeField] GameObject m_layerPrefab;
		[SerializeField] Transform m_contentContainer;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;

		bool m_initialised;
		bool m_changed;
		bool m_ignoreCallback;
		HashSet<AbstractLayer> m_originalLayers;
		HashSet<AbstractLayer> m_currentLayers;
		Dictionary<AbstractLayer, AP_LayerSelectLayer> m_layerObjects;
		Dictionary<string, AP_LayerSelectSubcategory> m_subcategoryObjects;
		Dictionary<string, AP_LayerSelectCategory> m_categoryObjects;

		void Initialise()
		{
			m_initialised = true;

			m_confirmButton.onClick.AddListener(OnAccept);
			m_cancelButton.onClick.AddListener(TryClose);

			m_layerObjects = new Dictionary<AbstractLayer, AP_LayerSelectLayer>();
			m_subcategoryObjects = new Dictionary<string, AP_LayerSelectSubcategory>();
			m_categoryObjects = new Dictionary<string, AP_LayerSelectCategory>();

			foreach(AbstractLayer layer in LayerManager.Instance.GetAllLayers())
			{
				AP_LayerSelectSubcategory subcategory;
				if (!m_subcategoryObjects.TryGetValue(layer.Category, out subcategory))
				{
					AP_LayerSelectCategory category;
					if (!m_categoryObjects.TryGetValue(layer.Category, out category))
					{
						category = Instantiate(m_catgoryPrefab, m_contentContainer).GetComponent<AP_LayerSelectCategory>();
						category.Initialise(LayerManager.Instance.MakeCategoryDisplayString(layer.Category));
						m_categoryObjects.Add(layer.Category, category);
					}
					subcategory = Instantiate(m_subcatgoryPrefab, category.ContentContainer).GetComponent<AP_LayerSelectSubcategory>();
					subcategory.Initialise(layer.SubCategory, LayerManager.Instance.GetSubcategoryIcon(layer.SubCategory));
					m_subcategoryObjects.Add(layer.SubCategory, subcategory);
				}

				AP_LayerSelectLayer layerObj = Instantiate(m_subcatgoryPrefab, subcategory.ContentContainer).GetComponent<AP_LayerSelectLayer>();
				layerObj.Initialise(layer, OnLayerToggleChanged);
				m_layerObjects.Add(layer, layerObj);
			}
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle)
		{
			if (!m_initialised)
				Initialise();

			base.OpenToContent(a_content, a_toggle);

			m_changed = false;
			m_ignoreCallback = true;
			foreach(var kvp in m_layerObjects)
			{
				kvp.Value.SetValue(false);
			}

			m_originalLayers = new HashSet<AbstractLayer>();
			foreach(PlanLayer pl in a_content.PlanLayers)
			{
				m_originalLayers.Add(pl.BaseLayer);
				m_layerObjects[pl.BaseLayer].SetValue(true);
				//TODO: check dependent layers (set non interactable)
			}
			m_currentLayers = m_originalLayers;
			m_ignoreCallback = false;
		}

		void OnLayerToggleChanged(AbstractLayer a_layer, bool a_value)
		{
			if (m_ignoreCallback)
				return;

			m_changed = true;
			if (a_value)
			{
				m_currentLayers.Add(a_layer);
				//TODO: check dependent layers
			}
			else
			{
				m_currentLayers.Remove(a_layer);
				//TODO: check dependent layers
			}
		}

		void OnAccept()
		{
			//TODO
		}

		public override bool MayClose()
		{
			if(m_changed)
			{
				DialogBoxManager.instance.ConfirmationWindow("Discard layer changes?", "Are you sure you want to discard any changes made to what layers are in the plan?", null, Close);
				return false;
			}
			return true;
		}
	}
}
