using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
			SimulationManager.Instance.PostLayerMetaInitialise();
			PolicyManager.Instance.PostLayerMetaInitialise();

			var autoLoginEnabled = null != CommandLineArgumentsManager.GetInstance().GetCommandLineArgumentValue(
				CommandLineArgumentsManager.CommandLineArgumentName.AutoLogin);
			if (Main.IsDeveloper && !autoLoginEnabled)
			{
				//InterfaceCanvas.Instance.loadingScreen.ShowHideLoadScreen(false);
				layerPickerUI.CreateUI();
				layerPickerUI.onLayersSelected = ImportLayers;
			}
			else
			{
				loadAllLayers = true;
			}

			InterfaceCanvas.Instance.SetAccent(SessionManager.Instance.CurrentTeamColor);
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.OnCountriesLoaded();

			if (loadAllLayers)
			{
				ImportAllLayers();
			}
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
					if (layer.m_id == meta.layer_id)
					{
						foreach (KeyValuePair<int, EntityTypeValues> kvp in meta.layer_type)
							layer.m_entityTypes[kvp.Key].SetColors(Util.HexToColor(kvp.Value.polygonColor), Util.HexToColor(kvp.Value.lineColor), Util.HexToColor(kvp.Value.pointColor));
						break;
					}
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
		}

		public void ImportAllLayers()
		{
			List<AbstractLayer> layerList = LayerManager.Instance.GetAllLayers();
			List<int> layersToLoad = new List<int>(layerList.Count);
			foreach (AbstractLayer layerToLoad in layerList)
			{
				layersToLoad.Add(layerToLoad.m_id);
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
				if (layer.GetGeoType() == LayerManager.EGeoType.Raster)
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
			LayerImportComplete(layer);
		}

		private void ImportRasterLayer(RasterLayer layer)
		{
			List<SubEntityObject> objects = new List<SubEntityObject>();
			SubEntityObject entityObject = new SubEntityObject();

			//Convert all the entity types to a
			StringBuilder typeIdString = new StringBuilder(64);
			foreach (int entityTypeId in layer.m_entityTypes.Keys)
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
			LayerImportComplete(layer);
		}

		private void LayerImportComplete(AbstractLayer layer)
		{
			importedLayers++;
			InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("layers");
			LayerManager.Instance.InvokeLayerLoaded(layer);

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
					entityTypes.Add(layer.m_entityTypes.GetFirstValue());
					return entityTypes;
				}

				EntityType entityType = layer.GetEntityTypeByKey(entityTypeKey);
				if (entityType == null)
				{
					UnityEngine.Debug.LogError("Invalid type in layer '" + layer.FileName + "': entity with ID '" + id + "' has type: '" + entityTypeString + "' which is an undefined type key");
					entityTypes.Add(layer.m_entityTypes.GetFirstValue());
					return entityTypes;
				}

				entityTypes.Add(entityType);
			}

			return entityTypes;
		}

	}

	public class PolicySimSettings
	{
		[JsonProperty(ItemConverterType = typeof(PolicySettingsJsonConverter))]
		public List<APolicyData> policy_settings;
		[JsonProperty(ItemConverterType = typeof(SimulationSettingsJsonConverter))]
		public List<ASimulationData> simulation_settings;

		//public JArray policy_settings;
		//public JArray simulation_settings;
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