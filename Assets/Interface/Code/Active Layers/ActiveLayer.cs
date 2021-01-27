using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ActiveLayer : MonoBehaviour {

	public TextMeshProUGUI layerName;
	public CustomToggle visibilityToggle, barToggle, layerTextToggle;
	public CustomButton closeButton;
	public CustomButton infoButton;
	public Image toggleCheckMark, textToggleCheckMark;
    public Sprite textToggleVisibleSprite, textToggleInvisibleSprite;
	public Transform contentLocation, collapseArrow;
	
	[HideInInspector]
	public AbstractLayer layerRepresenting;

	[Header("Prefabs")]
	public GameObject mapKeyPrefab;

	[Header("Content")]
	public List<MapKey> mapKeys;

	private void Start()
	{
		barToggle.onValueChanged.AddListener((b) => SetExpanded(b));
	}

	public void ShowCloseButton(bool show)
	{
		closeButton.gameObject.SetActive(show);
	}

	public void SetLayerRepresenting(AbstractLayer layer, bool forceTextHidden)
	{
		layerRepresenting = layer;
		layerName.text = string.IsNullOrEmpty(layer.ShortName) ? layer.FileName : layer.ShortName;
		CreateMapKeys();
        if (layerRepresenting.textInfo == null)
        {
            layerTextToggle.gameObject.SetActive(false);
        }
        else
        {
            if (forceTextHidden && layer.LayerTextVisible)
                layer.LayerTextVisible = false;
            layerTextToggle.isOn = layer.LayerTextVisible;
            textToggleCheckMark.sprite = layer.LayerTextVisible ? textToggleVisibleSprite : textToggleInvisibleSprite;
            layerTextToggle.onValueChanged.AddListener((value) =>
            {
                textToggleCheckMark.sprite = value ? textToggleVisibleSprite : textToggleInvisibleSprite;
                layerRepresenting.LayerTextVisible = value;
                InterfaceCanvas.Instance.activeLayers.TextShowingChanged(value);
            });
        }
    }

	private void CreateMapKeys()
	{
		mapKeys = new List<MapKey>();

		bool visibilityToggle = layerRepresenting.GetGeoType() != LayerManager.GeoType.raster;
		
		foreach (EntityType entityType in layerRepresenting.GetEntityTypesSortedByKey())
		{
			Texture2D pattern = MaterialManager.GetPatternOrDefault(entityType.DrawSettings.PolygonPatternName);

			Color color = entityType.DrawSettings.PolygonColor;

			if (layerRepresenting.GetGeoType() == LayerManager.GeoType.line)
			{
				color = entityType.DrawSettings.LineColor;
			}
			else if (layerRepresenting.GetGeoType() == LayerManager.GeoType.point)
			{
				color = entityType.DrawSettings.PointColor;
			} 
			else if (layerRepresenting.GetGeoType() == LayerManager.GeoType.raster)
			{
				pattern = MaterialManager.GetPatternOrDefault(((RasterLayer)layerRepresenting).rasterObject.layer_raster_pattern);
			}

			CreateMapKey(entityType, color, pattern, visibilityToggle);
		}
	}

	private void CreateMapKey(EntityType type, Color color, Texture2D pattern, bool visibilityToggle)
	{
		int key = layerRepresenting.GetEntityTypeKey(type);

		string mapKeyName = type.Name;
		if (TeamManager.IsGameMaster)
			mapKeyName += " (" + key + ")";		
		
		// Instantiate prefab
		GameObject go = Instantiate(mapKeyPrefab);
		go.transform.SetParent(contentLocation, false);

		// Store component
		MapKey mapKey = go.GetComponent<MapKey>();
		mapKeys.Add(mapKey);

		//Set properties
		//mapKey.areaKey.texture = pattern;
		//mapKey.areaKey.color = color;
		//mapKey.lineKey.color = color;
		//mapKey.pointKey.color = color;
		mapKey.label.text = mapKeyName;
		mapKey.layer = layerRepresenting;
		mapKey.entityType = type;
		mapKey.ID = key;
		if (!visibilityToggle)
			mapKey.DisableVisibilityToggle();

		if (layerRepresenting.GetGeoType() == LayerManager.GeoType.polygon)
		{
            mapKey.areaKey.texture = pattern;
            mapKey.areaKey.color = color;
            
			mapKey.outlineKey.transform.gameObject.SetActive(true);
			Color outlineColor = type.DrawSettings.LineColor;
			outlineColor.a = 1.0f; //Force Alpha to 1 as it is with the outline line rendering.
			mapKey.outlineKey.color = outlineColor;
		}
		else if (layerRepresenting.GetGeoType() == LayerManager.GeoType.line)
		{
			mapKey.areaKey.transform.parent.parent.gameObject.SetActive(false);
			mapKey.lineKey.transform.parent.gameObject.SetActive(true);
			mapKey.pointKey.transform.parent.gameObject.SetActive(false);
			mapKey.outlineKey.transform.gameObject.SetActive(false);

		    mapKey.lineKey.color = color;
            mapKey.lineKey.sprite = InterfaceCanvas.Instance.activeLayerLineSprites[(int)type.DrawSettings.LinePatternType];
        }
        else if (layerRepresenting.GetGeoType() == LayerManager.GeoType.point)
		{
			mapKey.areaKey.transform.parent.parent.gameObject.SetActive(false);
			mapKey.lineKey.transform.parent.gameObject.SetActive(false);
			mapKey.pointKey.transform.parent.gameObject.SetActive(true);
			mapKey.outlineKey.transform.gameObject.SetActive(false);

            mapKey.pointKey.sprite = type.DrawSettings.PointSprite;
		    mapKey.pointKey.color = color;
        }
        else
		{
			mapKey.outlineKey.transform.gameObject.SetActive(false);

			mapKey.areaKey.color = color;
			mapKey.areaKey.texture = pattern;
		}
	}

	public void SetExpanded(bool value)
	{
		if (contentLocation.gameObject.activeInHierarchy == value)
			return;

		contentLocation.gameObject.SetActive(value);
		Vector3 oldRotation = collapseArrow.eulerAngles;
		collapseArrow.eulerAngles = !value ? new Vector3(oldRotation.x, oldRotation.y, 90f) : new Vector3(oldRotation.x, oldRotation.y, 0f);
		InterfaceCanvas.Instance.activeLayers.LayerExpansionChanged(value);
    }

    public void SetVisibilityLocked(bool value)
    {
        visibilityToggle.interactable = !value;
        foreach (MapKey mapKey in mapKeys)
            mapKey.SetInteractable(!value);
    }

	public void Destroy()
	{
        if (contentLocation.gameObject.activeInHierarchy)
        {
            InterfaceCanvas.Instance.activeLayers.LayerExpansionChanged(false);
            InterfaceCanvas.Instance.activeLayers.TextShowingChanged(false);
        }
		Destroy(gameObject);
	}
}
