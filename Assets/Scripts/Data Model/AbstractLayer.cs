using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class AbstractLayer
	{
		public delegate string PresetPropertyDelegate(SubEntity a_subentity);
		public const string POINT_SPRITE_ROOT_FOLDER = "points/";

		public enum EditingType { Normal, Cable, Transformer, Socket, SourcePoint, SourcePolygon, SourcePolygonPoint };

		public int m_id;
		public string[] m_tags;
		public string FileName { get; set; }
		public string ShortName { get; set; }
		public string Media { get; private set; }
		public string Group { get; set; }
		public string Tooltip { get; set; }
		public string m_category;
		public string m_subCategory;
		public float Depth { get; set; }
		public int AssemblyTime { get; set; }
		public bool m_loaded = false;
		public bool m_dirty = false;
		public int m_versionNr;
		public LayerTextInfo m_textInfo;
		protected bool m_layerTextVisible = true;
		public ELayerKPICategory m_layerKpiCategory = ELayerKPICategory.Miscellaneous;

		//DRAWING
		private Dictionary<string, int> m_layerStates;
		public float m_order;
		public Dictionary<int, EntityType> m_entityTypes;
		private HashSet<EntityType> m_visibleEntityTypes = new HashSet<EntityType>();
		public bool m_multiTypeSelect = false;

		//EDITING
		public bool m_selectable;
		public bool m_editable;
		public bool Toggleable { get; protected set; }
		public bool ActiveOnStart { get; protected set; }
		public bool Optimized { get; protected set; }
		public EditingType m_editingType;
		public bool m_greenEnergy;
		public AbstractLayer[] Dependencies { get; protected set; }

		//PLANS
		public List<PlanLayer> m_planLayers;
		protected int m_lastImplementedPlanIndex = -1; //The index of the last planlayer that has been merged with the baselayer
		protected PlanLayer m_currentPlanLayer; //The plan layer currently being shown

		//UNITY
		protected static Transform LayerRoot { get; private set; }
		public GameObject LayerGameObject { get; protected set; }

		public delegate void EntityTypeVisibilityChangedDelegate(EntityType a_entityType, bool a_newVisibilityState);
		public event EntityTypeVisibilityChangedDelegate OnEntityTypeVisibilityChanged;

		public List<EntityPropertyMetaData> m_propertyMetaData = new List<EntityPropertyMetaData>();
		public Dictionary<string, PresetPropertyDelegate> m_presetProperties;

		private const string ASSEMBLY_STATE = "ASSEMBLY";

		static AbstractLayer()
		{
			GameObject entityParentObject = new GameObject("Layers");
			LayerRoot = entityParentObject.transform;
		}

		protected AbstractLayer(LayerMeta a_layerMeta)
		{
			m_layerStates = new Dictionary<string, int>();
			m_planLayers = new List<PlanLayer>();
			m_presetProperties = new Dictionary<string, PresetPropertyDelegate>();

			m_id = a_layerMeta.layer_id;
			m_tags = a_layerMeta.layer_tags;
			FileName = a_layerMeta.layer_name;
			ShortName = a_layerMeta.layer_short;
			Media = a_layerMeta.layer_media;
			Group = a_layerMeta.layer_group;
			Tooltip = a_layerMeta.layer_tooltip;
			m_category = a_layerMeta.layer_category;
			m_subCategory = a_layerMeta.layer_subcategory;
			Depth = Util.ParseToFloat(a_layerMeta.layer_depth, 0.0f);
			m_layerKpiCategory = a_layerMeta.layer_kpi_category;

			if (a_layerMeta.layer_editing_type == "protection")
			{
				m_multiTypeSelect = true;
				LayerManager.Instance.m_protectedAreaLayers.Add(this);
			}
			else
			{
				m_multiTypeSelect = a_layerMeta.layer_editing_type == "multitype";
			}

			m_selectable = a_layerMeta.layer_selectable;
			m_editable = a_layerMeta.layer_editable;
			Toggleable = a_layerMeta.layer_toggleable;
			if (a_layerMeta.layer_name.StartsWith("_PLAYAREA"))//For some reason the playarea is set to toggleable
				Toggleable = false;
			ActiveOnStart = a_layerMeta.layer_active_on_start || Main.Instance.LayerVisibleForCurrentExpertise(a_layerMeta.layer_name);
			m_greenEnergy = a_layerMeta.layer_green == 1;
			//TODO CHECK: If this turns out to have no impact (country layer) remove the layer optimization code
			//Optimized = !Selectable && !Editable && !Toggleable && ActiveOnStart;

			ParseEntityPropertyMetaData(a_layerMeta.layer_info_properties);
			m_entityTypes = ParseEntityTypes(a_layerMeta.layer_type);
			m_visibleEntityTypes.UnionWith(m_entityTypes.Values);
			if(a_layerMeta.layer_text_info != null && a_layerMeta.layer_text_info.property_per_state != null)
				m_textInfo = new LayerTextInfo(a_layerMeta.layer_text_info);	
		
			if (a_layerMeta.layer_states != null && a_layerMeta.layer_states != "")
			{
				List<LayerStateObject> layerstateObject = new List<LayerStateObject>();
				try
				{
					layerstateObject = JsonConvert.DeserializeObject<List<LayerStateObject>>(a_layerMeta.layer_states);
				}
				catch
				{
					Debug.LogError("Failed to deserialize: " + FileName + " trying to parse " + a_layerMeta.layer_states);
				}

				foreach (LayerStateObject obj in layerstateObject)
				{
					m_layerStates.Add(obj.state, obj.time);
				}

				if (m_layerStates.ContainsKey(ASSEMBLY_STATE))
					AssemblyTime = m_layerStates[ASSEMBLY_STATE];
			}

			switch (a_layerMeta.layer_editing_type)
			{
				case "cable":
					m_editingType = EditingType.Cable;
					break;
				case "transformer":
					m_editingType = EditingType.Transformer;
					PolicyLogicEnergy.Instance.AddEnergyPointLayer(this as PointLayer);
					break;
				case "socket":
					m_editingType = EditingType.Socket;
					PolicyLogicEnergy.Instance.AddEnergyPointLayer(this as PointLayer);
					break;
				case "sourcepoint":
					m_editingType = EditingType.SourcePoint;
					PolicyLogicEnergy.Instance.AddEnergyPointLayer(this as PointLayer);
					break;
				case "sourcepolygon":
					m_editingType = EditingType.SourcePolygon;
					break;
				default:
					m_editingType = EditingType.Normal;
					break;
			}
			if (m_editingType == EditingType.Normal)
				return;
			m_presetProperties.Add("MaxCapacity", (a_subent) => 
			{
				ValueConversionCollection valueConversions = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
				IEnergyDataHolder data = (IEnergyDataHolder)a_subent;
				return valueConversions.ConvertUnit(data.Capacity, ValueConversionCollection.UNIT_WATT).FormatAsString();
			});
			m_presetProperties.Add("UsedCapacity", (a_subent) =>
			{
				ValueConversionCollection valueConversions = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
				IEnergyDataHolder data = (IEnergyDataHolder)a_subent;
				return valueConversions.ConvertUnit(data.UsedCapacity, ValueConversionCollection.UNIT_WATT).FormatAsString() + " / " + valueConversions.ConvertUnit(data.Capacity, ValueConversionCollection.UNIT_WATT).FormatAsString();
			});
		}

		public void LoadDependencies(LayerMeta a_layerMeta)
		{
			if (a_layerMeta.layer_dependencies == null)
				return;
			Dependencies = new AbstractLayer[a_layerMeta.layer_dependencies.Length];
			for(int i = 0; i < a_layerMeta.layer_dependencies.Length; i++)
			{
				Dependencies[i] = LayerManager.Instance.GetLayerByID(a_layerMeta.layer_dependencies[i]);
			}
		}

		//FUNCTIONS
		public abstract void Initialise();
		public abstract bool IsEnergyPointLayer();
		public abstract bool IsEnergyLineLayer();
		public abstract bool IsEnergyPolyLayer();
		public abstract bool IsEnergyLayer();
		public abstract void LoadLayerObjects(List<SubEntityObject> a_layerObjects);
		public abstract List<EntityType> GetEntityTypesByKeys(params int[] a_keys);
		public abstract EntityType GetEntityTypeByKey(int a_key);
		public abstract EntityType GetEntityTypeByName(string a_name);
		public abstract int GetEntityTypeKey(EntityType a_entityType);
		public abstract void RemoveSubEntity(SubEntity a_subEntity, bool a_uncreate = false);
		public abstract void RestoreSubEntity(SubEntity a_subEntity, bool a_recreate = false);
		public abstract List<EntityType> GetEntityTypesSortedByKey();
		public abstract HashSet<Entity> GetEntitiesOfType(EntityType a_type);
		public abstract HashSet<Entity> GetActiveEntitiesOfType(EntityType a_type);
		public abstract string GetShortName();
		public abstract void DrawGameObject();
		protected abstract void DrawGameObjects(Transform a_layerTransform);

		public void RedrawGameObjects(Camera a_targetCamera)
		{
			RedrawGameObjects(a_targetCamera, SubEntityDrawMode.Default);
		}

		protected abstract void RedrawGameObjects(Camera a_targetCamera, SubEntityDrawMode a_drawMode, bool a_forceScaleUpdate = false);
		public abstract void SubmitMetaData();
		protected abstract string MetaToJson();
		public abstract Rect GetLayerBounds();
		public abstract List<Entity> GetEntitiesAt(Vector2 a_position);
		public abstract SubEntity GetSubEntityByDatabaseID(int a_id);
		public abstract SubEntity GetSubEntityByPersistentID(int a_persistentID);
		public abstract HashSet<SubEntity> GetActiveSubEntities();
		public abstract int GetEntityCount();
		public abstract Entity GetEntity(int a_index);
		public abstract void UpdateScale(Camera a_targetCamera);
		public abstract Entity CreateEntity(SubEntityObject a_obj);
		public abstract Entity GetEntityAt(Vector2 a_position);
		public abstract SubEntity GetSubEntityAt(Vector2 a_position);
		public abstract List<SubEntity> GetSubEntitiesAt(Vector2 a_position);
		public abstract LayerManager.EGeoType GetGeoType();
		public abstract void DrawSettingsUpdated();
		public abstract void LayerShown();
		public abstract void LayerHidden();

		/// <summary>
		/// Updates the visible layer index of the current layer type (Polygon, Point, Raster etc.)
		/// </summary>
		/// <param name="a_visibleIndexOfLayerType"></param>
		public virtual void UpdateVisibleIndexLayerType(int a_visibleIndexOfLayerType)
		{
		}

		public abstract PlanLayer CurrentPlanLayer();
		public abstract int AddPlanLayer(PlanLayer a_planLayer);
		public abstract int UpdatePlanLayerTime(PlanLayer a_planLayer);
		public abstract Entity AddObject(SubEntityObject a_obj);
		public abstract void RemovePlanLayer(PlanLayer a_planLayer);
		public abstract void RemovePlanLayerAndEntities(PlanLayer a_planLayer);
		public abstract void SetEntitiesActiveUpToTime(int a_month);
		public abstract void SetEntitiesActiveUpTo(int a_index, bool a_showRemovedInLatestPlan = true, bool a_showCurrentIfNotInfluencing = true);
		public abstract void SetEntitiesActiveUpTo(Plan a_plan);
		public abstract void SetEntitiesActiveUpToCurrentTime();
    
		public abstract bool IsIDInActiveGeometry(int a_id);
		public abstract bool IsPersisIDCurrentlyNew(int a_persisID);
		public abstract bool IsDatabaseIDPreModified(int a_dataBaseID);
		public abstract void ActivateLastEntityWith(int a_persistentID);
		public abstract void DeactivateCurrentEntityWith(int a_persistentID);
		public abstract void AdvanceTimeTo(int a_time);
		protected abstract void MergePlanWithBaseUpToIndex(int a_newPlanIndex);
		public abstract LayerState GetLayerStateAtPlan(Plan a_plan);
		public abstract LayerState GetLayerStateAtTime(int a_month, Plan a_treatAsInfluencingState = null);
		public abstract LayerState GetLayerStateAtIndex(int a_planIndex);
		public abstract List<EnergyGrid> DetermineChangedGridsInPlan(Plan a_plan, List<EnergyGrid> a_gridsInPlanPreviously, List<EnergyGrid> a_gridsBeforePlan, HashSet<int> a_removedGrids);
		public abstract void ActivateCableLayerConnections();
		public abstract void ResetEnergyConnections();
		public abstract void ResetCurrentGrids();
		public abstract List<EnergyLineStringSubEntity> RemoveInvalidCables();
		public abstract void RestoreInvalidCables(List<EnergyLineStringSubEntity> a_cables);
		public abstract List<SubEntity> GetFirstInstancesOfPersisIDBeforePlan(PlanLayer a_planLayer, HashSet<int> a_persistentIDs);

		private bool IsEntityTypeVisible(EntityType a_entityType)
		{
			return m_visibleEntityTypes.Contains(a_entityType);
		}

		public bool IsEntityTypeVisible(IEnumerable<EntityType> a_entityTypes)
		{
			bool result = false;
			foreach (EntityType type in a_entityTypes)
			{
				if (!IsEntityTypeVisible(type))
					continue;
				result = true;
				break;
			}
			return result;
		}

		/// <summary>
		/// Set the visibility of the given entity type.
		/// Returns whether a layer update and redraw is required based on this change.
		/// This update and redraw is not performed in this function so iit can be batched.
		/// </summary>
		public bool SetEntityTypeVisibility(EntityType a_entityType, bool a_visible)
		{
			var stateChanged= a_visible ? m_visibleEntityTypes.Add(a_entityType) : m_visibleEntityTypes.Remove(a_entityType);
			if (!stateChanged)
				return false;
			if (OnEntityTypeVisibilityChanged != null)
			{
				OnEntityTypeVisibilityChanged.Invoke(a_entityType, a_visible);
			}
			return true;
		}

		public void SetActiveToCurrentPlanAndRedraw()
		{
			if (PlanManager.Instance.m_planViewing != null || PlanManager.Instance.m_timeViewing < 0)
				SetEntitiesActiveUpTo(PlanManager.Instance.m_planViewing);
			else
				SetEntitiesActiveUpToTime(PlanManager.Instance.m_timeViewing);
			RedrawGameObjects(CameraManager.Instance.gameCamera);
		}

		public abstract bool LayerTextVisible
		{
			get;
			set;
		}

		//STATIC FUNCTIONS
		private static Dictionary<int, EntityType> ParseEntityTypes(Dictionary<int, EntityTypeValues> a_entityTypeValues)
		{
			Dictionary<int, EntityType> entityTypes = new Dictionary<int, EntityType>();
			foreach (var kvp in a_entityTypeValues)
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
					kvp.Value.displayPoints, Util.HexToColor(kvp.Value.pointColor), kvp.Value.pointSize*0.5f, pointSprite);

				EntityType entityType = new EntityType(kvp.Value.displayName, kvp.Value.description, kvp.Value.capacity, kvp.Value.investmentCost, kvp.Value.availability, drawSettings, kvp.Value.media, kvp.Value.value, kvp.Value.approval);
				entityTypes.Add(kvp.Key, entityType);
			}

			return entityTypes;
		}

		public static Dictionary<string, EntityTypeValues> ParseEntityTypesValues(Dictionary<int, EntityType> a_entityTypes)
		{
			Dictionary<string, EntityTypeValues> entityTypeValues = new Dictionary<string, EntityTypeValues>();

			foreach (var kvp in a_entityTypes)
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

		private void ParseEntityPropertyMetaData(LayerInfoPropertiesObject[] a_metaInfoList)
		{
			if (a_metaInfoList == null)
				return;
			m_propertyMetaData.Capacity = a_metaInfoList.Length;
			for (int i = 0; i < a_metaInfoList.Length; ++i)
			{
				LayerInfoPropertiesObject info = a_metaInfoList[i];
				EntityPropertyMetaData meta = new EntityPropertyMetaData(info.property_name, info.enabled, info.editable, info.display_name, info.policy_type, info.sprite_name, info.default_value, info.update_visuals, info.update_text, info.update_calculation, info.content_type, info.content_validation, info.unit);
				m_propertyMetaData.Add(meta);
			}
		}

		public EntityPropertyMetaData FindPropertyMetaDataByName(string a_propertyName)
		{
			EntityPropertyMetaData result = null;
			for (int i = 0; i < m_propertyMetaData.Count; ++i)
			{
				EntityPropertyMetaData meta = m_propertyMetaData[i];
				if (meta.PropertyName == a_propertyName)
				{
					result = meta;
				}
			}
			return result;
		}
	}
}
