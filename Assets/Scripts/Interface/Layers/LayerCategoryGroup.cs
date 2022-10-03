using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerCategoryGroup : MonoBehaviour
	{

		//public LayerPanel layerPanel;
		[SerializeField] Transform m_contentParent;
		[SerializeField] TextMeshProUGUI m_title;
        [SerializeField] GameObject m_layersCategoryPrefab;

        Dictionary<string, LayerSubCategory> layerButtons = new Dictionary<string, LayerSubCategory>();

        public Transform ContentParent => m_contentParent;
        public TextMeshProUGUI Title => m_title;

		public void SetContent(string text)
		{
			m_title.text = text;
		}

        public LayerSubCategory CreateLayerButton(string subCategory)
        {
            // Instantiate prefab
            GameObject go = Instantiate(m_layersCategoryPrefab, m_contentParent);

            // Store component
            LayerSubCategory button = go.GetComponent<LayerSubCategory>();

            // Add to list
            layerButtons.Add(subCategory, button);

            return button;
        }
    }
}
