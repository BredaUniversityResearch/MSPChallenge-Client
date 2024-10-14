using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerManager : MonoBehaviour
	{
		public enum EGeoType { Polygon, Line, Point, Raster }

		private static LayerManager Singleton;
		public static LayerManager Instance
		{
			get
			{
				if (Singleton == null)
					Singleton = FindObjectOfType<LayerManager>();
				return Singleton;
			}
		}

		[SerializeField] private List<Sprite> m_subcategoryIcons = null;
		private Dictionary<string, Sprite> m_subcategoryIconDict;

		private List<AbstractLayer> m_layers = new List<AbstractLayer>();
		private HashSet<AbstractLayer> m_loadedLayers = new HashSet<AbstractLayer>();
		private HashSet<AbstractLayer> m_visibleLayers = new HashSet<AbstractLayer>();
		private HashSet<AbstractLayer> m_visibilityLockedLayers = new HashSet<AbstractLayer>();
		private HashSet<AbstractLayer> m_nonReferenceLayers; //Layers that are drawn as normal during edit mode

		public PolygonLayer m_eezLayer;
		public List<AbstractLayer> m_protectedAreaLayers = new List<AbstractLayer>();
		private Dictionary<string, List<AbstractLayer>> m_subcategoryToLayers = new Dictionary<string, List<AbstractLayer>>();

		private Dictionary<string, List<string>> m_categorySubcategories = new Dictionary<string, List<string>>();
		private bool m_finishedImporting = false;

		public AbstractLayer m_highLightedLayer;

		public delegate void OnLayerVisibilityChanged(AbstractLayer a_layer, bool a_visible);
		public event OnLayerVisibilityChanged m_onLayerVisibilityChanged;
		public event OnLayerVisibilityChanged m_onLayerVisibilityLockChanged;

		[HideInInspector] public event Action<AbstractLayer> OnLayerLoaded;
		[HideInInspector] public event Action<Plan> OnVisibleLayersUpdatedToPlan;
		[HideInInspector] public event Action OnVisibleLayersUpdatedToBase;
		[HideInInspector] public event Action<int> OnVisibleLayersUpdatedToTime;

		private void Start()
		{
			if (Singleton != null && Singleton != this)
				Destroy(this);
			else
				Singleton = this;
			SetSubcategoryIcons();
		}

		private void OnDestroy()
		{
			Singleton = null;
		}

		public void AddLayer(AbstractLayer a_layer)
		{
			m_finishedImporting = false;
			while (a_layer.m_id >= m_layers.Count)
			{
				m_layers.Add(null);
			}
			m_layers[a_layer.m_id] = a_layer;

			if (a_layer.FileName == SessionManager.Instance.MspGlobalData.countries)
				m_eezLayer = a_layer as PolygonLayer;
			if(m_subcategoryToLayers.TryGetValue(a_layer.m_subCategory, out var entry))
			{
				entry.Add(a_layer);
			}
			else
			{
				m_subcategoryToLayers.Add(a_layer.m_subCategory, new List<AbstractLayer>() { a_layer });
			}
		}

		public void FinishedImportingLayers()
		{
			PopulateAllCountryIDs();
			m_finishedImporting = true;
			Debug.Log("All layers imported (" + GetValidLayerCount() + ")");

			//foreach(var kvp in m_subcategoryToLayers)
			//{
			//	kvp.Value.Sort((a_x, a_y) => a_x.ShortName.CompareTo(a_y.ShortName));
			//}
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
			if (m_eezLayer == null)
				return;

			//Set the EEZs own country id
			foreach (Entity ent in m_eezLayer.Entities)
				ent.Country = ent.EntityTypes[0].value;

			foreach (AbstractLayer tLayer in m_loadedLayers)
			{
				if (tLayer.m_id == m_eezLayer.m_id)
					continue;

				if (tLayer is PointLayer)
				{
					foreach (PointEntity tPointEntity in (tLayer as PointLayer).Entities)
					{
						if (tPointEntity.Country > 0)
							continue;
						foreach (PolygonEntity tCountryEntity in m_eezLayer.Entities)
						{
							if (!Util.PolygonPointIntersection(tCountryEntity.GetPolygonSubEntity(),
								tPointEntity.GetPointSubEntity()))
								continue;
							//the .value from EntityType in EEZLayers is the ID
							tPointEntity.Country = tCountryEntity.EntityTypes[0].value;
							break; //Early out skip to the next PointEntity
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
						foreach (PolygonEntity tCountryEntity in m_eezLayer.Entities)
						{
							if (!Util.PolygonPolygonIntersection(tCountryEntity.GetPolygonSubEntity(),
								tPolyEntity.GetPolygonSubEntity()))
								continue;
							//the .value from EntityType in EEZLayers is the ID
							tPolyEntity.Country = tCountryEntity.EntityTypes[0].value;
							break; //Early out skip to the next PolyEntity
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
						foreach (PolygonEntity tCountryEntity in m_eezLayer.Entities)
						{
							if (!Util.PolygonLineIntersection(tCountryEntity.GetPolygonSubEntity(),
								tLineStringEntity.GetLineStringSubEntity()))
								continue;
							//the .value from EntityType in EEZLayers is the ID
							tLineStringEntity.Country = tCountryEntity.EntityTypes[0].value;
							break; //Early out skip to the next LineStringEntity
						}
					}
				}
			}
		}

		public int GetLayerCount()
		{
			return m_layers.Count;
		}

		private int GetValidLayerCount()
		{
			int result = 0;
			foreach (AbstractLayer layer in m_layers)
			{
				if (layer != null)
				{
					result++;
				}
			}
			return result;
		}

		public AbstractLayer GetLayerByID(int a_layerID)
		{
			return m_layers[a_layerID];
		}

        public AbstractLayer GetLayerByUniqueTag(string a_layerTag)
        {
            foreach (var layer in m_layers)
            {
                if (layer.m_tags.Contains(a_layerTag))
                {
                    return layer;
                }
            }
            return null;
        }

        public List<AbstractLayer> GetLoadedLayers(string a_category, string a_subcategory)
		{
			List<AbstractLayer> result = new List<AbstractLayer>();
			foreach (AbstractLayer layer in m_layers)
			{
				if (!m_loadedLayers.Contains(layer))
					continue;
				if (layer.m_category == a_category && layer.m_subCategory == a_subcategory)
				{
					result.Add(layer);
				}
			}

			return result;
		}


		public List<AbstractLayer> GetLoadedLayers()
		{
			List<AbstractLayer> result = new List<AbstractLayer>();
			foreach (AbstractLayer layer in m_layers)
			{
				if (m_loadedLayers.Contains(layer))
				{
					result.Add(layer);
				}
			}

			return result;
		}

		public AbstractLayer FindFirstLayerContainingName(string a_name)
		{
			foreach (AbstractLayer layer in m_loadedLayers)
			{
				if (layer.FileName.IndexOf(a_name, StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					return layer;
				}
			}
			return null;
		}

		public AbstractLayer FindLayerByFilename(string a_name)
		{
			foreach (AbstractLayer layer in m_loadedLayers)
			{
				if (layer.FileName.Equals(a_name, StringComparison.InvariantCultureIgnoreCase))
				{
					return layer;
				}
			}
			return null;
		}

		public AbstractLayer GetLoadedLayer(int a_id)
		{
			AbstractLayer layer = GetLayerByID(a_id);

			if (m_loadedLayers.Contains(layer))
			{
				return layer;
			}

			return null;
		}

		public List<AbstractLayer> GetAllLayers()
		{
			List<AbstractLayer> result = new List<AbstractLayer>();
			foreach (AbstractLayer layer in m_layers)
			{
				if (layer != null)
				{
					result.Add(layer);
				}
			}

			return result;
		}

		public List<AbstractLayer> GetAllLayersOfGroup(string a_group)
		{
			if (a_group == string.Empty)
			{
				return GetAllLayers();
			}

			List<AbstractLayer> result = new List<AbstractLayer>();
			foreach (AbstractLayer layer in m_layers)
			{
				if (layer != null && layer.Group == a_group)
				{
					result.Add(layer);
				}
			}

			return result;
		}

		public Dictionary<string, List<string>> GetCategorySubcategories()
		{
			return m_categorySubcategories;
		}

		public void LoadLayer(AbstractLayer a_layer, List<SubEntityObject> a_layerObjects)
		{
			a_layer.LoadLayerObjects(a_layerObjects);

			a_layer.DrawGameObject();

			if (!a_layer.m_dirty)
			{
				m_loadedLayers.Add(a_layer);

				AddToCategories(a_layer.m_category, a_layer.m_subCategory);

				InterfaceCanvas.Instance.layerInterface.AddLayerToInterface(a_layer);

				if (a_layer.ActiveOnStart)
				{
					ShowLayer(a_layer);
				}
				else if (Main.Instance.LayerSelectedForCurrentExpertise(a_layer.FileName))
				{
					InterfaceCanvas.Instance.activeLayers.AddPinnedInvisibleLayer(a_layer);
				}

				a_layer.m_loaded = true;
			}
			else
			{
				Debug.LogError("(Corrupt) Layer has been found dirty: " + a_layer.ShortName);
			}
		}

		public void ShowLayer(AbstractLayer a_layer)
		{
			bool needsUpdateAndRedraw = false;
			foreach (EntityType entityType in a_layer.m_entityTypes.Values)
			{
				bool newNeed = a_layer.SetEntityTypeVisibility(entityType, true);
				needsUpdateAndRedraw = needsUpdateAndRedraw || newNeed;
			}

			if (!m_visibleLayers.Contains(a_layer))
			{
				//Performs a more elaborate update and redraw, so no other is needed
				needsUpdateAndRedraw = false;

				if (PlanManager.Instance.m_planViewing != null || PlanManager.Instance.m_timeViewing < 0)
					a_layer.SetEntitiesActiveUpTo(PlanManager.Instance.m_planViewing);
				else
					a_layer.SetEntitiesActiveUpToTime(PlanManager.Instance.m_timeViewing);

				a_layer.LayerGameObject.SetActive(true);
				m_visibleLayers.Add(a_layer);
				a_layer.LayerShown();
				a_layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
				a_layer.UpdateScale(CameraManager.Instance.gameCamera);

				if (a_layer.Toggleable)
				{
					//Show in Layer Select and Active Layers
					if (m_onLayerVisibilityChanged != null)
						m_onLayerVisibilityChanged.Invoke(a_layer, true);
				}
			}

			if (needsUpdateAndRedraw)
				a_layer.SetActiveToCurrentPlanAndRedraw();

			UpdateVisibleLayerIndexForAllTypes();
		}

		public void SetLayerVisibilityLock(AbstractLayer a_layer, bool a_locked)
		{
			if (a_locked)
				m_visibilityLockedLayers.Add(a_layer);
			else
				m_visibilityLockedLayers.Remove(a_layer);
			m_onLayerVisibilityLockChanged.Invoke(a_layer, a_locked);
		}

		public bool IsLayerVisibilityLocked(AbstractLayer a_layer)
		{
			return m_visibilityLockedLayers.Contains(a_layer);
		}

		public void HideLayer(AbstractLayer a_layer)
		{
			//Layer that is being edited cannot be hidden
			if (Main.InEditMode && InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer == a_layer)
				return;

			if (m_visibleLayers.Contains(a_layer))
			{
				a_layer.LayerGameObject.SetActive(false);
				m_visibleLayers.Remove(a_layer);
				a_layer.LayerHidden();

				//hide in Layer Select and Active Layers
				if (a_layer.Toggleable)
				{
					if (m_onLayerVisibilityChanged != null)
						m_onLayerVisibilityChanged.Invoke(a_layer, false);
				}
			}

			UpdateVisibleLayerIndexForAllTypes();
		}

		private void UpdateVisibleLayerIndexForAllTypes()
		{
			Dictionary<Type, int> visibleLayerIndexByType = new Dictionary<Type, int>();
			foreach (AbstractLayer layer in m_visibleLayers)
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
		/// <param name="a_plan"></param>
		public void UpdateVisibleLayersToPlan(Plan a_plan)
		{
			foreach (AbstractLayer layer in m_visibleLayers)
			{
				layer.SetEntitiesActiveUpTo(a_plan);
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}

			foreach (PlanLayer planLayer in a_plan.PlanLayers)
				if (!m_visibleLayers.Contains(planLayer.BaseLayer))
					ShowLayer(planLayer.BaseLayer);

			if (OnVisibleLayersUpdatedToPlan != null)
				OnVisibleLayersUpdatedToPlan(a_plan);
		}

		public void UpdateVisibleLayersToBase()
		{
			foreach (AbstractLayer layer in m_visibleLayers)
			{
				//layer.SetEntitiesActiveUpToCurrentTime();
				layer.SetEntitiesActiveUpTo(-1);
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			if (OnVisibleLayersUpdatedToBase != null)
				OnVisibleLayersUpdatedToBase();
		}

		public void UpdateVisibleLayersToTime(int a_month)
		{
			foreach (AbstractLayer layer in m_visibleLayers)
			{
				layer.SetEntitiesActiveUpToTime(a_month);
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			if (OnVisibleLayersUpdatedToTime != null)
				OnVisibleLayersUpdatedToTime(a_month);
		}

		public void UpdateLayerToPlan(AbstractLayer a_baseLayer, Plan a_plan, bool a_showIfHidden)
		{
			if (m_visibleLayers.Contains(a_baseLayer))
			{
				a_baseLayer.SetEntitiesActiveUpTo(a_plan);
				a_baseLayer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			else if (a_showIfHidden)
				ShowLayer(a_baseLayer);
		}

		public void RedrawVisibleLayers()
		{
			foreach (AbstractLayer layer in m_visibleLayers)
			{
				layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
		}

		public bool LayerIsVisible(AbstractLayer a_layer)
		{
			return m_visibleLayers.Contains(a_layer);
		}

		public IEnumerable<AbstractLayer> GetVisibleLayers()
		{
			return m_visibleLayers;
		}

		public List<AbstractLayer> GetVisibleLayersSortedByDepth()
		{
			List<AbstractLayer> result = new List<AbstractLayer>(m_visibleLayers);
			result.Sort((a_x, a_y) => a_y.Depth.CompareTo(a_x.Depth));
			return result;
		}

		private List<AbstractLayer> GetLoadedLayersSortedByDepth()
		{
			List<AbstractLayer> result = new List<AbstractLayer>(m_loadedLayers);
			result.Sort((a_x, a_y) => a_y.Depth.CompareTo(a_x.Depth));
			return result;
		}

		public void UpdateLayerScales(Camera a_targetCamera)
		{
			foreach (AbstractLayer layer in m_visibleLayers)
			{
				if (layer.LayerGameObject.activeInHierarchy)
				{
					layer.UpdateScale(a_targetCamera);
				}
			}
		}

		public SubEntity GetSubEntity(AbstractLayer a_layer, int a_subEntityID)
		{
			for (int i = 0; i < a_layer.GetEntityCount(); i++)
			{
				Entity entity = a_layer.GetEntity(i);

				for (int j = 0; j < entity.GetSubEntityCount(); j++)
				{
					SubEntity tmpEntity = entity.GetSubEntity(j);
					if (!tmpEntity.HasDatabaseID())
						continue;
					if (tmpEntity.GetDatabaseID() == a_subEntityID)
					{
						return tmpEntity;
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
				layersInOrder[i].m_order = i;
				layersInOrder[i].LayerGameObject.transform.position = new Vector3(0, 0, -layersInOrder[i].m_order);
			}
		}

		private void AddToCategories(string a_category, string a_subcategory)
		{
			if (!m_categorySubcategories.ContainsKey(a_category))
			{
				m_categorySubcategories.Add(a_category, new List<string>());
			}

			if (!m_categorySubcategories[a_category].Contains(a_subcategory))
			{
				m_categorySubcategories[a_category].Add(a_subcategory);
			}
		}

		/// <summary>
		/// Use with care. Is quite an expensive call to make.
		/// </summary>
		/// <param name="a_persistentId"></param>
		/// <returns></returns>
		public SubEntity FindSubEntityByPersistentID(int a_persistentId)
		{
			SubEntity result = null;
			for (int i = 0; i < m_layers.Count; ++i)
			{
				AbstractLayer layer = m_layers[i];
				if (layer == null)
					continue;
				SubEntity foundEntity = layer.GetSubEntityByPersistentID(a_persistentId);
				if (foundEntity == null)
					continue;
				result = foundEntity;
				break;
			}
			return result;
		}

		/// <summary>
		/// Calls SetEntitiesActiveUpTo current viewing plan for all layers that would be altered by this plans update.
		/// Takes plan's time into account.
		/// Doesn't run in edit mode.
		/// </summary>
		/// <param name="a_plan"></param>
		public void UpdateVisibleLayersFromPlan(Plan a_plan)
		{
			//Dont update layers while in edit mode, this is handled in ActivePlanWindow
			if (Main.InEditMode)
				return;

			//Only update if we are viewing the plan or one further in the future
			if (PlanManager.Instance.m_planViewing == null ||
			    (PlanManager.Instance.m_planViewing.StartTime < a_plan.StartTime ||
			     (PlanManager.Instance.m_planViewing.StartTime == a_plan.StartTime && PlanManager.Instance.m_planViewing.ID < a_plan.ID)))
				return;

			//Only update if already visible
			foreach (PlanLayer layer in a_plan.PlanLayers)
				if (m_visibleLayers.Contains(layer.BaseLayer))
					layer.BaseLayer.SetEntitiesActiveUpTo(PlanManager.Instance.m_planViewing);
		}

		public void AddNonReferenceLayer(AbstractLayer a_layer, bool a_redrawLayer)
		{
			if (m_nonReferenceLayers == null)
				m_nonReferenceLayers = new HashSet<AbstractLayer>() { a_layer };
			else
				m_nonReferenceLayers.Add(a_layer);
			if (a_redrawLayer)
				a_layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}

		public void SetNonReferenceLayers(HashSet<AbstractLayer> a_layers, bool a_redrawNewLayers, bool a_redrawOldLayers)
		{
			HashSet<AbstractLayer> oldLayers = m_nonReferenceLayers;
			m_nonReferenceLayers = a_layers;
			if (a_redrawOldLayers && oldLayers != null)
			{
				foreach (AbstractLayer layer in oldLayers)
					layer.RedrawGameObjects(CameraManager.Instance.gameCamera);

				if (!a_redrawNewLayers)
					return;
				//Dont redraw layers that were also in old layers
				foreach (AbstractLayer layer in m_nonReferenceLayers)
					if(!oldLayers.Contains(layer))
						layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
			else if (a_redrawNewLayers)
			{
				foreach (AbstractLayer layer in m_nonReferenceLayers)
					layer.RedrawGameObjects(CameraManager.Instance.gameCamera);
			}
		}

		public bool IsReferenceLayer(AbstractLayer a_layer)
		{
			if (m_nonReferenceLayers == null)
				return false;
			return !m_nonReferenceLayers.Contains(a_layer);
		}

		public void ClearNonReferenceLayers()
		{
			m_nonReferenceLayers = null;
		}

		public string MakeCategoryDisplayString(string a_subcategory)
		{
			if(string.IsNullOrEmpty(a_subcategory))
			{
				return "Empty subcategory";
			}
			StringBuilder result = new StringBuilder(a_subcategory);
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
			if (m_subcategoryToLayers.TryGetValue(a_subcategory, out List<AbstractLayer> layers))
			{
				return layers;
			}
			return null;
		}

		public void InvokeLayerLoaded(AbstractLayer a_layer)
		{
			OnLayerLoaded.Invoke(a_layer);
		}
	}
}
