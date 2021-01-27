using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using Networking;
using TMPro;

public class PropertiesWindow : MonoBehaviour
{
	private const float PROPERTY_WINDOW_WIDTH = 300.0f;
	private const float PROPERTY_WINDOW_DEFAULT_HEIGHT = 150.0f;
	
	public GenericWindow window;
	public TextMeshProUGUI windowName;
	public Image layerIcon;

	[Header("Icons")]
	public Sprite countryIcon;
	public Sprite gridIcon, capacityIcon, usedCapacityIcon, locationIcon, areaIcon, pointsIcon;

	[Header("Content locations")]
	public GenericContent baseDataParent;
	public GenericContent entityTypeParent, otherInfoParent, debugInfoParent;

	private List<GenericEntry> genericEntries;

	[SerializeField]
	private ValueConversionCollection valueConversionCollection = null;

	private void Start()
	{
		gameObject.SetActive(false);
	}

	public void ShowPropertiesWindow(SubEntity subEntity, Vector3 worldSamplePosition, Vector3 windowPosition)
	{
		bool reposition = !gameObject.activeInHierarchy;
		gameObject.SetActive(true);

		//Setup data
		Entity entity = subEntity.Entity;
		windowName.text = entity.Layer.GetShortName();
		layerIcon.sprite = LayerInterface.GetIconStatic(entity.Layer.SubCategory);

        //Base data
        baseDataParent.Initialise();
		baseDataParent.DestroyAllContent();

		Team team = TeamManager.FindTeamByID(entity.Country);
		AddEntry(baseDataParent, "Name", entity.name, countryIcon, team == null ? Color.white : team.color);
		AddEntry(baseDataParent, "X", (subEntity.BoundingBox.center.x * 1000).FormatAsCoordinateText(), locationIcon, Color.white);
		AddEntry(baseDataParent, "Y", (subEntity.BoundingBox.center.y * 1000).FormatAsCoordinateText(), locationIcon, Color.white);

		//Geometry type specific base data
		if (entity.Layer.GetGeoType() == LayerManager.GeoType.polygon)
		{
			PolygonSubEntity polygonEntity = (PolygonSubEntity)subEntity;
			AddEntry(baseDataParent, "Area", polygonEntity.SurfaceAreaSqrKm.ToString("0.00") + " km²", areaIcon, Color.white);
			AddEntry(baseDataParent, "Points", polygonEntity.GetTotalPointCount().ToString(), pointsIcon, Color.white);
		}
		else if (entity.Layer.GetGeoType() == LayerManager.GeoType.line)
		{
			LineStringSubEntity lineEntity = (LineStringSubEntity)subEntity;
			AddEntry(baseDataParent, "Length", lineEntity.LineLengthKm.ToString("0.00") + " km", areaIcon, Color.white);
			AddEntry(baseDataParent, "Points", lineEntity.GetPointCount().ToString(), pointsIcon, Color.white);
		}
		if (entity.Layer.IsEnergyLayer())
		{
			IEnergyDataHolder data = (IEnergyDataHolder)subEntity;
			AddEntry(baseDataParent, "Max Capacity", valueConversionCollection.ConvertUnit(data.Capacity, ValueConversionCollection.UNIT_WATT).FormatAsString(), capacityIcon, Color.white);
			AddEntry(baseDataParent, "Used Capacity", valueConversionCollection.ConvertUnit(data.UsedCapacity, ValueConversionCollection.UNIT_WATT).FormatAsString(), usedCapacityIcon, Color.white);
			if(data.CurrentGrid == null)
				AddEntry(baseDataParent, "Last Run Grid", data.LastRunGrid == null ? "-" : data.LastRunGrid.name, gridIcon, Color.white);
			else
				AddEntry(baseDataParent, "Current Grid", data.CurrentGrid.name, gridIcon, Color.white);
		}

		//Entity type information
		entityTypeParent.DestroyAllContent();
        entityTypeParent.Initialise();
        List<EntityType> entityTypes;
		RasterLayer rasterLayer = entity.Layer as RasterLayer;
		if (rasterLayer != null)
		{
			entityTypes = new List<EntityType>(1) {rasterLayer.GetEntityTypeForRasterAt(worldSamplePosition)};
		}
		else
		{
			entityTypes = entity.EntityTypes;
		}
		for (int i = 0; i < entityTypes.Count; i++)
		{
			//string URL = "http://www.google.com/search?q=" + entity.EntityTypes[i].Name.Replace(' ', '+');
			if (!string.IsNullOrEmpty(entityTypes[i].media))
			{
				var iCopy = i;
				AddEntry(entityTypeParent, entityTypes[i].Name, "", () => 
				{
					Vector3[] corners = new Vector3[4];
					window.windowTransform.GetWorldCorners(corners);

					string mediaUrl = MediaUrl.Parse(entityTypes[iCopy].media);
					InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(mediaUrl, new Vector3(corners[2].x, corners[2].y - Screen.height, 0));
				});
			}
			else
			{
				AddEntry(entityTypeParent, entityTypes[i].Name, "");
			}
		}

		//Other information
		otherInfoParent.DestroyAllContent();
        otherInfoParent.Initialise();
        genericEntries = new List<GenericEntry>();
		if (entity.metaData.Count > 0)
		{
            otherInfoParent.gameObject.SetActive(true);

            foreach (var kvp in entity.metaData)
			{
				EntityPropertyMetaData propertyMeta = entity.Layer.FindPropertyMetaDataByName(kvp.Key);
				if ((propertyMeta == null || propertyMeta.Enabled) || Main.IsDeveloper)
				{
					string propertyDisplayName = (propertyMeta != null && !string.IsNullOrEmpty(propertyMeta.DisplayName)) ? propertyMeta.DisplayName : kvp.Key;
					string value = propertyMeta != null && !string.IsNullOrEmpty(propertyMeta.Unit) ? entity.GetMetaData(kvp.Key) + " " + propertyMeta.Unit : entity.GetMetaData(kvp.Key);
					genericEntries.Add(AddEntry(otherInfoParent, propertyDisplayName, value));
				}
			}
        }
        else
            otherInfoParent.gameObject.SetActive(false);

        //Debug information
        if (Main.IsDeveloper)
		{
			debugInfoParent.gameObject.SetActive(true);
			debugInfoParent.DestroyAllContent();
            debugInfoParent.Initialise();
            AddEntry(debugInfoParent, "MSP ID", subEntity.GetMspID().ToString());
			AddEntry(debugInfoParent, "Persistent ID", subEntity.GetPersistentID().ToString());
			AddEntry(debugInfoParent, "Database ID", subEntity.GetDatabaseID().ToString());
		}
		else
			debugInfoParent.gameObject.SetActive(false);

		//Update window position
		if (reposition)
		{
			StartCoroutine(RepositionOnFrameEnd(windowPosition));
		}

		window.transform.SetAsLastSibling();
	}

