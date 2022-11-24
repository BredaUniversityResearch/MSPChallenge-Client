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
		[HideInInspector] Transform contentLocation;
		[HideInInspector] GameObject activeLayerPrefab;

		[Header("Buttons")]
		[HideInInspector] CustomButton clearAllButton;
		[HideInInspector] CustomButton collapseAllButton;
		[HideInInspector] CustomButton showHideTextButton;
		[HideInInspector] TextMeshProUGUI collapseAllButtonText;
		[HideInInspector] TextMeshProUGUI showHideTextText;

		[Header("Other")]
		[HideInInspector] ScrollRect contentScrollRect;

		private Dictionary<AbstractLayer, ActiveLayer> activeLayers = new Dictionary<AbstractLayer, ActiveLayer>();
		private int expandedLayers = 0;
		private bool allTextHidden;

		private void Start()
		{
			collapseAllButton.onClick.AddListener(() =>
			{
				if (expandedLayers == 0)
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in activeLayers)
						kvp.Value.SetExpanded(true);
				}
				else
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in activeLayers)
						kvp.Value.SetExpanded(false);
					contentScrollRect.verticalNormalizedPosition = 1f;
				}
			});

			clearAllButton.onClick.AddListener(ClearAllActiveLayers);

			showHideTextButton.onClick.AddListener(() =>
			{
				if (allTextHidden)
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in activeLayers)
						kvp.Value.layerTextToggle.isOn = true;
					AllTextHidden = false;
				}
				else
				{
					foreach (KeyValuePair<AbstractLayer, ActiveLayer> kvp in activeLayers)
						kvp.Value.layerTextToggle.isOn = false;
					AllTextHidden = true;
				}
			});
		}

		private void OnDisable()
		{
			if (InterfaceCanvas.Instance.menuBarActiveLayers.toggle.isOn)
			{
				InterfaceCanvas.Instance.menuBarActiveLayers.toggle.isOn = false;
			}
		}

		public void AddLayer(AbstractLayer layer, bool addEnabled = true)
		{
			if (activeLayers.ContainsKey(layer))
			{
				if (activeLayers[layer] && activeLayers[layer].gameObject)
				{
					activeLayers[layer].gameObject.SetActive(true);
					if(addEnabled)
						activeLayers[layer].visibilityToggle.isOn = true;
				}
			}
			else
			{
				ActiveLayer activeLayer = Instantiate(activeLayerPrefab, contentLocation).GetComponent<ActiveLayer>();
				activeLayer.SetLayerRepresenting(layer, allTextHidden);
				activeLayer.visibilityToggle.isOn = addEnabled;
				activeLayers.Add(layer, activeLayer);
				activeLayer.closeButton.onClick.AddListener(() => 
				{
					if (PlanManager.Instance.planViewing == null || !PlanManager.Instance.planViewing.IsLayerpartOfPlan(activeLayer.layerRepresenting))
					{
						LayerManager.Instance.HideLayer(activeLayer.layerRepresenting);
						RemoveLayer(activeLayer.layerRepresenting);
					}
				});

				activeLayer.visibilityToggle.onValueChanged.AddListener((value) =>
				{
					if (InterfaceCanvas.Instance.ignoreLayerToggleCallback)
						return;

					InterfaceCanvas.Instance.ignoreLayerToggleCallback = true;
					if (value)
					{
						LayerManager.Instance.ShowLayer(activeLayer.layerRepresenting);
					}
					else
					{
						LayerManager.Instance.HideLayer(activeLayer.layerRepresenting);
					}
					InterfaceCanvas.Instance.ignoreLayerToggleCallback = false;
				});
			}
		}

		public void RemoveLayer(AbstractLayer layer)
		{
			if (activeLayers.ContainsKey(layer))
			{
				activeLayers[layer].Destroy();
				activeLayers.Remove(layer);
			}
		}

		public void ToggleLayer(AbstractLayer layer, bool value)
		{
			activeLayers[layer].visibilityToggle.isOn = value;
		}

		public void SetLayerVisibilityLocked(AbstractLayer layer, bool value)
		{
			if(activeLayers.TryGetValue(layer, out var result))
				result.SetVisibilityLocked(value);
		}

		public void ClearAllActiveLayers()
		{
			StartCoroutine(CoroutineHideAllVisibleLayers());
			contentScrollRect.verticalNormalizedPosition = 1f;
		}

		private IEnumerator CoroutineHideAllVisibleLayers()
		{
			List<AbstractLayer> layers = activeLayers.Keys.ToList();
			Plan currentPlan = PlanManager.Instance.planViewing;
			foreach(AbstractLayer layer in layers)
			{
				if (layer.Toggleable && (currentPlan == null || !currentPlan.IsLayerpartOfPlan(layer)))
				{
					LayerManager.Instance.HideLayer(layer);
					RemoveLayer(layer);
					yield return 0;
				}
			}

			yield return null;
		}

		public void LayerExpansionChanged(bool expanded)
		{
			if (expanded)
				expandedLayers++;
			else
				expandedLayers--;
			collapseAllButtonText.text = expandedLayers == 0 ? "Expand all" : "Collapse all";
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
				allTextHidden = value;
				showHideTextText.text = allTextHidden ? "Show all" : "Hide all";
			}
			get
			{
				return allTextHidden;
			}
		}
	}
}
