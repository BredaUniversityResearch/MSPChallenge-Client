#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using KPI;
using System.Diagnostics;

public class LayerImporter
{
    public delegate void DoneImporting();
    public static event DoneImporting OnDoneImporting;

	static int importedLayers;
	static int expectedLayers;
	static Stopwatch stopWatch;

	public static bool IsCurrentlyImportingLayers 
	{ 
		get; 
		private set; 
	}

    public static void ImportLayerMetaData()
    {
		if (!Main.IsDeveloper)
		{
			//Force the loading screen active so we don't show a single frame of uglyness.
			InterfaceCanvas.Instance.loadingScreen.ShowHideLoadScreen(true);
			InterfaceCanvas.Instance.loadingScreen.SetLoadingBarPercentage(0.0f);
			InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("Layer Meta Data");
		}

		NetworkForm form = new NetworkForm();
		form.AddField("user", TeamManager.CurrentSessionID.ToString());
		ServerCommunication.DoRequest<List<LayerMeta>>(Server.LayerMeta(), form, handleImportLayerMetaCallback);
    }

    private static void handleImportLayerMetaCallback(List<LayerMeta> layerMeta)
    {
		//Load layers
		LayerInfo.Load(layerMeta);
		if (Main.IsDeveloper)
		{
			LayerPickerUI.CreateUI();
		}
		else
		{
			GameObject layerImporterHelper = new GameObject("LayerImporterHelper");
			ImportAllLayers();
			LayerPickerUI.HideUI();
		}

		if (TeamManager.TeamCount == 0)
        {
            TeamManager.LoadTeams();
        }
        else
        {
            TeamManager.TeamsLoaded();
        }

       //MEL config use requires layers to be loaded (for kpi creation)
		NetworkForm form = new NetworkForm();
		ServerCommunication.DoRequest<CELConfig>(Server.GetCELConfig(), form, handleCELConfigCallback);
		ServerCommunication.DoRequest<JObject>(Server.GetMELConfig(), form, handleMELConfigCallback);
		ServerCommunication.DoRequest<SELGameClientConfig>(Server.GetShippingClientConfig(), form, HandleSELClientConfigCallback);
		ServerCommunication.DoRequest<KPICategoryDefinition[]>(Server.ShippingKPIConfig(), form, handleShippingKPIConfig);
	}

	private static void handleShippingKPIConfig(KPICategoryDefinition[] config)
	{
		KPIManager.CreateShippingKPIBars(config);
	}

	private static void handleMELConfigCallback(JObject melConfig)
    {
        KPIManager.CreateEcologyKPIs(melConfig);
        PlanManager.LoadFishingFleets(melConfig);
    }

	private static void handleCELConfigCallback(CELConfig config)
	{
		Sprite greenSprite = config.green_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.green_centerpoint_sprite);
		Sprite greySprite = config.grey_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.grey_centerpoint_sprite);
		Color greenColor = Util.HexToColor(config.green_centerpoint_color);
		Color greyColor = Util.HexToColor(config.grey_centerpoint_color);

		foreach (PointLayer layer in LayerManager.GetCenterPointLayers())
		{
			layer.EntityTypes[0].DrawSettings.PointColor = layer.greenEnergy ? greenColor : greyColor;
			layer.EntityTypes[0].DrawSettings.PointSprite = layer.greenEnergy ? greenSprite : greySprite;
			layer.EntityTypes[0].DrawSettings.PointSize = layer.greenEnergy ? config.green_centerpoint_size : config.grey_centerpoint_size;
		}
	}

	private static void HandleSELClientConfigCallback(SELGameClientConfig newSelConfig)
	{
		Main.SelConfig = newSelConfig;
	}

#if UNITY_EDITOR
	[MenuItem("MSP 2050/Reload layer meta")]
    public static void ReimportLayerTypeColors()
    {
        NetworkForm form = new NetworkForm();
		form.AddField("user", TeamManager.CurrentSessionID.ToString());
		ServerCommunication.DoRequest< List<LayerMeta>>(Server.LayerMeta(), form, handleReimportLayerTypeColorsCallback);
    }