	IEnumerator RepositionOnFrameEnd(Vector3 position)
	{
		yield return new WaitForEndOfFrame();

		Rect rect = window.windowTransform.rect;
		float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
		window.SetPosition(new Vector3(
			Mathf.Clamp(position.x / scale, 0f, (Screen.width - (rect.width * scale)) / scale),
			Mathf.Clamp(position.y / scale, (-Screen.height + (rect.height * scale)) / scale, 0f),
			position.z));
	}

	//private void SetupVideoWindowButton(GenericWindow window, string layerName, Entity entity)
	//{
	//	bool hasVideo = PropertyWindowVideoAssigner.instance.HasVideo(layerName);
	//	window.ShowEditButton(hasVideo);
	//	if (hasVideo)
	//	{
	//		Image editBtnImage = window.editButton.gameObject.transform.parent.Find("Icon").GetComponent<Image>();

	//		editBtnImage.sprite = InterfaceCanvas.instance.playLSpr;
	//		window.editButton.onClick.AddListener(() =>
	//		{
	//			if (videoWindow != null)
	//			{
	//				editBtnImage.sprite = InterfaceCanvas.instance.playRSpr;
	//				videoWindow.Destroy();
	//			}
	//			else
	//			{
	//				editBtnImage.sprite = InterfaceCanvas.instance.playLSpr;
	//				videoWindow = CreateVideoWindow(window, layerName, "VideoWindow", new Vector2(PROPERTY_WINDOW_WIDTH, PROPERTY_WINDOW_DEFAULT_HEIGHT), new Vector2(600.0f, 300.0f), entity);
	//			}
	//		});

