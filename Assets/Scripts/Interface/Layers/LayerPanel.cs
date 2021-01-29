using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class LayerPanel : MonoBehaviour
{
    public Transform layerGroupLocation;

    [Header("Prefabs")]
    public GameObject layerGroupPrefab;

    [Header("Content")]
    public List<LayerCategoryGroup> layerGroup = new List<LayerCategoryGroup>();

    LayerInterface layerInterface;

    void Start()
    {
        layerInterface = this.GetComponent<LayerInterface>();
    }

    private void OnDisable()
    {
        if (InterfaceCanvas.Instance.menuBarLayers.toggle.isOn)
        {
            InterfaceCanvas.Instance.menuBarLayers.toggle.isOn = false;
        }
    }

    /// <summary>
    /// Creates a new layer group
    /// </summary>
    public LayerCategoryGroup CreateLayerGroup()
    {
        // Instantiate prefab
        GameObject go = Instantiate(layerGroupPrefab);

        // Store component
        LayerCategoryGroup categoryGroup = go.GetComponent<LayerCategoryGroup>();

        // Add to list
        layerGroup.Add(categoryGroup);

        // Assign parent
        go.transform.SetParent(layerGroupLocation, false);

        // Set up references
        categoryGroup.layerPanel = this;
		
        return categoryGroup;
    }

    /// <summary>
    /// Properly destroys a layer group
    /// </summary>
    public void DestroyLayerGroup(LayerCategoryGroup categoryGroup)
    {
        layerGroup.Remove(categoryGroup);
        Destroy(categoryGroup.gameObject);
    }

    public void DisableLayerSelect(bool b)
    {
        if (!b)
        {
            InterfaceCanvas.Instance.layerSelect.gameObject.SetActive(b);
            layerInterface.NotifyLayerSelectClosing();
        }
    }
}