#endif

    private static void handleReimportLayerTypeColorsCallback(List<LayerMeta> layerMeta)
    {
        foreach (AbstractLayer layer in LayerManager.GetLoadedLayers())
        {
            foreach (LayerMeta meta in layerMeta)
                if (layer.ID == meta.layer_id)
                {
                    foreach (KeyValuePair<int, EntityTypeValues> kvp in meta.layer_type)
                        layer.EntityTypes[kvp.Key].SetColors(Util.HexToColor(kvp.Value.polygonColor), Util.HexToColor(kvp.Value.lineColor), Util.HexToColor(kvp.Value.pointColor));
                    break;
                }
            layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
        }
    }

	public static void ImportAllLayers()
	{
		List<AbstractLayer> layerList = LayerManager.GetAllValidLayers();
		List<int> layersToLoad = new List<int>(layerList.Count);
		foreach (AbstractLayer layerToLoad in layerList)
		{
			layersToLoad.Add(layerToLoad.ID);
		}
		ImportLayers(layersToLoad);
	}

	public static void ImportLayers(List<int> selectedLayerIDs)
	{
		IsCurrentlyImportingLayers = true;

        string layerName = LayerManager.GetLayerByID(selectedLayerIDs[0]).FileName;
		InterfaceCanvas.Instance.loadingScreen.CreateLoadingBar(selectedLayerIDs.Count + 2, "layers");
		expectedLayers = selectedLayerIDs.Count;
		
		//stopWatch = new Stopwatch();
		//stopWatch.Start();

		foreach (int selectedLayerID in selectedLayerIDs)
		{
			AbstractLayer layer = LayerManager.GetLayerByID(selectedLayerID);
			if (layer.GetGeoType() == LayerManager.GeoType.raster)
			{
				ImportRasterLayer((layer as RasterLayer));
			}
			else
			{
				NetworkForm form = new NetworkForm();
				form.AddField("layer_id", selectedLayerID);
				ServerCommunication.DoRequest<List<SubEntityObject>>(Server.GetLayer(), form, (objs) => HandleVectorLayerImport(objs, layer));
			}
		}

		//selectedLayerIDList = selectedLayerIDs;
		//LoadNextLayer();
    }

	//static List<int> selectedLayerIDList;
	//private static void LoadNextLayer()
	//{
	//	AbstractLayer layer = LayerManager.GetLayerByID(selectedLayerIDList[importedLayers]);
	//	if (layer.GetGeoType() == LayerManager.GeoType.raster)
	//	{
	//		ImportRasterLayer((layer as RasterLayer));
	//	}
	//	else
	//	{
	//		NetworkForm form = new NetworkForm();
	//		form.AddField("layer_id", selectedLayerIDList[importedLayers]);
	//		ServerCommunication.DoRequest<List<SubEntityObject>>(Server.GetLayer(), form, (objs) => HandleVectorLayerImport(objs, layer));
	//	}
	//}

	static void HandleVectorLayerImport(List<SubEntityObject> layerObjects, AbstractLayer layer)
	{
		importLayer(layerObjects, layer);
		LayerImportComplete();
	}

    static void ImportRasterLayer(RasterLayer layer)
    {
        List<SubEntityObject> objects = new List<SubEntityObject>();
        SubEntityObject entityObject = new SubEntityObject();

		//Convert all the entity types to a 
		StringBuilder typeIdString = new StringBuilder(64);
		foreach (int entityTypeId in layer.EntityTypes.Keys)
		{
			if (typeIdString.Length > 0)
			{
				typeIdString.Append(", ");
			}
			typeIdString.Append(entityTypeId);
		}

		entityObject.type = typeIdString.ToString();
        objects.Add(entityObject); // add one empty object, it doesnt need this anyways
        LayerManager.LoadLayer(layer, objects);
		LayerImportComplete();
	}

	static void LayerImportComplete()
	{
		importedLayers++;
		InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("layers");

		if (importedLayers == expectedLayers)
		{
			//stopWatch.Stop();
			//UnityEngine.Debug.Log($"Importing layers took {stopWatch.ElapsedMilliseconds} ms");
			Main.AllLayersImported();

			LayerManager.ReorderLayers();

			CameraManager.Instance.GetNewPlayArea();

			IsCurrentlyImportingLayers = false;
			if (OnDoneImporting != null)
			{
				OnDoneImporting();
			}
		}
		//else
		//{
		//	InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem(LayerManager.GetLayerByID(selectedLayerIDList[importedLayers]).ShortName);
		//	LoadNextLayer();
		//}
	}

    private static void importLayer(List<SubEntityObject> objects, AbstractLayer layer)
    {
        LayerManager.LoadLayer(layer, objects);
    }
}

public class SubEntityObject
{
    public int id { get; set; }
    public List<List<float>> geometry { get; set; }
    public List<GeometryObject> subtractive { get; set; }
    public int active { get; set; }
    public int persistent { get; set; }
    public int mspid { get; set; }
    public int country = -1;
    public string type { get; set; }
    public Dictionary<string, string> data { get; set; }

    public List<EntityType> GetEntityType(AbstractLayer layer)
    {
        string[] types = type.Split(',');
        List<EntityType> entityTypes = new List<EntityType>();

        foreach (string entityTypeString in types)
        {
            int entityTypeKey;
            if (!int.TryParse(entityTypeString, out entityTypeKey))
            {
                UnityEngine.Debug.LogError("Invalid type in layer '" + layer.FileName + "': entity with ID '" + id + "' has type: '" + entityTypeString + "' which is not a valid integer");
                entityTypes.Add(layer.EntityTypes.GetFirstValue());
                return entityTypes;
            }

            EntityType entityType = layer.GetEntityTypeByKey(entityTypeKey);
            if (entityType == null)
            {
				UnityEngine.Debug.LogError("Invalid type in layer '" + layer.FileName + "': entity with ID '" + id + "' has type: '" + entityTypeString + "' which is an undefined type key");
                entityTypes.Add(layer.EntityTypes.GetFirstValue());
                return entityTypes;
            }

            entityTypes.Add(entityType);
        }

        return entityTypes;
    }

}

public class GeometryObject
{
    public int id { get; set; }
    public List<List<float>> geometry { get; set; }
    public List<GeometryObject> subtractive { get; set; }
    public int active { get; set; }
    public int persistent { get; set; }
    public int mspid { get; set; }
}