	//		Create3DScene(layerName, window, PROPERTY_WINDOW_DEFAULT_HEIGHT, entity);
	//		CreateVideoWindow(layerName, window, PROPERTY_WINDOW_DEFAULT_HEIGHT, PROPERTY_WINDOW_WIDTH, entity);
	//	}
	//}

	//GenericWindow CreateVideoWindow(GenericWindow window, string videoName, string windowName, Vector2 windowSize, Vector2 resizeSize, Entity entity)
	//{
	//	MovieTexture tMovieTex = new MovieTexture();
	//	if (PropertyWindowVideoAssigner.instance.GetVideo(videoName, ref tMovieTex))
	//	{
	//		GenericWindow tVideoWindow = UIManager.GetInterfaceCanvas().CreateGenericWindow(windowName);
	//		if (tVideoWindow.gameObject.GetComponent<VerticalLayoutGroup>())
	//		{
	//			MonoBehaviour.DestroyImmediate(tVideoWindow.gameObject.GetComponent<VerticalLayoutGroup>());
	//			tVideoWindow.gameObject.AddComponent<HorizontalLayoutGroup>();
	//		}

	//		tVideoWindow.ShowEditButton(true);
	//		tVideoWindow.editButton.gameObject.transform.parent.Find("Icon").GetComponent<Image>().sprite = InterfaceCanvas.instance.cameraSpr;
	//		//Resize property Window
	//		tVideoWindow.editButton.onClick.AddListener(() =>
	//		{
	//			tVideoWindow.Destroy();
	//			videoWindow = CreateVideoWindow(window, videoName, windowName, resizeSize, windowSize, entity);
	//		});

	//		tVideoWindow.transform.SetParent(window.transform, false);
	//		//videoWindow.transform.position = position + Vector3.up * 600.0f + Vector3.right * windowWidth;
	//		if (tVideoWindow.GetComponent<Draggable>())
	//		{
	//			tVideoWindow.GetComponent<Draggable>().enabled = false;
	//		}

	//		GenericContent Window3D = tVideoWindow.CreateContentWindow(false);
	//		Window3D.SetPrefHeight(windowSize.y);
	//		GenericEntry Window3DEntry = AddEntry(Window3D, "", tMovieTex);
	//		Window3DEntry.gameObject.GetComponentInParent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

	//		VerticalLayoutGroup tVerticalLayout = Window3DEntry.gameObject.GetComponentInParent<VerticalLayoutGroup>();
	//		tVerticalLayout.childForceExpandWidth = true;
	//		tVerticalLayout.childForceExpandHeight = true;
	//		tVerticalLayout.childAlignment = TextAnchor.MiddleCenter;
	//		LayoutElement tLayoutElement = Window3DEntry.transform.GetComponent<LayoutElement>();
	//		tLayoutElement.minWidth = windowSize.x; //windowHeight;
	//		tLayoutElement.minHeight = windowSize.y; //windowHeight;
	//		tMovieTex.loop = true;
	//		tMovieTex.Play();

	//		return tVideoWindow;
	//	}
	//	return null;
	//}

	public void Close()
	{
		gameObject.SetActive(false);
	}

