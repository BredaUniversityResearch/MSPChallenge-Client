using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine.Networking;

public static class LayerManager
{
	public enum GeoType { polygon, line, point, raster }

	private static List<AbstractLayer> layers = new List<AbstractLayer>();
	private static HashSet<AbstractLayer> loadedLayers = new HashSet<AbstractLayer>();
	private static HashSet<AbstractLayer> visibleLayers = new HashSet<AbstractLayer>();
	private static List<PointLayer> energyPointLayers = new List<PointLayer>();
	private static HashSet<AbstractLayer> nonReferenceLayers; //Layers that are drawn as normal during edit mode

	public static LineStringLayer energyCableLayerGreen;
	public static LineStringLayer energyCableLayerGrey;
	public static PolygonLayer EEZLayer;
	public static List<AbstractLayer> energyLayers = new List<AbstractLayer>(); //Does not include sourcepolygonpoints
	public static List<AbstractLayer> protectedAreaLayers = new List<AbstractLayer>();
	public static Dictionary<int, int> sourceCountries = new Dictionary<int, int>();

	public static Dictionary<int, SubEntity> energySubEntities;

	private static Dictionary<string, List<string>> categorySubcategories = new Dictionary<string, List<string>>();
	private static bool finishedImporting = false;

	public static AbstractLayer highLightedLayer;

	static LayerManager()
	{
		visibleLayers.Clear();
	}

	public static void AddLayer(AbstractLayer layer)
	{
		finishedImporting = false;
		while (layer.ID >= layers.Count)
		{
			layers.Add(null);
		}
		layers[layer.ID] = layer;
		if (layer.IsEnergyLineLayer())
		{
			if (layer.greenEnergy)
				energyCableLayerGreen = layer as LineStringLayer;
			else
				energyCableLayerGrey = layer as LineStringLayer;
		}
		if (layer.editingType != AbstractLayer.EditingType.Normal && layer.editingType != AbstractLayer.EditingType.SourcePolygonPoint)
			energyLayers.Add(layer);
		if (layer.FileName == Main.MspGlobalData.countries)
			EEZLayer = layer as PolygonLayer;
	}

	public static void FinishedImportingLayers()
	{
		PopulateAllCountryIDs();
		finishedImporting = true;
		Debug.Log("All layers imported (" + GetValidLayerCount() + ")");
	}


	//Todo EEZLayers should be replaced by Teritorial waters, but this requires extra steps
	public static void PopulateAllCountryIDs()
	{
		if (EEZLayer == null)
			return;

		//Set the EEZs own country id
		foreach (Entity ent in EEZLayer.Entities)
			ent.Country = ent.EntityTypes[0].value;

		foreach (AbstractLayer tLayer in loadedLayers)
		{
			if (tLayer.ID == EEZLayer.ID)
				continue;

			if (tLayer is PointLayer)
			{
				foreach (PointEntity tPointEntity in (tLayer as PointLayer).Entities)
				{
					if (tPointEntity.Country > 0)
						continue;
					foreach (PolygonEntity tCountryEntity in EEZLayer.Entities)
					{
						if (Util.PolygonPointIntersection(tCountryEntity.GetPolygonSubEntity(), tPointEntity.GetPointSubEntity()))
						{
							//the .value from EntityType in EEZLayers is the ID
							tPointEntity.Country = tCountryEntity.EntityTypes[0].value;
							break; //Early out skip to the next PointEntity
						}
					}
				}
				continue;
			}
			if (tLayer is PolygonLayer)
			{
				foreach (PolygonEntity tPolyEntity in (tLayer as PolygonLayer).Entities)
				{
					if (tPolyEntity.Country > 0)
						continue;
					foreach (PolygonEntity tCountryEntity in EEZLayer.Entities)
					{
						if (Util.PolygonPolygonIntersection(tCountryEntity.GetPolygonSubEntity(), tPolyEntity.GetPolygonSubEntity()))
						{
							//the .value from EntityType in EEZLayers is the ID
							tPolyEntity.Country = tCountryEntity.EntityTypes[0].value;
							break; //Early out skip to the next PolyEntity
						}
					}
				}
				continue;
			}
			if (tLayer is LineStringLayer)
			{
				foreach (LineStringEntity tLineStringEntity in (tLayer as LineStringLayer).Entities)
				{
					if (tLineStringEntity.Country > 0)
						continue;
					foreach (PolygonEntity tCountryEntity in EEZLayer.Entities)
					{
						if (Util.PolygonLineIntersection(tCountryEntity.GetPolygonSubEntity(), tLineStringEntity.GetLineStringSubEntity()))
						{
							//the .value from EntityType in EEZLayers is the ID
							tLineStringEntity.Country = tCountryEntity.EntityTypes[0].value;
							break; //Early out skip to the next LineStringEntity
						}
					}
				}
				continue;
			}
		}
	}

