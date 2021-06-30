using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using Networking;
using Object = UnityEngine.Object;
using TMPro;

public class ActiveLayerWindow : MonoBehaviour
{
    [Header("Children")]
	public Transform contentLocation;
	public Object activeLayerPrefab;

    [Header("Buttons")]
    public CustomButton clearAllButton;
    public CustomButton collapseAllButton;
    public CustomButton showHideTextButton;
	public TextMeshProUGUI collapseAllButtonText, showHideTextText;
    public Image showHideTextIcon;
    public Sprite hideTextSprite, showTextSprite;

    [Header("Other")]
    public ResizeHandle resizeHandle;
	public ScrollRect contentScrollRect;
	public LayoutElement rescaleLayoutElement;

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
			GameObject go = Instantiate(activeLayerPrefab) as GameObject;
			go.transform.SetParent(contentLocation, false);
			ActiveLayer activeLayer = go.GetComponent<ActiveLayer>();
			activeLayer.SetLayerRepresenting(layer, allTextHidden);
            activeLayer.visibilityToggle.isOn = addEnabled;
			activeLayers.Add(layer, activeLayer);
            activeLayer.closeButton.onClick.AddListener(() => 
			{
				if (PlanManager.planViewing == null || !PlanManager.planViewing.IsLayerpartOfPlan(activeLayer.layerRepresenting))
				{
					LayerManager.HideLayer(activeLayer.layerRepresenting);
					RemoveLayer(activeLayer.layerRepresenting);
				}
			});

            if (!String.IsNullOrEmpty(activeLayer.layerRepresenting.Media))
            {
                activeLayer.infoButton.onClick.AddListener(() =>
                {
                    string mediaUrl = MediaUrl.Parse(activeLayer.layerRepresenting.Media);
                    InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(mediaUrl, new Vector3(Screen.width/2f, Screen.height/2f, 0));
                });
            }
            else
                activeLayer.infoButton.gameObject.SetActive(false);

            activeLayer.visibilityToggle.onValueChanged.AddListener((value) =>
			{
				if (UIManager.ignoreLayerToggleCallback)
					return;

				UIManager.ignoreLayerToggleCallback = true;
				if (value)
				{
					LayerManager.ShowLayer(activeLayer.layerRepresenting);
				}
				else
				{
					LayerManager.HideLayer(activeLayer.layerRepresenting);
				}
				UIManager.ignoreLayerToggleCallback = false;
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
		Plan currentPlan = PlanManager.planViewing;
		foreach(AbstractLayer layer in layers)
		{
			if (layer.Toggleable && (currentPlan == null || !currentPlan.IsLayerpartOfPlan(layer)))
			{
				LayerManager.HideLayer(layer);
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
            showHideTextIcon.sprite = allTextHidden ? hideTextSprite : showTextSprite;
        }
        get
        {
            return allTextHidden;
        }
    }
}