	private void closeWindowDelegate(Entity entity)
	{
		//Delete created rendertexture
		SceneCaptureManager.instance.CloseSceneRenderer(entity.Layer.GetShortName());

	}

	private GenericEntry AddEntry(GenericContent content, string entryName, string entryContent, Sprite icon, Color color)
	{
		GenericEntry entry = content.CreateEntry<string>(entryName, entryContent, icon, color);

		return entry;
	}

	private GenericEntry AddEntry(GenericContent content, string entryName, string entryContent)
	{
		GenericEntry entry = content.CreateEntry<string>(entryName, entryContent);

		return entry;
	}

	private GenericEntry AddEntry(GenericContent content, string entryName, Texture entryContent)
	{
		GenericEntry entry = content.CreateEntry<Texture>(entryName, entryContent);

		return entry;
	}

	private GenericEntry AddEntry(GenericContent content, string entryName, string entryContent, UnityAction callBack)
	{
		GenericEntry entry = content.CreateEntry<string>(entryName, entryContent, callBack);

		return entry;
	}

	//private void Create3DScene(string windowName, GenericWindow window, float windowHeight, Entity entity)
	//{
	//	//Creates a 3D scene window in the top of the window
	//	RenderTexture tTex = new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32);
	//	if (SceneCaptureManager.instance.OpenSceneRenderer(windowName, ref tTex))
	//	{
	//		GenericContent Window3D = window.CreateContentWindow(false);
	//		Window3D.SetPrefHeight(windowHeight);

	//		GenericEntry Window3DEntry = AddEntry(Window3D, "", tTex);
	//		Window3DEntry.gameObject.GetComponentInParent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
	//		VerticalLayoutGroup tVerticalLayout = Window3DEntry.gameObject.GetComponentInParent<VerticalLayoutGroup>();
	//		tVerticalLayout.childForceExpandWidth = false;
	//		tVerticalLayout.childForceExpandHeight = false;
	//		tVerticalLayout.childAlignment = TextAnchor.MiddleCenter;
	//		LayoutElement tLayoutElement = Window3DEntry.transform.GetComponent<LayoutElement>();
	//		tLayoutElement.minWidth = windowHeight;
	//	}
	//}

	//private void CreateVideoWindow(string windowName, GenericWindow window, float windowHeight, float windowWidth, Entity entity)
	//{
	//	if (displayVideoInNewWindow)
	//	{
	//		videoWindow = CreateVideoWindow(window, windowName, "VideoWindow", new Vector2(PROPERTY_WINDOW_WIDTH, PROPERTY_WINDOW_DEFAULT_HEIGHT), new Vector2(600.0f, 300.0f), entity);
	//	}
	//	else
	//	{
	//		//Creates a video window in the top of the window
	//		MovieTexture tMovieTex = new MovieTexture();
	//		if (PropertyWindowVideoAssigner.instance.GetVideo(windowName, ref tMovieTex))
	//		{
	//			GenericContent Window3D = window.CreateContentWindow(false);
	//			Window3D.SetPrefHeight(windowHeight);

	//			GenericEntry Window3DEntry = AddEntry(Window3D, "", tMovieTex);
	//			Window3DEntry.gameObject.GetComponentInParent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
	//			VerticalLayoutGroup tVerticalLayout = Window3DEntry.gameObject.GetComponentInParent<VerticalLayoutGroup>();
	//			tVerticalLayout.childForceExpandWidth = false;
	//			tVerticalLayout.childForceExpandHeight = false;
	//			tVerticalLayout.childAlignment = TextAnchor.MiddleCenter;
	//			LayoutElement tLayoutElement = Window3DEntry.transform.GetComponent<LayoutElement>();
	//			tLayoutElement.minWidth = windowWidth; //windowHeight;

	//			tMovieTex.loop = true;
	//			tMovieTex.Play();
	//		}
	//	}
	//}
}
