using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerManager : MonoBehaviour
	{
		public enum GeoType { polygon, line, point, raster }

		private static LayerManager singleton;
		public static LayerManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<LayerManager>();
				return singleton;
			}
		}

		[SerializeField] List<Sprite> m_subcategoryIcons = null;
		private Dictionary<string, Sprite> m_subcategoryIconDict;

		private List<AbstractLayer> layers = new List<AbstractLayer>();
		private HashSet<AbstractLayer> loadedLayers = new HashSet<AbstractLayer>();
		private HashSet<AbstractLayer> visibleLayers = new HashSet<AbstractLayer>();
		private HashSet<AbstractLayer> nonReferenceLayers; //Layers that are drawn as normal during edit mode

		public PolygonLayer EEZLayer;
		public List<AbstractLayer> protectedAreaLayers = new List<AbstractLayer>();
		private Dictionary<string, List<AbstractLayer>> m_subcategoryToLayers = new Dictionary<string, List<AbstractLayer>>();

		private Dictionary<string, List<string>> categorySubcategories = new Dictionary<string, List<string>>();
		private bool finishedImporting = false;

		public AbstractLayer highLightedLayer;

		public delegate void OnLayerVisibilityChanged(AbstractLayer a_layer, bool a_visible);
		public event OnLayerVisibilityChanged m_onLayerVisibilityChanged;

		[HideInInspector] public event Action<AbstractLayer> OnLayerLoaded;
		[HideInInspector] public event Action<Plan> OnVisibleLayersUpdatedToPlan;
		[HideInInspector] public event Action OnVisibleLayersUpdatedToBase;
		[HideInInspector] public event Action<int> OnVisibleLayersUpdatedToTime;

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
			SetSubcategoryIcons();
		}

		void OnDestroy()
		{
			singleton = null;
		}

		public void AddLayer(AbstractLayer layer)
		{
			finishedImporting = false;
			while (layer.ID >= layers.Count)
			{
				layers.Add(null);
			}
			layers[layer.ID] = layer;

			if (layer.FileName == SessionManager.Instance.MspGlobalData.countries)
				EEZLayer = layer as PolygonLayer;
			if(m_subcategoryToLayers.TryGetValue(layer.SubCategory, out var entry))
			{
				entry.Add(layer);
			}
			else
			{
				m_subcategoryToLayers.Add(layer.SubCategory, new List<AbstractLayer>() { layer });
			}
		}

		public void FinishedImportingLayers()
		{
			PopulateAllCountryIDs();
			finishedImporting = true;
			Debug.Log("All layers imported (" + GetValidLayerCount() + ")");

			foreach(var kvp in m_subcategoryToLayers)
			{
				kvp.Value.Sort((x, y) => x.ShortName.CompareTo(y.ShortName));
			}
		}

		private void SetSubcategoryIcons()
		{
			m_subcategoryIconDict = new Dictionary<string, Sprite>();

			if (m_subcategoryIcons != null)
			{
				for (int i = 0; i < m_subcategoryIcons.Count; i++)
				{
					m_subcategoryIconDict.Add(m_subcategoryIcons[i].name, m_subcategoryIcons[i]);
				}
			}
			else
			{
				Debug.LogError("Icons for layer categories are not assigned on " + gameObject.name);
			}
		}
		public Sprite GetSubcategoryIcon(string a_subcategory)
		{
			if (m_subcategoryIconDict.TryGetValue(a_subcategory, out var result))
			{
				return result;
			}
			return null;
		}


		public void PopulateAllCountryIDs()
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

		public int GetLayerCount()
		{
			return layers.Count;
		}

		public int GetValidLayerCount()
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

		public AbstractLayer GetLayerByID(int layerID)
		{
			return layers[layerID];
		}

		public List<AbstractLayer> GetLoadedLayers(string category, string subcategory)
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


		public List<AbstractLayer> GetLoadedLayers()
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

		public AbstractLayer FindFirstLayerContainingName(string name)
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

		public AbstractLayer FindLayerByFilename(string name)
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

		public AbstractLayer GetLoadedLayer(int ID)
		{
			AbstractLayer layer = GetLayerByID(ID);

			if (loadedLayers.Contains(layer))
			{
				return layer;
			}

			return null;
		}

		public List<AbstractLayer> GetAllLayers()
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

		public List<AbstractLayer> GetAllLayersOfGroup(string group)
		{
			if (group == string.Empty)
			{
				return GetAllLayers();
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

		public Dictionary<string, List<string>> GetCategorySubcategories()
		{
			return categorySubcategories;
		}

		public void LoadLayer(AbstractLayer layer, List<SubEntityObject> layerObjects)
		{
			layer.LoadLayerObjects(layerObjects);

			layer.DrawGameObject();

			if (!layer.Dirty)
			{
				loadedLayers.Add(layer);

				AddToCategories(layer.Category, layer.SubCategory);

				InterfaceCanvas.Instance.layerInterface.AddLayerToInterface(layer);

				if (layer.ActiveOnStart)
				{
					ShowLayer(layer);
				}
				else if (Main.Instance.LayerSelectedForCurrentExpertise(layer.FileName))
				{
					InterfaceCanvas.Instance.activeLayers.AddPinnedInvisibleLayer(layer);
				}

				layer.Loaded = true;
			}
			else
			{
				Debug.LogError("(Corrupt) Layer has been found dirty: " + layer.ShortName);
			}
		}

		public void UpdateAllVisibleLayersTo(Plan plan)
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				layer.SetEntitiesActiveUpTo(plan);
			}
		}

		public void ShowLayer(AbstractLayer layer)
		{
			bool needsUpdateAndRedraw = false;
			foreach (EntityType entityType in layer.EntityTypes.Values)
			{
				bool newNeed = layer.SetEntityTypeVisibility(entityType, true);
				needsUpdateAndRedraw = needsUpdateAndRedraw || newNeed;
			}

			if (!visibleLayers.Contains(layer))
			{
				//Performs a more elaborate update and redraw, so no other is needed
				needsUpdateAndRedraw = false;

				if (PlanManager.Instance.planViewing != null || PlanManager.Instance.timeViewing < 0)
					layer.SetEntitiesActiveUpTo(PlanManager.Instance.planViewing);
				else
					layer.SetEntitiesActiveUpToTime(PlanManager.Instance.timeViewing);

				layer.LayerGameObject.SetActive(true);
				visibleLayers.Add(layer);
				layer.LayerShown();
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
				layer.UpdateScale(CameraManager.Instance.gameCamera);

				if (layer.Toggleable)
				{
					//Show in Layer Select and Active Layers
					if (m_onLayerVisibilityChanged != null)
						m_onLayerVisibilityChanged.Invoke(layer, true);
				}
			}

			if (needsUpdateAndRedraw)
				layer.SetActiveToCurrentPlanAndRedraw();

			UpdateVisibleLayerIndexForAllTypes();
		}

		public void HideLayer(AbstractLayer layer)
		{
			//Layer that is being edited cannot be hidden
			if (Main.InEditMode && InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer == layer)
				return;

			if (visibleLayers.Contains(layer))
			{
				layer.LayerGameObject.SetActive(false);
				visibleLayers.Remove(layer);
				layer.LayerHidden();

				//hide in Layer Select and Active Layers
				if (layer.Toggleable)
				{
					if (m_onLayerVisibilityChanged != null)
						m_onLayerVisibilityChanged.Invoke(layer, false);
				}
			}

			UpdateVisibleLayerIndexForAllTypes();
		}

		private void UpdateVisibleLayerIndexForAllTypes()
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

		/// <summary>
		/// Sets entities in visible layers active to plan and shows layers in the plan that were not visible.
		/// </summary>
		/// <param name="plan"></param>
		public void UpdateVisibleLayersToPlan(Plan plan)
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				layer.SetEntitiesActiveUpTo(plan);
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}

			foreach (PlanLayer planLayer in plan.PlanLayers)
				if (!visibleLayers.Contains(planLayer.BaseLayer))
					ShowLayer(planLayer.BaseLayer);

			if (OnVisibleLayersUpdatedToPlan != null)
				OnVisibleLayersUpdatedToPlan(plan);
		}

		public void UpdateVisibleLayersToBase()
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				//layer.SetEntitiesActiveUpToCurrentTime();
				layer.SetEntitiesActiveUpTo(-1);
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			if (OnVisibleLayersUpdatedToBase != null)
				OnVisibleLayersUpdatedToBase();
		}

		public void UpdateVisibleLayersToTime(int month)
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				layer.SetEntitiesActiveUpToTime(month);
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			if (OnVisibleLayersUpdatedToTime != null)
				OnVisibleLayersUpdatedToTime(month);
		}

		public void UpdateLayerToPlan(AbstractLayer baseLayer, Plan plan, bool showIfHidden)
		{
			if (visibleLayers.Contains(baseLayer))
			{
				baseLayer.SetEntitiesActiveUpTo(plan);
				baseLayer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			else if (showIfHidden)
				ShowLayer(baseLayer);
		}

		public void RedrawVisibleLayers()
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
		}

		public bool LayerIsVisible(AbstractLayer layer)
		{
			return visibleLayers.Contains(layer);
		}

		public IEnumerable<AbstractLayer> GetVisibleLayers()
		{
			return visibleLayers;
		}

		public List<AbstractLayer> GetVisibleLayersSortedByDepth()
		{
			List<AbstractLayer> result = new List<AbstractLayer>(visibleLayers);
			result.Sort((x, y) => y.Depth.CompareTo(x.Depth));
			return result;
		}

		public List<AbstractLayer> GetLoadedLayersSortedByDepth()
		{
			List<AbstractLayer> result = new List<AbstractLayer>(loadedLayers);
			result.Sort((x, y) => y.Depth.CompareTo(x.Depth));
			return result;
		}

		public void UpdateLayerScales(Camera targetCamera)
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				if (layer.LayerGameObject.activeInHierarchy)
				{
					layer.UpdateScale(targetCamera);
				}
			}
		}

		public SubEntity GetSubEntity(AbstractLayer layer, int subEntityID)
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

		public void ReorderLayers()
		{
			List<AbstractLayer> layersInOrder = GetLoadedLayersSortedByDepth();
			layersInOrder.Reverse();

			for (int i = 0; i < layersInOrder.Count; i++)
			{
				layersInOrder[i].Order = i;
				layersInOrder[i].LayerGameObject.transform.position = new Vector3(0, 0, -layersInOrder[i].Order);
			}
		}

		private void AddToCategories(string category, string subcategory)
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

		/// <summary>
		/// Use with care. Is quite an expensive call to make.
		/// </summary>
		/// <param name="persistentId"></param>
		/// <returns></returns>
		public SubEntity FindSubEntityByPersistentID(int persistentId)
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
		public void UpdateVisibleLayersFromPlan(Plan plan)
		{
			//Dont update layers while in edit mode, quickly causes errors
			if (Main.InEditMode)
				return;

			//Only update if we are viewing the plan or one further in the future
			if (PlanManager.Instance.planViewing == null ||
			    (PlanManager.Instance.planViewing.StartTime < plan.StartTime ||
			     (PlanManager.Instance.planViewing.StartTime == plan.StartTime && PlanManager.Instance.planViewing.ID < plan.ID)))
				return;

			//Only update if already visible
			foreach (PlanLayer layer in plan.PlanLayers)
				if (visibleLayers.Contains(layer.BaseLayer))
					layer.BaseLayer.SetEntitiesActiveUpTo(PlanManager.Instance.planViewing);
		}

		public void AddNonReferenceLayer(AbstractLayer layer, bool redrawLayer)
		{
			if (nonReferenceLayers == null)
				nonReferenceLayers = new HashSet<AbstractLayer>() { layer };
			else
				nonReferenceLayers.Add(layer);
			if (redrawLayer)
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}

		public void SetNonReferenceLayers(HashSet<AbstractLayer> layers, bool redrawNewLayers, bool redrawOldLayers)
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

		public bool IsReferenceLayer(AbstractLayer layer)
		{
			if (nonReferenceLayers == null)
				return false;
			return !nonReferenceLayers.Contains(layer);
		}

		public void ClearNonReferenceLayers()
		{
			nonReferenceLayers = null;
		}

		public string MakeCategoryDisplayString(string subcategory)
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

		public List<AbstractLayer> GetLayersInSubcategory(string a_subcategory)
		{
			if(m_subcategoryToLayers.TryGetValue(a_subcategory, out List<AbstractLayer> layers))
			{
				return layers;
			}
			return null;
		}
	}
}