	public static bool AllLayersImported()
	{
		PopulateAllCountryIDs();
		return finishedImporting;
	}

	public static int GetLayerCount()
	{
		return layers.Count;
	}

	public static int GetValidLayerCount()
	{
		int result = 0;
		foreach (AbstractLayer layer in layers)
		{
			if (layer != null)
			{
				result++;
			}
		}
		return result;
	}

	public static AbstractLayer GetLayerByID(int layerID)
	{
		return layers[layerID];
	}

	public static List<AbstractLayer> GetLoadedLayers(string category, string subcategory)
	{
		List<AbstractLayer> result = new List<AbstractLayer>();
		foreach (AbstractLayer layer in layers)
		{
			if (loadedLayers.Contains(layer))
			{
				if (layer.Category == category && layer.SubCategory == subcategory)
				{
					result.Add(layer);
				}
			}
		}

		return result;
	}


	public static List<AbstractLayer> GetLoadedLayers()
	{
		List<AbstractLayer> result = new List<AbstractLayer>();
		foreach (AbstractLayer layer in layers)
		{
			if (loadedLayers.Contains(layer))
			{
				result.Add(layer);
			}
		}

		return result;
	}

	public static AbstractLayer FindFirstLayerContainingName(string name)
	{
		foreach (AbstractLayer layer in loadedLayers)
		{
			if (layer.FileName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) != -1)
			{
				return layer;
			}
		}
		return null;
	}

	public static AbstractLayer FindLayerByFilename(string name)
	{
		foreach (AbstractLayer layer in loadedLayers)
		{
			if (layer.FileName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
			{
				return layer;
			}
		}
		return null;
	}

	public static AbstractLayer GetLoadedLayer(int ID)
	{
		AbstractLayer layer = GetLayerByID(ID);

		if (loadedLayers.Contains(layer))
		{
			return layer;
		}

		return null;
	}

	public static List<AbstractLayer> GetAllValidLayers()
	{
		List<AbstractLayer> result = new List<AbstractLayer>();
		foreach (AbstractLayer layer in layers)
		{
			if (layer != null)
			{
				result.Add(layer);
			}
		}

		return result;
	}

	public static List<AbstractLayer> GetAllValidLayersOfGroup(string group)
	{
		if (group == string.Empty)
		{
			return GetAllValidLayers();
		}

		List<AbstractLayer> result = new List<AbstractLayer>();
		foreach (AbstractLayer layer in layers)
		{
			if (layer != null && layer.Group == group)
			{
				result.Add(layer);
			}
		}

		return result;
	}

	public static Dictionary<string, List<string>> GetCategorySubcategories()
	{
		return categorySubcategories;
	}

	public static void LoadLayer(AbstractLayer layer, List<SubEntityObject> layerObjects)
	{
		layer.LoadLayerObjects(layerObjects);

		layer.DrawGameObject();

		if (!layer.Dirty)
		{
			loadedLayers.Add(layer);

			AddToCategories(layer.Category, layer.SubCategory);

			LayerInterface.AddLayerToInterface(layer);

			if (layer.ActiveOnStart)
			{
				ShowLayer(layer);
			}
            else if (Main.LayerSelectedForCurrentExpertise(layer.FileName))
            {
                InterfaceCanvas.Instance.activeLayers.AddLayer(layer, false);
            }

			layer.Loaded = true;
		}
		else
		{
			Debug.LogError("(Corrupt) Layer has been found dirty: " + layer.ShortName);
		}
	}

	public static void UpdateAllVisibleLayersTo(Plan plan)
	{
		foreach (AbstractLayer layer in visibleLayers)
		{
			layer.SetEntitiesActiveUpTo(plan);
		}
	}

	public static void ShowLayer(AbstractLayer layer, bool shownInUI = true, bool toggleValuePlanMonitor = true)
	{
        bool needsUpdateAndRedraw = false;
		foreach (EntityType entityType in layer.EntityTypes.Values)
		{
            needsUpdateAndRedraw = needsUpdateAndRedraw || layer.SetEntityTypeVisibility(entityType, true);
		}

		if (!visibleLayers.Contains(layer))
		{
            //Performs a more elaborate update and redraw, so no other is needed
            needsUpdateAndRedraw = false;

			if (PlanManager.planViewing != null || PlanManager.timeViewing < 0)
				layer.SetEntitiesActiveUpTo(PlanManager.planViewing);
			else
				layer.SetEntitiesActiveUpToTime(PlanManager.timeViewing);

			layer.LayerGameObject.SetActive(true);
			visibleLayers.Add(layer);
			layer.LayerShown();
			layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			layer.UpdateScale(CameraManager.Instance.gameCamera);

			if (shownInUI && layer.Toggleable)
			{
				//Show in Layer Select and Active Layers
				UIManager.OnShowLayer(layer);
			}
		}

        if (needsUpdateAndRedraw)
            layer.SetActiveToCurrentPlanAndRedraw();

		UpdateVisibleLayerIndexForAllTypes();
	}

	public static void HideAllVisibleLayers()
	{
		//We need a copy since we will be modifying the collection
		List<AbstractLayer> list = new List<AbstractLayer>(visibleLayers);
		for (int i = 0; i < list.Count; ++i)
		{
			AbstractLayer layer = list[i];
			if (layer.Toggleable)
			{
				HideLayer(layer);
			}
		}
	}

	public static void HideLayer(AbstractLayer layer)
	{
		//Layer that is being edited cannot be hidden
		if (Main.InEditMode && PlanDetails.LayersTab.CurrentlyEditingBaseLayer == layer)
			return;

		if (visibleLayers.Contains(layer))
		{
			layer.LayerGameObject.SetActive(false);
			visibleLayers.Remove(layer);
			layer.LayerHidden();

			//hide in Layer Select and Active Layers
			UIManager.OnHideLayer(layer);
		}

		UpdateVisibleLayerIndexForAllTypes();
	}

	private static void UpdateVisibleLayerIndexForAllTypes()
	{
		Dictionary<Type, int> visibleLayerIndexByType = new Dictionary<Type, int>();
		foreach (AbstractLayer layer in visibleLayers)
		{
			int layerIndex = 0;
			visibleLayerIndexByType.TryGetValue(layer.GetType(), out layerIndex);

			layer.UpdateVisibleIndexLayerType(layerIndex);

			++layerIndex;
			visibleLayerIndexByType[layer.GetType()] = layerIndex;
		}
	}

	public static List<EnergyLineStringSubEntity> ForceEnergyLayersActiveUpTo(Plan plan)
	{
		//Call setactiveupto on all energy layers not yet active and clear connections
		foreach (AbstractLayer energyLayer in energyLayers)
		{
			if (!plan.IsLayerpartOfPlan(energyLayer))
				energyLayer.SetEntitiesActiveUpTo(plan);
			energyLayer.ResetEnergyConnections();
		}

		List<EnergyLineStringSubEntity> cablesToRemove = new List<EnergyLineStringSubEntity>();

		//Have the cable layer activate all connections that are present in the current state
		if (energyCableLayerGreen != null)
		{
			if (plan.GetPlanLayerForLayer(energyCableLayerGreen) != null) //Only remove invalid cables if the plan contains a cable layer
			{
				List<EnergyLineStringSubEntity> newCablesToRemove = energyCableLayerGreen.RemoveInvalidCables();
				if (newCablesToRemove != null)
					cablesToRemove = newCablesToRemove;
			}
			energyCableLayerGreen.ActivateCableLayerConnections();
		}
		if (energyCableLayerGrey != null)
		{
			if (plan.GetPlanLayerForLayer(energyCableLayerGrey) != null) //Only remove invalid cables if the plan contains a cable layer
			{
				List<EnergyLineStringSubEntity> newCablesToRemove = energyCableLayerGrey.RemoveInvalidCables();
				if (newCablesToRemove != null && newCablesToRemove.Count > 0)
					cablesToRemove.AddRange(newCablesToRemove);
			}
			energyCableLayerGrey.ActivateCableLayerConnections();
		}
		return cablesToRemove;
	}

	public static void RestoreRemovedCables(List<EnergyLineStringSubEntity> removedCables)
	{
		if (energyCableLayerGreen != null)
		{
			energyCableLayerGreen.RestoreInvalidCables(removedCables);
		}
		if (energyCableLayerGrey != null)
		{
			energyCableLayerGrey.RestoreInvalidCables(removedCables);
		}
	}

	/// <summary>
	/// Sets entities in visible layers active to plan and shows layers in the plan that were not visible.
	/// </summary>
	/// <param name="plan"></param>
	public static void UpdateVisibleLayersToPlan(Plan plan)
	{
		foreach (AbstractLayer layer in visibleLayers)
		{
			layer.SetEntitiesActiveUpTo(plan);
			layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}

		foreach (PlanLayer planLayer in plan.PlanLayers)
			if (!visibleLayers.Contains(planLayer.BaseLayer))
				ShowLayer(planLayer.BaseLayer);

		if (energyCableLayerGreen != null || energyCableLayerGrey != null)
		{
			foreach (AbstractLayer energyLayer in energyLayers)
				energyLayer.ResetCurrentGrids();

			List<EnergyGrid> grids = PlanManager.GetEnergyGridsAtTime(plan.StartTime, EnergyGrid.GridColor.Either);
			if (energyCableLayerGreen != null)
			{
				Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGreen.GetCableNetworkForPlan(plan);
				foreach (EnergyGrid grid in grids)
					if (grid.IsGreen)
						grid.SetAsCurrentGridForContent(network);
			}
			if (energyCableLayerGrey != null)
			{
				Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGrey.GetCableNetworkForPlan(plan);
				foreach (EnergyGrid grid in grids)
					if (!grid.IsGreen)
						grid.SetAsCurrentGridForContent(network);
			}
		}
	}

	public static void UpdateVisibleLayersToBase()
	{
		foreach (AbstractLayer layer in visibleLayers)
		{
			//layer.SetEntitiesActiveUpToCurrentTime();
			layer.SetEntitiesActiveUpTo(-1);
			layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}
		if (energyCableLayerGreen != null || energyCableLayerGrey != null)
		{
			foreach (AbstractLayer energyLayer in energyLayers)
				energyLayer.ResetCurrentGrids();
		}
	}

	public static void UpdateVisibleLayersToTime(int month)
	{
		foreach (AbstractLayer layer in visibleLayers)
		{
			layer.SetEntitiesActiveUpToTime(month);
			layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}
		
		if (energyCableLayerGreen != null || energyCableLayerGrey != null)
		{
			foreach (AbstractLayer energyLayer in energyLayers)
				energyLayer.ResetCurrentGrids();

			List<EnergyGrid> grids = PlanManager.GetEnergyGridsAtTime(month, EnergyGrid.GridColor.Either);
			if (energyCableLayerGreen != null)
			{
				Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGreen.GetCableNetworkAtTime(month);
				foreach (EnergyGrid grid in grids)
					if (grid.IsGreen)
						grid.SetAsCurrentGridForContent(network);
			}
			if (energyCableLayerGrey != null)
			{
				Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGrey.GetCableNetworkAtTime(month);
				foreach (EnergyGrid grid in grids)
					if (!grid.IsGreen)
						grid.SetAsCurrentGridForContent(network);
			}
		}
	}

	public static void UpdateLayerToPlan(AbstractLayer baseLayer, Plan plan, bool showIfHidden)
	{
		if (visibleLayers.Contains(baseLayer))
		{
			baseLayer.SetEntitiesActiveUpTo(plan);
			baseLayer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}
		else if (showIfHidden)
			ShowLayer(baseLayer);
	}

	public static void RedrawVisibleLayers()
	{
		foreach (AbstractLayer layer in visibleLayers)
		{
			layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}
	}

	public static bool LayerIsVisible(AbstractLayer layer)
	{
		return visibleLayers.Contains(layer);
	}

	public static IEnumerable<AbstractLayer> GetVisibleLayers()
	{
		return visibleLayers;
	}

	public static List<AbstractLayer> GetVisibleLayersSortedByDepth()
	{
		List<AbstractLayer> result = new List<AbstractLayer>(visibleLayers);
		result.Sort((x, y) => y.Depth.CompareTo(x.Depth));
		return result;
	}

	public static List<AbstractLayer> GetLoadedLayersSortedByDepth()
	{
		List<AbstractLayer> result = new List<AbstractLayer>(loadedLayers);
		result.Sort((x, y) => y.Depth.CompareTo(x.Depth));
		return result;
	}

	public static void UpdateLayerScales(Camera targetCamera)
	{
		foreach (AbstractLayer layer in visibleLayers)
		{
			if (layer.LayerGameObject.activeInHierarchy)
			{
				layer.UpdateScale(targetCamera);
			}
		}
	}

	public static SubEntity GetSubEntity(AbstractLayer layer, int subEntityID)
	{
		for (int i = 0; i < layer.GetEntityCount(); i++)
		{
			Entity entity = layer.GetEntity(i);

			for (int j = 0; j < entity.GetSubEntityCount(); j++)
			{
				SubEntity tmpEntity = entity.GetSubEntity(j);
				if (tmpEntity.HasDatabaseID())
				{
					if (tmpEntity.GetDatabaseID() == subEntityID)
					{
						return tmpEntity;
					}
				}
			}
		}

		return null; // This means it it doesnt exist
	}

	public static void ReorderLayers()
	{
		List<AbstractLayer> layersInOrder = GetLoadedLayersSortedByDepth();
		layersInOrder.Reverse();

		for (int i = 0; i < layersInOrder.Count; i++)
		{
			layersInOrder[i].Order = i;
			layersInOrder[i].LayerGameObject.transform.position = new Vector3(0, 0, -layersInOrder[i].Order);
		}
	}

	private static void moveGeometry(AbstractLayer from, AbstractLayer to, int offset)
	{
		NetworkForm form = new NetworkForm();

		form.AddField("old", from.ID);
		form.AddField("new", to.ID);
		form.AddField("offset", offset);

		ServerCommunication.DoRequest(Server.MergeLayer(), form);
	}

	public delegate void AddNewLayerCallback(AbstractLayer layer);

	//public static void AddNewLayer(string name, GeoType type, AddNewLayerCallback addNewLayerCallback)
	//{
	//	NetworkForm form = new NetworkForm();

	//	form.AddField("name", name);
	//	form.AddField("geotype", type.ToString());

	//	ServerCommunication.DoRequest(Server.NewLayer(), form, (www2) => { handleNewLayerCallback(www2, addNewLayerCallback); });
	//}

	//private static void handleNewLayerCallback(UnityWebRequest www, AddNewLayerCallback addNewLayerCallback)
	//{
	//	int layerID = Util.ParseToInt(www.downloadHandler.text);

	//	GetNewLayerMeta(layerID, addNewLayerCallback);
	//}

	//private static void GetNewLayerMeta(int layerID, AddNewLayerCallback addNewLayerCallback)
	//{
	//	NetworkForm form = new NetworkForm();

	//	ServerCommunication.DoRequest(Server.LayerMeta(layerID), form, (www2) => { handleGetNewLayerMetaCallback(www2, addNewLayerCallback); });
	//}

	//private static void handleGetNewLayerMetaCallback(UnityWebRequest www, AddNewLayerCallback addNewLayerCallback)
	//{
	//	List<LayerMeta> layerMeta = LayerInfo.Load(www);

	//	int layerID = layerMeta[0].layer_id;

	//	LoadLayer(layerID, new List<SubEntityObject>());

	//	AbstractLayer layer = GetLayerByID(layerID);
	//	addNewLayerCallback(layer);
	//}

	public static void ResetAll()
	{
		layers.Clear();
		loadedLayers.Clear();
		visibleLayers.Clear();
		finishedImporting = false;
	}

	//public static void MergeAllVisibleLayers()
	//{
	//	List<AbstractLayer> mergeLayers = new List<AbstractLayer>(visibleLayers);
	//	for (int i = mergeLayers.Count - 1; i >= 0; --i)
	//	{
	//		if (!mergeLayers[i].Selectable) { mergeLayers.RemoveAt(i); }
	//	}

	//	if (mergeLayers.Count < 2)
	//	{
	//		Debug.LogError("Unable to merge the visible layers: less than 2 layers selected");
	//		return;
	//	}

	//	bool geoTypeDefined = false;
	//	GeoType geoType = GeoType.polygon;
	//	foreach (AbstractLayer layer in mergeLayers)
	//	{
	//		if (!geoTypeDefined) { geoType = layer.GetGeoType(); geoTypeDefined = true; }
	//		else
	//		{
	//			if (geoType != layer.GetGeoType())
	//			{
	//				Debug.LogError("Unable to merge the visible layers: not all layers have the same type");
	//				return;
	//			}
	//		}
	//	}

	//	Debug.Log("Merging visible layers");
	//	AddNewLayer("Merged layer", geoType, (layer) => mergedLayerCreated(layer, mergeLayers));
	//}

	private static void AddToCategories(string category, string subcategory)
	{
		if (!categorySubcategories.ContainsKey(category))
		{
			categorySubcategories.Add(category, new List<string>());
		}

		if (!categorySubcategories[category].Contains(subcategory))
		{
			categorySubcategories[category].Add(subcategory);
		}

	}

	private static void mergedLayerCreated(AbstractLayer newLayer, List<AbstractLayer> mergeLayers)
	{
		newLayer.EntityTypes = new Dictionary<int, EntityType>();//new List<EntityType>();
		foreach (AbstractLayer l in mergeLayers)
		{
			int offset = newLayer.EntityTypes.Count;

			string layerName = l.GetShortName();// l.ShortName != "" ? l.ShortName : l.FileName;
			if (l.EntityTypes.Count == 1)
			{
				//newLayer.EntityTypes.Add(l.EntityTypes[0].GetClone(layerName));
				foreach (var kvp in l.EntityTypes)
				{
					newLayer.EntityTypes.Add(kvp.Key + offset, kvp.Value.GetClone(layerName));
				}
			}
			else
			{
				foreach (var kvp in l.EntityTypes)
				{
					newLayer.EntityTypes.Add(kvp.Key + offset, kvp.Value.GetClone(layerName + ": " + kvp.Value.Name));
				}
			}

			// move geometry locally on client

			//TODO: resupport this if required
			//l.MoveAllGeometryTo(newLayer, offset);

			// move geometry on server
			moveGeometry(l, newLayer, offset);
		}

		HashSet<string> names = new HashSet<string>();
		foreach (var kvp in newLayer.EntityTypes)
		{
			if (!names.Contains(kvp.Value.Name))
			{
				names.Add(kvp.Value.Name);
			}
			else
			{
				int number = 2;
				while (names.Contains(kvp.Value.Name + " " + number))
				{
					number++;
				}
				kvp.Value.Name = kvp.Value.Name + " " + number;
				names.Add(kvp.Value.Name);
			}
		}

		newLayer.Category = mergeLayers[0].Category;
		newLayer.SubCategory = mergeLayers[0].SubCategory;
		newLayer.Depth = mergeLayers[0].Depth;

		newLayer.SubmitMetaData();

		newLayer.DrawGameObject();

		//foreach (Layer l in mergeLayers)
		//{
		//    DeleteLayer(l);
		//}

		//ShowLayer(newLayer);
	}

	public static void AddEnergyPointLayer(PointLayer layer)
	{
		energyPointLayers.Add(layer);
	}

	public static EnergyPointSubEntity GetEnergyPointAtPosition(Vector3 pos)
	{
		foreach (PointLayer p in energyPointLayers)
			if (visibleLayers.Contains(p) || (p.sourcePolyLayer != null && visibleLayers.Contains(p.sourcePolyLayer)))
				foreach (SubEntity e in p.GetSubEntitiesAt(pos))
					if (e is EnergyPointSubEntity)
						return (e as EnergyPointSubEntity);
		return null;
	}

	public static List<PointLayer> GetCenterPointLayers()
	{
		List<PointLayer> result = new List<PointLayer>();
		foreach (PointLayer layer in energyPointLayers)
			if (layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
				result.Add(layer);
		return result;
	}

	public static void RemoveEnergySubEntityReference(int ID)
	{
		if (ID == -1)
			return;
		if (energySubEntities != null)
			energySubEntities.Remove(ID);
	}
	public static void AddEnergySubEntityReference(int ID, SubEntity subent)
	{
		if (ID == -1)
			return;
		if (energySubEntities == null)
			energySubEntities = new Dictionary<int, SubEntity>();
		if (!energySubEntities.ContainsKey(ID))
			energySubEntities.Add(ID, subent);
	}
	public static SubEntity GetEnergySubEntityByID(int ID, bool getSourcePointIfPoly = false)
	{
		SubEntity result = null;
		if (energySubEntities != null)
			energySubEntities.TryGetValue(ID, out result);
		if (getSourcePointIfPoly && result is EnergyPolygonSubEntity)
			result = ((EnergyPolygonSubEntity)result).sourcePoint;
		return result;
	}

	/// <summary>
	/// Use with care. Is quite an expensive call to make.
	/// </summary>
	/// <param name="persistentId"></param>
	/// <returns></returns>
	public static SubEntity FindSubEntityByPersistentID(int persistentId)
	{
		SubEntity result = null;
		for (int i = 0; i < layers.Count; ++i)
		{
			AbstractLayer layer = layers[i];
			if (layer != null)
			{
				SubEntity foundEntity = layer.GetSubEntityByPersistentID(persistentId);
				if (foundEntity != null)
				{
					result = foundEntity;
					break;
				}
			}
		}
		return result;
	}

	/// <summary>
	/// Calls SetEntitiesActiveUpTo current viewing plan for all layers that would be altered by this plans update.
	/// Takes plan's time into account.
	/// Doesn't run in edit mode.
	/// </summary>
	/// <param name="plan"></param>
	public static void UpdateVisibleLayersFromPlan(Plan plan)
	{
		//Dont update layers while in edit mode, quickly causes errors
		if (Main.InEditMode || Main.EditingPlanDetailsContent)
			return;

		//Only update if we are viewing the plan or one further in the future
		if (PlanManager.planViewing == null ||
			(PlanManager.planViewing.StartTime < plan.StartTime ||
			(PlanManager.planViewing.StartTime == plan.StartTime && PlanManager.planViewing.ID < plan.ID)))
			return;

		//Only update if already visible
		foreach (PlanLayer layer in plan.PlanLayers)		
			if (visibleLayers.Contains(layer.BaseLayer))
				layer.BaseLayer.SetEntitiesActiveUpTo(PlanManager.planViewing);
	}

    public static void AddNonReferenceLayer(AbstractLayer layer, bool redrawLayer)
    {
        if (nonReferenceLayers == null)
            nonReferenceLayers = new HashSet<AbstractLayer>() { layer };
        else
            nonReferenceLayers.Add(layer);
        if (redrawLayer)
            layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
    }

    public static void SetNonReferenceLayers(HashSet<AbstractLayer> layers, bool redrawNewLayers, bool redrawOldLayers)
    {
        HashSet<AbstractLayer> oldLayers = nonReferenceLayers;
        nonReferenceLayers = layers;
        if (redrawOldLayers && oldLayers != null)
        {
            foreach (AbstractLayer layer in oldLayers)
                layer.RedrawGameObjects(CameraManager.Instance.gameCamera);

            if (redrawNewLayers)
            {
                //Dont redraw layers that were also in old layers
                foreach (AbstractLayer layer in nonReferenceLayers)
                    if(!oldLayers.Contains(layer))
                        layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
            }
        }
        else if (redrawNewLayers)
        {
            foreach (AbstractLayer layer in nonReferenceLayers)              
                layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
        }
    }

    public static bool IsReferenceLayer(AbstractLayer layer)
    {
        if (nonReferenceLayers == null)
            return false;
        return !nonReferenceLayers.Contains(layer);
    }

    public static void ClearNonReferenceLayers()
    {
        nonReferenceLayers = null;
    }

	public static string MakeCategoryDisplayString(string subcategory)
	{
		StringBuilder result = new StringBuilder(subcategory);
		result.Replace('_', ' ');
		result[0] = char.ToUpperInvariant(result[0]);
		for (int i = 1; i < result.Length; ++i)
		{
			if (result[i] == ' ' && i + 1 < result.Length)
			{
				result[i + 1] = char.ToUpperInvariant(result[i + 1]);
			}
		}

		return result.ToString();
	}
}
