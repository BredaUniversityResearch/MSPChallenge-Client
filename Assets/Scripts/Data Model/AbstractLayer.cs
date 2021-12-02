using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public abstract class AbstractLayer
{
    public delegate string PresetPropertyDelegate(SubEntity subentity);
	public const string POINT_SPRITE_ROOT_FOLDER = "points/";

	public enum EditingType { Normal, Cable, Transformer, Socket, SourcePoint, SourcePolygon, SourcePolygonPoint };

	public int ID;
	public string FileName { get; set; }
	public string ShortName { get; set; }
	public string Media { get; set; }
	public string Group { get; set; }
	public string Tooltip { get; set; }
	public string Category;
	public string SubCategory;
	public float Depth { get; set; }
	public int AssemblyTime { get; set; }
	public bool Loaded = false;
	public bool Dirty = false;
	public int versionNr;
    public LayerTextInfo textInfo;
    protected bool layerTextVisible = true;
	public ELayerKPICategory LayerKPICategory = ELayerKPICategory.Miscellaneous;

	//DRAWING
	protected Dictionary<string, int> layerStates;
	public float Order;
	public Dictionary<int, EntityType> EntityTypes;
	private HashSet<EntityType> visibleEntityTypes = new HashSet<EntityType>();
	public bool MultiTypeSelect = false;

	//EDITING
	public bool Selectable;
	public bool Editable;
	public bool Toggleable { get; protected set; }
	public bool ActiveOnStart { get; protected set; }
	public bool Optimized { get; protected set; }
	public EditingType editingType;
	public bool greenEnergy;

	//PLANS
	public List<PlanLayer> planLayers;
	protected int lastImplementedPlanIndex = -1; //The index of the last planlayer that has been merged with the baselayer
	protected PlanLayer currentPlanLayer; //The plan layer currently being shown

	//UNITY
	public static Transform LayerRoot { get; private set; }
	public GameObject LayerGameObject { get; protected set; }

	public delegate void EntityTypeVisibilityChangedDelegate(EntityType entityType, bool newVisibilityState);
	public event EntityTypeVisibilityChangedDelegate OnEntityTypeVisibilityChanged;

	public List<EntityPropertyMetaData> propertyMetaData = new List<EntityPropertyMetaData>();
    public Dictionary<string, PresetPropertyDelegate> presetProperties;

	public const string ASSEMBLY_STATE = "ASSEMBLY";

	static AbstractLayer()
	{
		GameObject entityParentObject = new GameObject("Layers");
		LayerRoot = entityParentObject.transform;
	}

	protected AbstractLayer(LayerMeta layerMeta)
	{
		layerStates = new Dictionary<string, int>();
		planLayers = new List<PlanLayer>();
        presetProperties = new Dictionary<string, PresetPropertyDelegate>();

         ID = layerMeta.layer_id;
		FileName = layerMeta.layer_name;
		ShortName = layerMeta.layer_short;
		Media = layerMeta.layer_media;
		Group = layerMeta.layer_group;
		Tooltip = layerMeta.layer_tooltip;
		Category = layerMeta.layer_category;
		SubCategory = layerMeta.layer_subcategory;
		Depth = Util.ParseToFloat(layerMeta.layer_depth, 0.0f);
		LayerKPICategory = layerMeta.layer_kpi_category;

		if (layerMeta.layer_editing_type == "protection")
		{
			MultiTypeSelect = true;
			LayerManager.protectedAreaLayers.Add(this);
		}
		else
		{
			MultiTypeSelect = layerMeta.layer_editing_type == "multitype";
		}

		Selectable = layerMeta.layer_selectable;
		Editable = layerMeta.layer_editable;
		Toggleable = layerMeta.layer_toggleable;
		ActiveOnStart = layerMeta.layer_active_on_start || Main.LayerVisibleForCurrentExpertise(layerMeta.layer_name);
		greenEnergy = layerMeta.layer_green == 1;
        Optimized = !Selectable && !Editable && !Toggleable && ActiveOnStart;

        ParseEntityPropertyMetaData(layerMeta.layer_info_properties);
		EntityTypes = ParseEntityTypes(layerMeta.layer_type);
		visibleEntityTypes.UnionWith(EntityTypes.Values);
        if(layerMeta.layer_text_info != null && layerMeta.layer_text_info.property_per_state != null)
		    textInfo = new LayerTextInfo(layerMeta.layer_text_info);	
		
		if (layerMeta.layer_states != "")
		{
			List<LayerStateObject> layerstateObject = new List<LayerStateObject>();
			try
			{
				layerstateObject = JsonConvert.DeserializeObject<List<LayerStateObject>>(layerMeta.layer_states);
			}
			catch
			{
				Debug.LogError("Failed to deserialize: " + FileName + " trying to parse " + layerMeta.layer_states);
			}

			foreach (LayerStateObject obj in layerstateObject)
			{
				layerStates.Add(obj.state, obj.time);
			}

			if (layerStates.ContainsKey(ASSEMBLY_STATE))
				AssemblyTime = layerStates[ASSEMBLY_STATE];
		}

		switch (layerMeta.layer_editing_type)
		{
			case "cable":
				editingType = EditingType.Cable;
				break;
			case "transformer":
				editingType = EditingType.Transformer;
				LayerManager.AddEnergyPointLayer(this as PointLayer);
				break;
			case "socket":
				editingType = EditingType.Socket;
				LayerManager.AddEnergyPointLayer(this as PointLayer);
				break;
			case "sourcepoint":
				editingType = EditingType.SourcePoint;
				LayerManager.AddEnergyPointLayer(this as PointLayer);
				break;
			case "sourcepolygon":
				editingType = EditingType.SourcePolygon;
				break;
			default:
				editingType = EditingType.Normal;
				break;
		}
        if (editingType != EditingType.Normal)
        {
            presetProperties.Add("MaxCapacity", (subent) => 
            {
                ValueConversionCollection valueConversions = VisualizationUtil.VisualizationSettings.ValueConversions;
                IEnergyDataHolder data = (IEnergyDataHolder)subent;
                //return data.Capacity.ToString();
                return valueConversions.ConvertUnit(data.Capacity, ValueConversionCollection.UNIT_WATT).FormatAsString();
            });
            presetProperties.Add("UsedCapacity", (subent) =>
            {
                ValueConversionCollection valueConversions = VisualizationUtil.VisualizationSettings.ValueConversions;
                IEnergyDataHolder data = (IEnergyDataHolder)subent;
                //return data.UsedCapacity.ToString() + " / " + data.Capacity.ToString();
                return valueConversions.ConvertUnit(data.UsedCapacity, ValueConversionCollection.UNIT_WATT).FormatAsString() + " / " + valueConversions.ConvertUnit(data.Capacity, ValueConversionCollection.UNIT_WATT).FormatAsString();
            });
        }
        //if (Main.LayerSelectedForCurrentExpertise(layerMeta.layer_name))
        //    InterfaceCanvas.instance.activeLayers.AddLayer(this);
    }

	//FUNCTIONS
	public abstract void Initialise();
	public abstract bool IsEnergyPointLayer();
	public abstract bool IsEnergyLineLayer();
	public abstract bool IsEnergyPolyLayer();
	public abstract bool IsEnergyLayer();
	public abstract void LoadLayerObjects(List<SubEntityObject> layerObjects);
	//public abstract void TransformAllEntities(float scale, Vector3 translate);
	public abstract List<EntityType> GetEntityTypesByKeys(params int[] keys);
	public abstract EntityType GetEntityTypeByKey(int key);
	public abstract EntityType GetEntityTypeByName(string name);
	public abstract int GetEntityTypeKey(EntityType entityType);
	public abstract void RemoveSubEntity(SubEntity subEntity, bool uncreate = false);
	public abstract void RestoreSubEntity(SubEntity subEntity, bool recreate = false);
	public abstract List<EntityType> GetEntityTypesSortedByKey();
	public abstract HashSet<Entity> GetEntitiesOfType(EntityType type);
	public abstract HashSet<Entity> GetActiveEntitiesOfType(EntityType type);
	public abstract string GetShortName();
	public abstract void DrawGameObject();
	protected abstract void DrawGameObjects(Transform layerTransform);

	public void RedrawGameObjects(Camera targetCamera)
	{
		RedrawGameObjects(targetCamera, SubEntityDrawMode.Default);
	}

	public abstract void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode, bool forceScaleUpdate = false);
	public abstract void SubmitMetaData();
	protected abstract string MetaToJSON();
	public abstract Rect GetLayerBounds();
	public abstract List<Entity> GetEntitiesAt(Vector2 position);
	public abstract SubEntity GetSubEntityByDatabaseID(int id);
	public abstract SubEntity GetSubEntityByPersistentID(int persistentID);
	public abstract HashSet<SubEntity> GetActiveSubEntities();
	public abstract int GetEntityCount();
	public abstract Entity GetEntity(int index);
	public abstract void UpdateScale(Camera targetCamera);
	public abstract Entity CreateEntity(SubEntityObject obj);
	public abstract Entity GetEntityAt(Vector2 position);
	public abstract SubEntity GetSubEntityAt(Vector2 position);
	public abstract List<SubEntity> GetSubEntitiesAt(Vector2 position);
	public abstract LayerManager.GeoType GetGeoType();
	public abstract void DrawSettingsUpdated();
	public abstract void LayerShown();
	public abstract void LayerHidden();

	/// <summary>
	/// Updates the visible layer index of the current layer type (Polygon, Point, Raster etc.)
	/// </summary>
	/// <param name="visibleIndexOfLayerType"></param>
	public virtual void UpdateVisibleIndexLayerType(int visibleIndexOfLayerType)
	{
	}

	public abstract PlanLayer CurrentPlanLayer();
	public abstract int AddPlanLayer(PlanLayer planLayer);
	public abstract int UpdatePlanLayerTime(PlanLayer planLayer);
	public abstract Entity AddObject(SubEntityObject obj);
	public abstract void RemovePlanLayer(PlanLayer planLayer);
	public abstract void RemovePlanLayerAndEntities(PlanLayer planLayer);
	public abstract void SetEntitiesActiveUpToTime(int month);
	public abstract void SetEntitiesActiveUpTo(int index, bool showRemovedInLatestPlan = true, bool showCurrentIfNotInfluencing = true);
	public abstract void SetEntitiesActiveUpTo(Plan plan);
    public abstract void SetEntitiesActiveUpToCurrentTime();
    
	public abstract bool IsIDInActiveGeometry(int ID);
	public abstract bool IsPersisIDCurrentlyNew(int persisID);
	public abstract bool IsDatabaseIDPreModified(int dataBaseID);
	public abstract void ActivateLastEntityWith(int persistentID);
	public abstract void DeactivateCurrentEntityWith(int persistentID);
	public abstract void AdvanceTimeTo(int time);
	protected abstract void MergePlanWithBaseUpToIndex(int newPlanIndex);
	public abstract void MergePlanWithBaseAndSubmitChanges(FSM fsm);
	public abstract LayerState GetLayerStateAtPlan(Plan plan);
	public abstract LayerState GetLayerStateAtTime(int month, Plan treatAsInfluencingState = null);
	public abstract LayerState GetLayerStateAtIndex(int planIndex);
	public abstract List<EnergyGrid> DetermineGrids(Plan plan, List<EnergyGrid> gridsInPlanPreviously, List<EnergyGrid> gridsBeforePlan, HashSet<int> removedGridsBefore, out HashSet<int> removedGridsAfter);
	public abstract void ActivateCableLayerConnections();
	public abstract void ResetEnergyConnections();
	public abstract void ResetCurrentGrids();
	public abstract List<EnergyLineStringSubEntity> RemoveInvalidCables();
	public abstract void RestoreInvalidCables(List<EnergyLineStringSubEntity> cables);
	public abstract List<SubEntity> GetFirstInstancesOfPersisIDBeforePlan(PlanLayer planLayer, HashSet<int> persistentIDs);

	public bool IsEntityTypeVisible(EntityType entityType)
	{
		return visibleEntityTypes.Contains(entityType);
	}

	public bool IsEntityTypeVisible(IEnumerable<EntityType> entityTypes)
	{
		bool result = false;
		foreach (EntityType type in entityTypes)
		{
			if (IsEntityTypeVisible(type))
			{
				result = true;
				break;
			}
		}
		return result;
	}

    /// <summary>
    /// Set the visibility of the given entity type.
    /// Returns whether a layer update and redraw is required based on this change.
    /// This update and redraw is not performed in this function so iit can be batched.
    /// </summary>
	public bool SetEntityTypeVisibility(EntityType entityType, bool visible)
	{
		bool stateChanged;
		if (visible)
		{
			stateChanged = visibleEntityTypes.Add(entityType);
		}
		else
		{
			stateChanged = visibleEntityTypes.Remove(entityType);
		}

		if (stateChanged)
		{
			if (OnEntityTypeVisibilityChanged != null)
			{
				OnEntityTypeVisibilityChanged.Invoke(entityType, visible);
			}
		}
        return stateChanged;
	}

    public void SetActiveToCurrentPlanAndRedraw()
    {
		if (PlanManager.planViewing != null || PlanManager.timeViewing < 0)
			SetEntitiesActiveUpTo(PlanManager.planViewing);
		else
			SetEntitiesActiveUpToTime(PlanManager.timeViewing);
        RedrawGameObjects(CameraManager.Instance.gameCamera);
    }

    public abstract bool LayerTextVisible
    {
        get;
        set;
    }

    //STATIC FUNCTIONS
    public static Dictionary<int, EntityType> ParseEntityTypes(Dictionary<int, EntityTypeValues> entityTypeValues)
	{
		Dictionary<int, EntityType> entityTypes = new Dictionary<int, EntityType>();
		foreach (var kvp in entityTypeValues)
		{
			Sprite pointSprite = null;
			if (!string.IsNullOrEmpty(kvp.Value.pointSpriteName) && kvp.Value.pointSpriteName != "None")
			{
				pointSprite = Resources.Load<Sprite>(POINT_SPRITE_ROOT_FOLDER + kvp.Value.pointSpriteName);
				if (pointSprite == null)
				{
					Debug.LogWarning("Could not load sprite at location " + POINT_SPRITE_ROOT_FOLDER + kvp.Value.pointSpriteName + " for entityType " + kvp.Key + " (" + kvp.Value.displayName + "). Is there a file at this location and are the import settings set correctly to be a sprite?");
				}
			}
			SubEntityDrawSettings drawSettings = new SubEntityDrawSettings(kvp.Value.displayPolygon, Util.HexToColor(kvp.Value.polygonColor), kvp.Value.polygonPatternName,
																		   kvp.Value.innerGlowEnabled, kvp.Value.innerGlowRadius, kvp.Value.innerGlowIterations, kvp.Value.innerGlowMultiplier, kvp.Value.innerGlowPixelSize,
																		   kvp.Value.displayLines, Util.HexToColor(kvp.Value.lineColor), kvp.Value.lineWidth, kvp.Value.lineIcon, Color.white, -1, kvp.Value.linePatternType,
                                                                           kvp.Value.displayPoints, Util.HexToColor(kvp.Value.pointColor), kvp.Value.pointSize, pointSprite);

			EntityType entityType = new EntityType(kvp.Value.displayName, kvp.Value.description, kvp.Value.capacity, kvp.Value.investmentCost, kvp.Value.availability, drawSettings, kvp.Value.media, kvp.Value.value, kvp.Value.approval);
			entityTypes.Add(kvp.Key, entityType);
		}

		return entityTypes;
	}

	public static Dictionary<string, EntityTypeValues> ParseEntityTypesValues(Dictionary<int, EntityType> entityTypes)
	{
		Dictionary<string, EntityTypeValues> entityTypeValues = new Dictionary<string, EntityTypeValues>();

		foreach (var kvp in entityTypes)
		{
			EntityTypeValues etv = new EntityTypeValues();

			EntityType type = kvp.Value;
			SubEntityDrawSettings drawSettings = type.DrawSettings;

			etv.displayName = type.Name;
			etv.displayPolygon = drawSettings.DisplayPolygon;
			etv.polygonColor = Util.ColorToHex(drawSettings.PolygonColor);
			etv.polygonPatternName = drawSettings.PolygonPatternName;
			etv.innerGlowEnabled = drawSettings.InnerGlowEnabled;
			etv.innerGlowRadius = drawSettings.InnerGlowRadius;
			etv.innerGlowIterations = drawSettings.InnerGlowIterations;
			etv.innerGlowMultiplier = drawSettings.InnerGlowMultiplier;
			etv.displayLines = drawSettings.DisplayLines;
			etv.lineColor = Util.ColorToHex(drawSettings.LineColor);
			etv.displayPoints = drawSettings.DisplayPoints;
			etv.pointColor = Util.ColorToHex(drawSettings.PointColor);
			etv.pointSize = drawSettings.PointSize;

			entityTypeValues.Add("" + kvp.Key, etv);

		}

		return entityTypeValues;
	}

	private void ParseEntityPropertyMetaData(LayerInfoPropertiesObject[] metaInfoList)
	{
		if (metaInfoList != null)
		{
			propertyMetaData.Capacity = metaInfoList.Length;
			for (int i = 0; i < metaInfoList.Length; ++i)
			{
				LayerInfoPropertiesObject info = metaInfoList[i];
				EntityPropertyMetaData meta = new EntityPropertyMetaData(info.property_name, info.enabled, info.editable, info.display_name, info.sprite_name, info.default_value, info.update_visuals, info.update_text, info.update_calculation, info.content_type, info.content_validation, info.unit);
				propertyMetaData.Add(meta);
			}
		}
	}

	public EntityPropertyMetaData FindPropertyMetaDataByName(string propertyName)
	{
		EntityPropertyMetaData result = null;
		for (int i = 0; i < propertyMetaData.Count; ++i)
		{
			EntityPropertyMetaData meta = propertyMetaData[i];
			if (meta.PropertyName == propertyName)
			{
				result = meta;
			}
		}
		return result;
	}
}