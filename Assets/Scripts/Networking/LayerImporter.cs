using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerImporter
	{
		int importedLayers;
		int expectedLayers;
		Stopwatch stopWatch;
		private LayerPickerUI layerPickerUI;

		private bool loadAllLayers = false;

		public bool IsCurrentlyImportingLayers 
		{ 
			get; 
			private set; 
		}

		public LayerImporter(LayerPickerUI layerPickerUI)
		{ 
			this.layerPickerUI = layerPickerUI;

			if (!Main.IsDeveloper)
			{
				//Force the loading screen active so we don't show a single frame of uglyness.
				InterfaceCanvas.Instance.loadingScreen.ShowHideLoadScreen(true);
				InterfaceCanvas.Instance.loadingScreen.SetLoadingBarPercentage(0.0f);
				InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("Layer Meta Data");
				layerPickerUI.HideUI();
			}

			NetworkForm form = new NetworkForm();
			form.AddField("user", SessionManager.Instance.CurrentSessionID.ToString());
			ServerCommunication.Instance.DoRequest<List<LayerMeta>>(Server.LayerMeta(), form, HandleImportLayerMetaCallback);
		}

		private void HandleImportLayerMetaCallback(List<LayerMeta> layerMeta)
		{
			//Load layers
			LayerInfo.Load(layerMeta);
			if (Main.IsDeveloper)
			{
				layerPickerUI.CreateUI();
				layerPickerUI.onLayersSelected = ImportLayers;
			}
			else
			{
				loadAllLayers = true;
			}

			InterfaceCanvas.Instance.SetAccent(SessionManager.Instance.CurrentTeamColor);
			InterfaceCanvas.Instance.activePlanWindow.OnCountriesLoaded();
			KPIManager.Instance.CreateEnergyKPIs();

			//MEL config use requires layers to be loaded (for kpi creation)
			NetworkForm form = new NetworkForm();
			ServerCommunication.Instance.DoRequest<CELConfig>(Server.GetCELConfig(), form, HandleCELConfigCallback);
			ServerCommunication.Instance.DoRequest<JObject>(Server.GetMELConfig(), form, HandleMELConfigCallback);
			ServerCommunication.Instance.DoRequest<SELGameClientConfig>(Server.GetShippingClientConfig(), form, HandleSELClientConfigCallback);
			ServerCommunication.Instance.DoRequest<KPICategoryDefinition[]>(Server.ShippingKPIConfig(), form, HandleShippingKPIConfig);
		}

		private void HandleShippingKPIConfig(KPICategoryDefinition[] config)
		{
			KPIManager.Instance.CreateShippingKPIBars(config);
		}

		private void HandleMELConfigCallback(JObject melConfig)
		{
			KPIManager.Instance.CreateEcologyKPIs(melConfig);
			PlanManager.Instance.LoadFishingFleets(melConfig);

			if (loadAllLayers)
			{
				ImportAllLayers();
			}
		}

		private void HandleCELConfigCallback(CELConfig config)
		{
			if(config != null)
			{ 
				Sprite greenSprite = config.green_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.green_centerpoint_sprite);
				Sprite greySprite = config.grey_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.grey_centerpoint_sprite);
				Color greenColor = Util.HexToColor(config.green_centerpoint_color);
				Color greyColor = Util.HexToColor(config.grey_centerpoint_color);

				foreach (PointLayer layer in LayerManager.Instance.GetCenterPointLayers())
				{
					layer.EntityTypes[0].DrawSettings.PointColor = layer.greenEnergy ? greenColor : greyColor;
					layer.EntityTypes[0].DrawSettings.PointSprite = layer.greenEnergy ? greenSprite : greySprite;
					layer.EntityTypes[0].DrawSettings.PointSize = layer.greenEnergy ? config.green_centerpoint_size : config.grey_centerpoint_size;
				}
			}
		}

		private void HandleSELClientConfigCallback(SELGameClientConfig newSelConfig)
		{
			Main.Instance.SelConfig = newSelConfig;
		}

#if UNITY_EDITOR
		[MenuItem("MSP 2050/Reload layer meta")]
		public void ReimportLayerTypeColors()
		{
			NetworkForm form = new NetworkForm();
			form.AddField("user", SessionManager.Instance.CurrentSessionID.ToString());
			ServerCommunication.Instance.DoRequest< List<LayerMeta>>(Server.LayerMeta(), form, HandleReimportLayerTypeColorsCallback);
		}
#endif

		private void HandleReimportLayerTypeColorsCallback(List<LayerMeta> layerMeta)
		{
			foreach (AbstractLayer layer in LayerManager.Instance.GetLoadedLayers())
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

		public void ImportAllLayers()
		{
			List<AbstractLayer> layerList = LayerManager.Instance.GetAllValidLayers();
			List<int> layersToLoad = new List<int>(layerList.Count);
			foreach (AbstractLayer layerToLoad in layerList)
			{
				layersToLoad.Add(layerToLoad.ID);
			}
			ImportLayers(layersToLoad);
		}

		public void ImportLayers(List<int> selectedLayerIDs)
		{
			// only allow a single request during loading of layers, to minimize the load on the server - think of multiple clients starting simultaneously
			ServerCommunication.maxRequests = 1;
			

			IsCurrentlyImportingLayers = true;

			string layerName = LayerManager.Instance.GetLayerByID(selectedLayerIDs[0]).FileName;
			InterfaceCanvas.Instance.loadingScreen.CreateLoadingBar(selectedLayerIDs.Count + 2, "layers");
			expectedLayers = selectedLayerIDs.Count;

			foreach (int selectedLayerID in selectedLayerIDs)
			{
				AbstractLayer layer = LayerManager.Instance.GetLayerByID(selectedLayerID);
				if (layer.GetGeoType() == LayerManager.GeoType.raster)
				{
					ImportRasterLayer((layer as RasterLayer));
				}
				else
				{
					NetworkForm form = new NetworkForm();
					form.AddField("layer_id", selectedLayerID);
					ServerCommunication.Instance.DoRequest<List<SubEntityObject>>(Server.GetLayer(), form, (objs) => HandleVectorLayerImport(objs, layer));
				}
			}
		}

		private void HandleVectorLayerImport(List<SubEntityObject> layerObjects, AbstractLayer layer)
		{
			importLayer(layerObjects, layer);
			LayerImportComplete();
		}

		private void ImportRasterLayer(RasterLayer layer)
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
			LayerManager.Instance.LoadLayer(layer, objects);
			LayerImportComplete();
		}

		private void LayerImportComplete()
		{
			importedLayers++;
			InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("layers");

			if (importedLayers == expectedLayers)
			{
				Main.Instance.AllLayersImported();

				LayerManager.Instance.ReorderLayers();

				CameraManager.Instance.GetNewPlayArea();

				// to be restored to the default number of requests, once all layers have been loaded
				ServerCommunication.maxRequests = ServerCommunication.DEFAULT_MAX_REQUESTS;
				IsCurrentlyImportingLayers = false;
			}
		}

		private void importLayer(List<SubEntityObject> objects, AbstractLayer layer)
		{
			LayerManager.Instance.LoadLayer(layer, objects);
		}
	}

	public class SubEntityObject
	{
		public int id { get; set; }
		public List<List<float>> geometry { get; set; }
		public List<GeometryObject> subtractive { get; set; }
		public int active { get; set; }
		public int persistent { get; set; }
		public string mspid { get; set; }
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
		public string mspid { get; set; }
	}
}