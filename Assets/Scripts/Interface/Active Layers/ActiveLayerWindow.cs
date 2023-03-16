using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActiveLayerWindow : MonoBehaviour
	{
		[Header("Prefabs")]
		[SerializeField] Transform m_contentLocation;
		[SerializeField] GameObject m_activeLayerPrefab;

		[Header("Buttons")]
		[SerializeField] CustomButton m_clearAllButton;
		[SerializeField] CustomButton m_collapseAllButton;
		[SerializeField] CustomButton m_showHideTextButton;
		[SerializeField] TextMeshProUGUI m_collapseAllButtonText;
		[SerializeField] TextMeshProUGUI m_showHideTextText;

		[Header("Other")]
		[SerializeField] ScrollRect m_contentScrollRect;

		private Dictionary<AbstractLayer, ActiveLayer> m_activeLayers = new Dictionary<AbstractLayer, ActiveLayer>();
		private int m_expandedLayers = 0;
		private bool m_allTextHidden;

		private void Start()
		{
			LayerManager.Instance.m_onLayerVisibilityChanged += OnLayerVisibilityChanged;
			foreach(AbstractLayer layer in LayerManager.Instance.GetVisibleLayers())
			{
				ActiveLayer activeLayer = Instantiate(m_activeLayerPrefab, m_contentLocation).GetComponent<ActiveLayer>();
				activeLayer.SetLayerRepresenting(layer, m_allTextHidden);
				m_activeLayers.Add(layer, activeLayer);
			}

			m_collapseAllButton.onClick.AddListener(() =>
			{
				if (m_expandedLayers == 0)
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in m_activeLayers)
						kvp.Value.SetExpanded(true);
				}
				else
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in m_activeLayers)
						kvp.Value.SetExpanded(false);
					m_contentScrollRect.verticalNormalizedPosition = 1f;
				}
			});

			m_clearAllButton.onClick.AddListener(ClearAllActiveLayers);

			m_showHideTextButton.onClick.AddListener(() =>
			{
				if (m_allTextHidden)
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in m_activeLayers)
						kvp.Value.SetTextActive(true);
					AllTextHidden = false;
				}
				else
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in m_activeLayers)
						kvp.Value.SetTextActive(false);
					AllTextHidden = true;
				}
			});
		}

		private void OnDisable()
		{
			InterfaceCanvas.Instance.menuBarActiveLayers.toggle.isOn = false;
		}

		void OnLayerVisibilityChanged(AbstractLayer a_layer, bool a_visible)
		{
			if(m_activeLayers.TryGetValue(a_layer, out ActiveLayer activeLayer))
			{
				activeLayer.OnLayerVisibilityChanged(a_visible);
			}
			else if(a_visible)
			{
				ActiveLayer newActiveLayer = Instantiate(m_activeLayerPrefab, m_contentLocation).GetComponent<ActiveLayer>();
				newActiveLayer.SetLayerRepresenting(a_layer, m_allTextHidden);
				m_activeLayers.Add(a_layer, newActiveLayer);
			}
		}

		public void AddPinnedInvisibleLayer(AbstractLayer a_layer)
		{
			ActiveLayer newActiveLayer = Instantiate(m_activeLayerPrefab, m_contentLocation).GetComponent<ActiveLayer>();
			newActiveLayer.SetLayerRepresenting(a_layer, m_allTextHidden, true);
			m_activeLayers.Add(a_layer, newActiveLayer);
		}

		public void RemoveLayer(AbstractLayer layer)
		{
			if (m_activeLayers.TryGetValue(layer, out var activeLayer))
			{
				activeLayer.Destroy();
				m_activeLayers.Remove(layer);
			}
		}

		public void ClearAllActiveLayers()
		{
			StartCoroutine(CoroutineHideAllVisibleLayers());
			m_contentScrollRect.verticalNormalizedPosition = 1f;
		}

		private IEnumerator CoroutineHideAllVisibleLayers()
		{
			List<AbstractLayer> layers = m_activeLayers.Keys.ToList();
			Plan currentPlan = PlanManager.Instance.m_planViewing;
			foreach(AbstractLayer layer in layers)
			{
				if (layer.Toggleable && (currentPlan == null || !currentPlan.IsLayerpartOfPlan(layer)))
				{
					LayerManager.Instance.HideLayer(layer);
					yield return 0;
				}
			}

			yield return null;
		}

		public void LayerExpansionChanged(bool expanded)
		{
			if (expanded)
				m_expandedLayers++;
			else
				m_expandedLayers--;
			m_collapseAllButtonText.text = m_expandedLayers == 0 ? "Expand" : "Collapse";
		}

		public void TextShowingChanged(bool showing)
		{
			if (showing)
				AllTextHidden = false;       
		}

		public bool AllTextHidden
		{
			private set
			{
				m_allTextHidden = value;
				m_showHideTextText.text = m_allTextHidden ? "Show text" : "Hide text";
			}
			get
			{
				return m_allTextHidden;
			}
		}
	}
}
