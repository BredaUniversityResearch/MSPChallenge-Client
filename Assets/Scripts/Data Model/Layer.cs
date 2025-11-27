using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class Layer<T> : AbstractLayer where T : Entity
	{
		protected List<T> InitialEntities { get; private set; }	//Entities that existed on the base original layer
		public List<T> Entities { get; private set; } //Entities that exist at the games current time
		public HashSet<T> m_activeEntities; //Entities at the time of currently active plan
		public HashSet<T> PreModifiedEntities { get; private set; }	//Lastest geometry with the same persisID as new geometry in the current active planlayer
		private HashSet<int> PreExistingPersisIDs
		{
			get;
			set;
		}	//Persistent IDs that existed before the current plan

		public override bool LayerTextVisible
		{
			get => m_layerTextVisible;
			set {
				if (value == m_layerTextVisible)
					return;
				m_layerTextVisible = value;
				if (value)
				{
					foreach (T entity in Entities)
					{
						SubEntity sub = entity.GetSubEntity(0);
						if (sub.TextMeshVisibleAtZoom)
							sub.SetTextMeshActivity(true);
					}

					foreach (PlanLayer p in m_planLayers)
					{
						for (int i = 0; i < p.GetNewGeometryCount(); ++i)
						{
							SubEntity sub = p.GetNewGeometryByIndex(i).GetSubEntity(0);
							if (sub.TextMeshVisibleAtZoom)
								sub.SetTextMeshActivity(true);
						}
					}

				}
				else
				{
					foreach (T entity in Entities)
						entity.GetSubEntity(0).SetTextMeshActivity(false);

					foreach (PlanLayer p in m_planLayers)
						for (int i = 0; i < p.GetNewGeometryCount(); ++i)
							p.GetNewGeometryByIndex(i).GetSubEntity(0).SetTextMeshActivity(false);
				}
			}
		}

		protected Layer(LayerMeta a_layerMeta)
			: base(a_layerMeta)
		{
			Entities = new List<T>();
			InitialEntities = new List<T>();
			m_activeEntities = new HashSet<T>();

			Initialise();
		}

		public override void Initialise()
		{
			Entities.Clear();
			InitialEntities.Clear();
			m_activeEntities.Clear();
		}

		public override bool IsEnergyPointLayer()
		{
			return m_editingType == EditingType.Transformer || m_editingType == EditingType.Socket || m_editingType == EditingType.SourcePoint || m_editingType == EditingType.SourcePolygonPoint;
		}

		public override bool IsEnergyLineLayer()
		{
			return m_editingType == EditingType.Cable;
		}

		public override bool IsEnergyPolyLayer()
		{
			return m_editingType == EditingType.SourcePolygon;
		}

		public override bool IsEnergyLayer()
		{
			return m_editingType != EditingType.Normal;
		}

		public override void LoadLayerObjects(List<SubEntityObject> a_layerObjects)
		{
		}

		public override List<EntityType> GetEntityTypesByKeys(params int[] a_keys)
		{
			List<EntityType> types = new List<EntityType>();

			foreach (int key in a_keys)
			{
				if (m_entityTypes.ContainsKey(key))
				{
					types.Add(m_entityTypes[key]);
				}
			}
			return types;
		}

		public override EntityType GetEntityTypeByKey(int a_key)
		{
			return m_entityTypes.ContainsKey(a_key) ? m_entityTypes[a_key] : null;

		}

		public override EntityType GetEntityTypeByName(string a_name)
		{
			foreach (var kvp in m_entityTypes)
			{
				if (kvp.Value.Name == a_name) { return kvp.Value; }
			}

			return null;
		}

		public override int GetEntityTypeKey(EntityType a_entityType)
		{
			foreach (var kvp in this.m_entityTypes)
			{
				if (kvp.Value.Name == a_entityType.Name)
				{
					return kvp.Key;
				}
			}

			if (a_entityType == null) { Debug.LogError("Entity type error in " + FileName); }
			Debug.LogError("Failed to find key from entity type " + a_entityType.Name + " in " + FileName);

			return 0;
		}

		public override void RemoveSubEntity(SubEntity a_subEntity, bool a_uncreate = false)
		{
			T entity = (T)a_subEntity.m_entity;
			a_subEntity.RemoveDependencies();
			int persisID = a_subEntity.GetPersistentID();

			//If it wasnt in the curren't plan, simply add to removed geom
			if (entity.PlanLayer == m_currentPlanLayer)
			{
				if (entity.GetSubEntityCount() != 1)
				{
					//If there are no other subentities left in the entity
					throw new Exception("Entity has an invalid subentity count. Count: " + entity.GetSubEntityCount() + " on entity with database ID " + entity.DatabaseID);
				}
				m_currentPlanLayer.RemoveNewGeometry(entity);
				m_activeEntities.Remove(entity);

				//If the entity wasn't created in this plan, active the last entity with that persisID and add it to removedgeom
				if (!a_subEntity.m_entity.Layer.IsPersisIDCurrentlyNew(persisID))
				{
					if (!a_uncreate) //We are uncreating, only reactivate previous version, don't add to removed geom
					{
						AddPreModifiedEntity(a_subEntity.m_entity);
						entity.PlanLayer.RemovedGeometry.Add(persisID);
					}
					ActivateLastEntityWith(persisID);
				}
			}
			else //Was on another layer and removed by this plan. It's removed and displayed with (-) icons.
			{
				m_currentPlanLayer.RemovedGeometry.Add(persisID);
				AddPreModifiedEntity(a_subEntity.m_entity);
			}
			entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}

		public override void RestoreSubEntity(SubEntity a_subEntity, bool a_recreate = false)
		{
			T entity = (T)a_subEntity.m_entity;
			//entity.RestoreSubEntity(subEntity);
			a_subEntity.RestoreDependencies();
			int persistentID = a_subEntity.GetPersistentID();

			if (entity.PlanLayer == m_currentPlanLayer)
			{
				//This entity used to be inactive, and is now made active
				if (entity.GetSubEntityCount() == 1)
				{
					m_currentPlanLayer.AddNewGeometry(entity);

					// If another object was being displayed because of the deletion, deactivate it and show this instead
					if (!a_subEntity.m_entity.Layer.IsPersisIDCurrentlyNew(persistentID))
					{
						if (!a_recreate) //We are only recreating a newer version, the ID isn't in removedgeom
						{
							RemovePreModifiedEntity(a_subEntity.m_entity);
							entity.PlanLayer.RemovedGeometry.Remove(persistentID);
						}
						DeactivateCurrentEntityWith(persistentID);
					}

					//Only add to activeentities after calling DeactivateCurrentEntityWith
					m_activeEntities.Add(entity);
				}
			}
			else //Was on another layer and removed by this plan
			{
				m_currentPlanLayer.RemovedGeometry.Remove(persistentID);
				RemovePreModifiedEntity(a_subEntity.m_entity);
			}
			entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}

		public override List<EntityType> GetEntityTypesSortedByKey()
		{
			List<int> keys = new List<int>(m_entityTypes.Keys);
			keys.Sort();

			List<EntityType> result = new List<EntityType>();
			foreach (int key in keys)
			{
				result.Add(m_entityTypes[key]);
			}
			return result;
		}

		public override HashSet<Entity> GetEntitiesOfType(EntityType a_type)
		{
			HashSet<Entity> result = new HashSet<Entity>();

			int typeID = GetEntityTypeKey(a_type);
			foreach (T entity in Entities)
			{
				if (entity.GetEntityTypeKeys().Contains(typeID))
				{
					result.Add(entity);
				}
			}
			return result;
		}

		public override HashSet<Entity> GetActiveEntitiesOfType(EntityType a_type)
		{
			HashSet<Entity> result = new HashSet<Entity>();

			int typeID = GetEntityTypeKey(a_type);
			foreach (T entity in m_activeEntities)
			{
				if (entity.GetEntityTypeKeys().Contains(typeID))
				{
					result.Add(entity);
				}
			}
			return result;
		}

		public override string GetShortName()
		{
			return string.IsNullOrEmpty(ShortName) ? FileName : ShortName;
		}

		public override void DrawGameObject()
		{
			LayerGameObject = new GameObject(FileName);
			LayerGameObject.SetActive(false);
			LayerGameObject.transform.SetParent(LayerRoot);

			LayerGameObject.transform.position = new Vector3(0, 0, -m_order);
			if (!(this is RasterLayer))
				DrawGameObjects(LayerGameObject.transform);
		}

		protected override void DrawGameObjects(Transform a_layerTransform)
		{
			foreach (T entity in Entities)
			{
				entity.DrawGameObjects(a_layerTransform);
			}
		}

		protected override void RedrawGameObjects(Camera a_targetCamera, SubEntityDrawMode a_drawMode, bool a_forceScaleUpdate = false)
		{
			//Doesn't redraw geometry removed from Entities

			foreach (T entity in InitialEntities)
				entity.RedrawGameObjects(a_targetCamera, a_drawMode, a_forceScaleUpdate);

			foreach (PlanLayer p in m_planLayers)
			{
				for (int i = 0; i < p.GetNewGeometryCount(); ++i)
				{
					p.GetNewGeometryByIndex(i).RedrawGameObjects(a_targetCamera, a_drawMode, a_forceScaleUpdate);
				}
			}
		}

		public override void SubmitMetaData()
		{
			NetworkForm form = new NetworkForm();

			form.AddField("icon", "");
			form.AddField("category", m_category);
			form.AddField("subcategory", m_subCategory);
			form.AddField("type", MetaToJson());
			form.AddField("depth", Depth.ToString());
			form.AddField("short", ShortName);
			form.AddField("id", m_id);

			ServerCommunication.Instance.DoRequestForm(Server.PostLayerMeta(), form);
		}

		protected override string MetaToJson()
		{
			Dictionary<int, EntityTypeValues> types = new Dictionary<int, EntityTypeValues>();

			foreach (var kvp in m_entityTypes)
			{
				EntityTypeValues type = kvp.Value.DrawSettings.ToEntityTypeValues();
				kvp.Value.CopyEntityTypeValues(type);
				types.Add(kvp.Key, type);
			}

			return JsonConvert.SerializeObject(types);
		}

		public override Rect GetLayerBounds()
		{
			int entityCount = GetEntityCount();

			if (entityCount == 0) { return new Rect(); }

			Rect result = GetEntity(0).GetEntityBounds();
			for (int i = 1; i < entityCount; ++i)
			{
				Vector2 min = Vector2.Min(result.min, GetEntity(i).GetEntityBounds().min);
				Vector2 max = Vector2.Max(result.max, GetEntity(i).GetEntityBounds().max);
				result = new Rect(min, max - min);
			}

			return result;
		}

		public override List<Entity> GetEntitiesAt(Vector2 a_position)
		{
			List<SubEntity> subEntities = GetSubEntitiesAt(a_position);
			List<Entity> result = new List<Entity>();
			foreach (SubEntity subEntity in subEntities)
			{
				if (!result.Contains(subEntity.m_entity))
				{
					result.Add(subEntity.m_entity);
				}
			}
			return result;
		}

		public override SubEntity GetSubEntityByDatabaseID(int a_id)
		{
			for (int i = 0; i < GetEntityCount(); i++)
			{
				Entity entity = GetEntity(i);
				for (int j = 0; j < entity.GetSubEntityCount(); j++)
				{
					if (entity.GetSubEntity(j).GetDatabaseID() == a_id)
					{
						return entity.GetSubEntity(j);
					}
				}
			}

			return null;
		}

		public override SubEntity GetSubEntityByPersistentID(int a_persistentID)
		{
			for (int i = 0; i < GetEntityCount(); i++)
			{
				Entity entity = GetEntity(i);
				for (int j = 0; j < entity.GetSubEntityCount(); j++)
				{
					if (entity.GetSubEntity(j).GetPersistentID() == a_persistentID)
					{
						return entity.GetSubEntity(j);
					}
				}
			}
			return null;
		}

		public override int GetEntityCount()
		{
			return Entities.Count;
		}

		public override Entity GetEntity(int a_index)
		{
			return Entities[a_index];
		}

		public override HashSet<SubEntity> GetActiveSubEntities()
		{
			HashSet<SubEntity> result = new HashSet<SubEntity>();
			foreach (T entity in m_activeEntities)
			{
				if (!result.Add(entity.GetSubEntity(0)))
				{
					Debug.Log("Adding Duplicate entity in hashset.");
				}
			}
			return result;
		}

		public override void UpdateScale(Camera a_targetCamera)
		{ }
		public override Entity CreateEntity(SubEntityObject a_obj)
		{ return null; }
		public override Entity GetEntityAt(Vector2 a_position)
		{ return null; }
		public override SubEntity GetSubEntityAt(Vector2 a_position)
		{ return null; }
		public override List<SubEntity> GetSubEntitiesAt(Vector2 a_position)
		{ return null; }
		public override  LayerManager.EGeoType GetGeoType()
		{ return  LayerManager.EGeoType.Line; }

		//Called when the layer is shown by the layermanager
		public override void LayerShown()
		{
			foreach (SubEntity activeSubEntity in GetActiveSubEntities())
			{
				activeSubEntity.NotifySubEntityVisibilityChanged();
			}
		}
		//Called when the layer is hidden by the layermanager
		public override void LayerHidden()
		{
			foreach (SubEntity activeSubEntity in GetActiveSubEntities())
			{
				activeSubEntity.NotifySubEntityVisibilityChanged();
			}
		}

		public override void DrawSettingsUpdated()
		{
			int count = GetEntityCount();
			for (int i = 0; i < count; ++i)
			{
				GetEntity(i).UpdateGameObjectsForEveryLOD();
			}
		}

		/// <summary>
		/// Returns the planlayer that is currently set to active.
		/// </summary>
		public override PlanLayer CurrentPlanLayer()
		{
			return m_currentPlanLayer;
		}

		/// <summary>
		/// Adds the planlayer and makes sure the layers are sorted by implementation time (and then by the plan's db id)
		/// </summary>
		public override int AddPlanLayer(PlanLayer a_planLayer)
		{
			if (a_planLayer.Plan.State == Plan.PlanState.DELETED)
				Debug.LogError("Archived PlanLayer added to layer.");

			if (m_planLayers.Count == 0)
			{
				m_planLayers.Add(a_planLayer);
				return 0;
			}

			for (int i = 0; i < m_planLayers.Count; i++)
				if (m_planLayers[i].Plan.StartTime > a_planLayer.Plan.StartTime ||
				    (m_planLayers[i].Plan.StartTime == a_planLayer.Plan.StartTime && a_planLayer.Plan.ID >= 0 && m_planLayers[i].Plan.ID > a_planLayer.Plan.ID)) //If plan time is the same, sort by databaseID of the plan
				{
					if (i <= m_lastImplementedPlanIndex)
						Debug.LogError("PlanLayer added with an index lower than last implemented plan. Plans before the current time should be impossible.");
					m_planLayers.Insert(i, a_planLayer);
					return i;
				}

			//The planlayer should be the last element
			m_planLayers.Add(a_planLayer);
			return m_planLayers.Count - 1;
		}

		/// <summary>
		/// Should be called after a plan changes it's time.
		/// Moves the given plan layer to the correct time. Makes sure layers stay sorted.
		/// </summary>
		/// <returns>The given layer's new index. Can be use to get the PlanState for that layer.</returns>
		public override int UpdatePlanLayerTime(PlanLayer a_planLayer)
		{
			m_planLayers.Remove(a_planLayer);
			return AddPlanLayer(a_planLayer);
		}

		public override void RemovePlanLayer(PlanLayer a_planLayer)
		{
			m_planLayers.Remove(a_planLayer);
		}

		/// <summary>
		/// Removes the planlayer from this layer. Removes the planlayer's entities from active entities
		/// </summary>
		public override void RemovePlanLayerAndEntities(PlanLayer a_planLayer)
		{
			//If the removed planLayer was the current planlayer, set the one before active
			//All the planlayers active geom is deleted anyway, so no need to setActiveTo
			if (a_planLayer == m_currentPlanLayer)
			{
				int index = 0;
				for (int i = 0; i < m_planLayers.Count; i++)
				{
					if (m_planLayers[i] == a_planLayer)
					{
						index = i;
						break;
					}
				}
				if (index > 0)
				{
					m_currentPlanLayer = m_planLayers[index - 1];
					m_currentPlanLayer.SetEnabled(true);
				}
				else
					m_currentPlanLayer = null;
			}
			m_planLayers.Remove(a_planLayer);
			for (int i = 0; i < a_planLayer.GetNewGeometryCount(); ++i)
			{
				Entity entity = a_planLayer.GetNewGeometryByIndex(i);
				m_activeEntities.Remove((T)entity);
			}
		}

		public override void SetEntitiesActiveUpToTime(int a_month)
		{
			for (int i = m_planLayers.Count-1; i >= 0; i--)
			{
				//Find last plan at time (or a StartingPlan)
				if (m_planLayers[i].Plan.StartTime < 0 || m_planLayers[i].Plan.StartTime <= a_month)
				{
					//Since we are viewing a time, dont show removed geom and dont show geometry in last plan if it's not influencing.
					SetEntitiesActiveUpTo(i, false, false);
					return;
				}
			}

			//If no planlayers are later than chosen month, set intialEntities Active.
			SetEntitiesActiveUpToInitialTime();
		}

		public override void SetEntitiesActiveUpTo(Plan a_plan)
		{
			if (a_plan != null)
			{
				int index = m_planLayers.Count - 1;
				bool showRemoved = false;
				for (int i = 0; i < m_planLayers.Count; i++)
				{
					if (m_planLayers[i].Plan == a_plan)
					{
						index = i;
						showRemoved = true;
						break;
					}
					if (m_planLayers[i].Plan.StartTime > a_plan.StartTime || (a_plan.ID != -1 && m_planLayers[i].Plan.StartTime == a_plan.StartTime && a_plan.ID < m_planLayers[i].Plan.ID))
					{
						//Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
						index = i - 1;
						break;
					}
				}
				SetEntitiesActiveUpTo(index, showRemoved, a_plan.IsLayerpartOfPlan(this));
			}
			else
				SetEntitiesActiveUpTo(-1);
		}

		/// <summary>
		/// Sets activeEntities to encompass all entities from the base and plan layers that will be active at the given time.
		/// </summary>
		public override void SetEntitiesActiveUpTo(int a_index, bool a_showRemovedInLatestPlan = true, bool a_showCurrentIfNotInfluencing = true)
		{
			if (a_index < 0)
			{
				SetEntitiesActiveUpToCurrentTime();
			}
			else if (a_index <= m_lastImplementedPlanIndex)
			{
				//Plan is in the past, go back to initial geom
				SetEntitiesActiveUpToAdv(a_index, InitialEntities, -1, a_showRemovedInLatestPlan, a_showCurrentIfNotInfluencing);
			}
			else
			{
				//Plan is in the future, go back to current state
				SetEntitiesActiveUpToAdv(a_index, Entities, m_lastImplementedPlanIndex, a_showRemovedInLatestPlan, a_showCurrentIfNotInfluencing);
			}
		}

		private void SetEntitiesActiveUpToAdv(int a_index, List<T> a_baseEntities, int a_baseIndex, bool a_showRemovedInLatestPlan, bool a_showCurrentIfNotInfluencing)
		{
			if (m_currentPlanLayer != null)
			{
				m_currentPlanLayer.SetEnabled(false);
			}
			m_currentPlanLayer = m_planLayers[a_index];
			m_currentPlanLayer.SetEnabled(true);

			//Go through plans backwards from lastPlanIndex
			//Add addedgeometry to the activeEntities and keep a hashset of persistentIDs that should be excluded
			HashSet<int> excludedPersistentIDs = new HashSet<int>(m_currentPlanLayer.RemovedGeometry);
			//Keeps IDs that were removed in the most recent plan
			HashSet<int> removedDisplayedIDs = a_showRemovedInLatestPlan ? new HashSet<int>(m_currentPlanLayer.RemovedGeometry) : new HashSet<int>();
			HashSet<int> preModifiedPersisIDs = new HashSet<int>();
			m_activeEntities = new HashSet<T>();
			PreModifiedEntities = new HashSet<T>();
			PreExistingPersisIDs = new HashSet<int>();

			//Add current plan layer's entities if they are influencing or part of the plan we're viewing
			if (a_showCurrentIfNotInfluencing || m_currentPlanLayer.Plan.InInfluencingState)
			{
				for (int i = 0; i < m_currentPlanLayer.GetNewGeometryCount(); ++i)
				{
					Entity entity = m_currentPlanLayer.GetNewGeometryByIndex(i);
					if (IsEntityTypeVisible(entity.EntityTypes))
					{
						m_activeEntities.Add((T)entity);
					}
					int persisID = entity.GetSubEntity(0).GetPersistentID();
					excludedPersistentIDs.Add(persisID);
					preModifiedPersisIDs.Add(persisID);
				}
			}

			//Add previous plan layer entities and excluded entities
			for (int i = a_index - 1; i > a_baseIndex; i--)
			{
				if (!m_planLayers[i].Plan.InInfluencingState) //Plans in DESIGN and DELETES states are ignored
					continue;
				for (int entityIndex = 0; entityIndex < m_planLayers[i].GetNewGeometryCount(); ++entityIndex)
				{
					Entity entity = m_planLayers[i].GetNewGeometryByIndex(entityIndex);
					int id = entity.GetSubEntity(0).GetPersistentID();
					PreExistingPersisIDs.Add(id);
					if (removedDisplayedIDs.Contains(id)) //Makes geometry active that was removed in the most recent plan
					{
						T typedEntity = (T)entity;
						if (IsEntityTypeVisible(entity.EntityTypes))
						{
							m_activeEntities.Add(typedEntity);
						}
						PreModifiedEntities.Add(typedEntity);
						removedDisplayedIDs.Remove(id);
					}
					else
					{
						if (preModifiedPersisIDs.Contains(id)) //Finds the last previous instances of newgeometry
						{
							PreModifiedEntities.Add(entity as T);
							preModifiedPersisIDs.Remove(id);
						}
						if (!excludedPersistentIDs.Contains(id))//Makes geometry active that was not removed or replaced yet
						{
							if (IsEntityTypeVisible(entity.EntityTypes))
							{
								m_activeEntities.Add(entity as T);
							}
							excludedPersistentIDs.Add(id);
						}
					}
				}
				excludedPersistentIDs.UnionWith(m_planLayers[i].RemovedGeometry);
			}

			//Add geom from the base layer that has not been removed or altered
			foreach (T entity in a_baseEntities)
			{
				int id = entity.GetSubEntity(0).GetPersistentID();
				PreExistingPersisIDs.Add(id);
				if (preModifiedPersisIDs.Contains(id)) //Finds the previous instances of newgeometry
					PreModifiedEntities.Add(entity);

				bool isRemovedEntity = removedDisplayedIDs.Contains(id);
				if (!isRemovedEntity && excludedPersistentIDs.Contains(id))
					continue;
				if (IsEntityTypeVisible(entity.EntityTypes))
				{
					m_activeEntities.Add(entity);
				}
				if (isRemovedEntity)
				{
					PreModifiedEntities.Add(entity);
				}
			}
		}

		public override void SetEntitiesActiveUpToCurrentTime()
		{
			if (m_currentPlanLayer != null)
				m_currentPlanLayer.SetEnabled(false);
			m_currentPlanLayer = null;

			//active entities copies Entities (but not the reference)
			m_activeEntities = new HashSet<T>();
			for (int i = 0; i < Entities.Count; ++i)
			{
				Entity ent = Entities[i];
				if (IsEntityTypeVisible(ent.EntityTypes))
				{
					m_activeEntities.Add((T)ent);
				}
			}

			PreExistingPersisIDs = new HashSet<int>();
			PreModifiedEntities = new HashSet<T>();
		}

		void SetEntitiesActiveUpToInitialTime()
		{
			if (m_currentPlanLayer != null)
				m_currentPlanLayer.SetEnabled(false);
			m_currentPlanLayer = null;

			//active entities copies initialEntities (but not the reference)
			m_activeEntities = new HashSet<T>();
			for (int i = 0; i < InitialEntities.Count; ++i)
			{
				Entity ent = InitialEntities[i];
				m_activeEntities.Add((T)ent);
			}

			PreExistingPersisIDs = new HashSet<int>();
			PreModifiedEntities = new HashSet<T>();
		}

		public override bool IsIDInActiveGeometry(int a_id)
		{
			if (m_activeEntities == null)
				return false;

			foreach (T entity in m_activeEntities)
			{
				for (int i = 0; i < entity.GetSubEntityCount(); i++)
					if (entity.GetSubEntity(i).GetDatabaseID() == a_id)
						return true;
			}
			return false;
		}

		/// <summary>
		/// Did an object with the given persistentID exist before the current plan?
		/// </summary>
		public override bool IsPersisIDCurrentlyNew(int a_persisID)
		{
			return !PreExistingPersisIDs.Contains(a_persisID);
		}

		public void AddPreModifiedEntity(Entity a_entity)
		{
			PreModifiedEntities.Add(a_entity as T);
		}

		private void RemovePreModifiedEntity(Entity a_entity)
		{
			PreModifiedEntities.Remove(a_entity as T);
		}

		public override bool IsDatabaseIDPreModified(int a_dataBaseID)
		{
			foreach (T entity in PreModifiedEntities)
				if (entity.GetSubEntity(0).GetDatabaseID() == a_dataBaseID)
					return true;
			return false;
		}

		/// <summary>
		/// Goes back from the current planlayer and activates the first Entity with the given persistent ID encountered.
		/// </summary>
		public override void ActivateLastEntityWith(int a_persistentID)
		{
			foreach (T entity in PreModifiedEntities)
				for (int j = 0; j < entity.GetSubEntityCount(); j++)
				{
					SubEntity subEnt = entity.GetSubEntity(j);
					if (subEnt.GetPersistentID() != a_persistentID)
						continue;
					m_activeEntities.Add(entity);
					entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
					if (subEnt.IsNotAffectedByPlan())
						subEnt.RestoreDependencies();
					return;
				}
		}

		/// <summary>
		/// Removes the entity with the given persistent ID from active geometry and redraws it.
		/// </summary>
		public override void DeactivateCurrentEntityWith(int a_persistentID)
		{
			//for (int i = 0; i < activeEntities.Count; i++)
			foreach (T entity in m_activeEntities)
			{
				//T entity = activeEntities[i];
				for (int j = 0; j < entity.GetSubEntityCount(); j++)
				{
					SubEntity subEnt = entity.GetSubEntity(j);
					if (subEnt.GetPersistentID() != a_persistentID)
						continue;
					m_activeEntities.Remove(entity);
					if (subEnt.IsNotAffectedByPlan())
						subEnt.RemoveDependencies();
					entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
					return;
				}
			}
		}

		/// <summary>
		/// Merges all planlayers up to the given time with the base layer (clientside).
		/// Sets lastImplementedPlanIndex to reflect this update.
		/// Returns a dictionary with the investment cost per country if we are an energylayer
		/// </summary>
		public override void AdvanceTimeTo(int a_time)
		{
			if (m_planLayers.Count <= m_lastImplementedPlanIndex + 1 || m_planLayers[m_lastImplementedPlanIndex + 1].Plan.StartTime > a_time)
				return;

			int newPlanIndex = m_lastImplementedPlanIndex;

			//Find index of the most recent plan at the given planTime
			for (int i = m_planLayers.Count - 1; i > m_lastImplementedPlanIndex; i--)
				if (m_planLayers[i].Plan.StartTime <= a_time)
				{
					newPlanIndex = i;
					break;
				}
			MergePlanWithBaseUpToIndex(newPlanIndex);
		}

		/// <summary>
		/// Merges plans up to the given index with the base layer (client side).
		/// Ends with lastImplementedPlanIndex equal to the given index
		/// </summary>
		protected override void MergePlanWithBaseUpToIndex(int a_newPlanIndex)
		{
			if (a_newPlanIndex <= m_lastImplementedPlanIndex)
				return;

			List<T> newEntities = new List<T>();
			List<T> removedEntities = new List<T>();

			//Go through plans backwards from lastPlanIndex to get the state at the given time
			HashSet<int> excludedPersistentIDs = new HashSet<int>();
			for (int i = a_newPlanIndex; i > m_lastImplementedPlanIndex; i--)
			{
				for (int entityIndex = 0; entityIndex < m_planLayers[i].GetNewGeometryCount(); ++entityIndex)
				{
					Entity entity = m_planLayers[i].GetNewGeometryByIndex(entityIndex);

					if (excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
						continue;
					newEntities.Add(entity as T);
					excludedPersistentIDs.Add(entity.GetSubEntity(0).GetPersistentID());
				}
				excludedPersistentIDs.UnionWith(m_planLayers[i].RemovedGeometry);
			}

			//Find entities to be removed
			foreach (T entity in Entities)
				if (excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
					removedEntities.Add(entity);

			//Remove and add the respective entities
			foreach (T entity in removedEntities)
			{
				Entities.Remove(entity);
				entity.GetSubEntity(0).ForceGameObjectVisibility(false);//Won't be redrawn naturally because it isnt in Entities anymore
			}
			foreach (T entity in newEntities)
				Entities.Add(entity);

			m_lastImplementedPlanIndex = a_newPlanIndex;
			if (!LayerManager.Instance.LayerIsVisible(this) || PlanManager.Instance.m_planViewing != null ||
				PlanManager.Instance.m_timeViewing >= 0)
				return;
			SetEntitiesActiveUpToCurrentTime();
			RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default, true);
		}

		public override LayerState GetLayerStateAtTime(int a_month, Plan a_treatAsInfluencingState = null)
		{
			int planIndex = -1;
			for (int i = 0; i < m_planLayers.Count; i++)
			{
				if (!m_planLayers[i].Plan.InInfluencingState && m_planLayers[i].Plan != a_treatAsInfluencingState)
					continue;
				if (m_planLayers[i].Plan.StartTime <= a_month)
				{
					planIndex = i;
				}
				else
				{
					break;
				}
			}
			return GetLayerStateAtIndex(planIndex);
		}

		public override LayerState GetLayerStateAtPlan(Plan a_plan)
		{
			int planIndex = -1;
			if (a_plan == null)
				return GetLayerStateAtIndex(planIndex);
			for (int i = 0; i < m_planLayers.Count; i++)
			{
				if (m_planLayers[i].Plan == a_plan)
				{
					planIndex = i;
					break;
				}
				if (m_planLayers[i].Plan.StartTime > a_plan.StartTime || (m_planLayers[i].Plan.StartTime == a_plan.StartTime && a_plan.ID < m_planLayers[i].Plan.ID))
				{
					//Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
					planIndex = i - 1;
					break;
				}
				planIndex = i;
			}

			return GetLayerStateAtIndex(planIndex);
		}

		public override LayerState GetLayerStateAtIndex(int a_planIndex)
		{
			//See SetEntitiesActiveUpTo for a commented (more elaborate) version of this function
			List<Entity> geometry = new List<Entity>();

			//If planindex = -1. only return base geometry
			if (a_planIndex == -1)
			{
				foreach (T entity in InitialEntities)
					geometry.Add(entity);
			}
			//If planindex is the last implemented one, return the current entities
			else if (a_planIndex == m_lastImplementedPlanIndex)
			{
				foreach (T entity in Entities)
					geometry.Add(entity);
			}
			else
			{
				HashSet<int> excludedPersistentIDs = new HashSet<int>(m_planLayers[a_planIndex].RemovedGeometry);
				bool addCurrent = false;

				for (int entityIndex = 0; entityIndex < m_planLayers[a_planIndex].GetNewGeometryCount(); ++entityIndex)
				{
					Entity entity = m_planLayers[a_planIndex].GetNewGeometryByIndex(entityIndex);
					geometry.Add((T)entity);
					excludedPersistentIDs.Add(entity.GetSubEntity(0).GetPersistentID());
				}

				//Add previous plan layer entities and excluded entities
				for (int i = a_planIndex - 1; i >= 0; i--)
				{
					//If we reach the last implemented plan, stop and add the current state instead of the initial one
					if (i == m_lastImplementedPlanIndex)
					{
						addCurrent = true;
						break;
					}
					if (!m_planLayers[i].Plan.InInfluencingState) //Plans in DESIGN and DELETES states are ignored
						continue;
					for (int entityIndex = 0; entityIndex < m_planLayers[i].GetNewGeometryCount(); ++entityIndex)
					{
						Entity entity = m_planLayers[i].GetNewGeometryByIndex(entityIndex);
						int id = entity.GetSubEntity(0).GetPersistentID();
						if (!excludedPersistentIDs.Contains(id))//Makes geometry active that was not removed or replaced yet
						{
							geometry.Add(entity as T);
							excludedPersistentIDs.Add(id);
						}
					}
					excludedPersistentIDs.UnionWith(m_planLayers[i].RemovedGeometry);
				}

				if (addCurrent)
				{
					//Add geom from the current layer that has not been removed or altered
					foreach (T entity in Entities)
						if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
							geometry.Add(entity);
				}
				else
				{
					//Add geom from the initial layer that has not been removed or altered
					foreach (T entity in InitialEntities)
						if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
							geometry.Add(entity);
				}
			}

			return new LayerState(geometry, a_planIndex, this);
		}

		/// <summary>
		/// Should be called on socket layer.
		/// Returns a list of all updated/new grids for the given plan.
		/// Grids will have the same name, persisID and distribution if they existed in the given previousEnergyPlan.
		/// Grids that will be there after the plan are removed from removedGrids.
		/// </summary>
		public override List<EnergyGrid> DetermineChangedGridsInPlan(Plan a_plan, List<EnergyGrid> a_gridsInPlanPreviously, List<EnergyGrid> a_gridsBeforePlan, HashSet<int> a_removedGrids)
		{
			if (m_editingType != EditingType.Socket)
			{
				return null;
			}

			if ((m_currentPlanLayer == null || m_currentPlanLayer.Plan.ID != a_plan.ID) && m_planLayers.Count > 0)
			{
				SetEntitiesActiveUpTo(a_plan);
			}
			if (m_currentPlanLayer == null)
			{
				return new List<EnergyGrid>();
			}

			//HashSet<int> ignoredGridPersistentIds;
			List<EnergyGrid> result = new List<EnergyGrid>();
			HashSet<SubEntity> visitedSockets = new HashSet<SubEntity>();

			foreach (T entity in m_activeEntities)
			{
				//If not visited, create new grid
				if (visitedSockets.Contains(entity.GetSubEntity(0)) ||
					m_currentPlanLayer.RemovedGeometry.Contains(entity.GetSubEntity(0).GetPersistentID()))
					continue;
				EnergyGrid newGrid = new EnergyGrid((EnergyPointSubEntity)entity.GetSubEntity(0), a_plan);
				foreach (EnergyPointSubEntity socket in newGrid.m_sockets)
					visitedSockets.Add(socket);

				//Determine if the new grid is the same as one of the old ones, if so: take over distribution and persistentID
				bool identicalGridFound = false;
				bool initialDistributionSet = false;

				//======================= PREVIOUS VERSION OF THIS PLAN ========================

				//Look for grids that were already in this plan that are identical to the new one.
				//If one is found add it to the results. We don't care about partial identicality.

				if (a_gridsInPlanPreviously != null)
				{
					foreach (EnergyGrid oldGrid in a_gridsInPlanPreviously)
					{
						if (!oldGrid.MatchesColor(m_greenEnergy ? EnergyGrid.GridColor.Green : EnergyGrid.GridColor.Grey))
							continue;
						if (!newGrid.SocketWiseIdentical(oldGrid))
							continue;
						//Sockets are identical, are the sources?
						identicalGridFound = newGrid.SourceWiseIdentical(oldGrid);

						if (identicalGridFound)
						{
							//If sources also identical, take over values from previous version of this plan
							initialDistributionSet = true;
							newGrid.m_name = oldGrid.m_name;
							newGrid.m_distributionOnly = oldGrid.m_distributionOnly;
							if (oldGrid.DatabaseIDSet())
								newGrid.SetDatabaseID(oldGrid.GetDatabaseID());
							newGrid.m_persistentID = oldGrid.m_persistentID;
							if (a_removedGrids.Contains(oldGrid.m_persistentID))
								a_removedGrids.Remove(oldGrid.m_persistentID);
							newGrid.CalculateInitialDistribution(oldGrid);
							result.Add(newGrid);
						}
						break;
					}
				}

				//============================== PREVIOUS PLANS ================================
				//Even if we already found an identical grid, go through here to find grids that we removed
				if (a_gridsBeforePlan != null)
				{
					foreach (EnergyGrid oldGrid in a_gridsBeforePlan)
					{
						if (!oldGrid.MatchesColor(m_greenEnergy ? EnergyGrid.GridColor.Green : EnergyGrid.GridColor.Grey))
							continue;

						if (!newGrid.SocketWiseIdentical(oldGrid))
							continue;
						//If identicalgridfound we already found the grid and are only looking for partials
						//Since this matches exactly, there will be no partials
						if (identicalGridFound)
							break;

						//If sources are also identical, it was unchanged, don't add it to results
						identicalGridFound = newGrid.SourceWiseIdentical(oldGrid);

						if (identicalGridFound)
						{
							//Oldgrid is still present, remove its ID from removed IDs
							if (a_removedGrids.Contains(oldGrid.m_persistentID))
								a_removedGrids.Remove(oldGrid.m_persistentID);
						}
						else	//Grids are not sourcewise identical, so take over values from previous one
						{
							initialDistributionSet = true;
							newGrid.m_name = oldGrid.m_name;
							newGrid.m_persistentID = oldGrid.m_persistentID;
							newGrid.CalculateInitialDistribution(oldGrid);
						}
						break; //We found a socketwise identical grid, no use in continueing to look for matches
					}
				}
				//============================== SECTION END ================================

				//No exactly identical grid was found, so this grid is new and can be added to results. this occurs in 2 cases:
				// 1. No matching grids were found.
				// 2. A socketwise match was found, but sources differed. We add a new grid with the same persistentID. (initialDistributionSet is true)
				if (!identicalGridFound)
				{
					if (!initialDistributionSet)
						newGrid.CalculateInitialDistribution();
					else if (a_removedGrids.Contains(newGrid.m_persistentID))
						a_removedGrids.Remove(newGrid.m_persistentID);
					result.Add(newGrid);
				}
				else if (a_removedGrids.Contains(newGrid.m_persistentID))
					a_removedGrids.Remove(newGrid.m_persistentID);
			}
			return result;
		}

		/// <summary>
		/// Adds the cables connections to the points that are connected to them.
		/// </summary>
		public override void ActivateCableLayerConnections()
		{
			if (!IsEnergyLineLayer())
				return;

			foreach (T entity in m_activeEntities)
				for (int i = 0; i < entity.GetSubEntityCount(); i++)
				{
					SubEntity subEntity = entity.GetSubEntity(i);
					if (!subEntity.IsPlannedForRemoval())
						subEntity.ActivateConnections();
				}
		}

		/// <summary>
		/// Should be called on cables layers when you start editing a plan with an energy error.
		/// Assumes active entities have been updated for all energy layers.
		/// </summary>
		public override List<EnergyLineStringSubEntity> RemoveInvalidCables()
		{
			if (!IsEnergyLineLayer() || m_currentPlanLayer == null)
				return null;

			List<EnergyLineStringSubEntity> cablesToRemove = new List<EnergyLineStringSubEntity>();

			foreach (Entity entity in m_currentPlanLayer.GetNewGeometry())
			{
				//Check the 2 connections for valid points
				EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)entity.GetSubEntity(0);
				foreach (Connection conn in cable.Connections)
				{
					//If point is not in active entities, remove the cable.
					if (!conn.point.m_entity.Layer.IsIDInActiveGeometry(conn.point.GetDatabaseID()))
					{
						cablesToRemove.Add(cable);
						break;
					}
				}
			}

			foreach (EnergyLineStringSubEntity cable in cablesToRemove)
			{
				//Remove from new geometry
				m_currentPlanLayer.RemoveNewGeometry(cable.m_entity);
				//Remove from active entities
				m_activeEntities.Remove((T)cable.m_entity);
				//Remove GO
				cable.RemoveGameObject();
				//Actual removal from the server is done with the batch submit
			}

			if(cablesToRemove.Count > 0)
				DialogBoxManager.instance.NotificationWindow("Removed invalid cables", "The plan contained cables that were no longer connected to points. They have been removed.", null);
			return cablesToRemove;
		}

		public override void RestoreInvalidCables(List<EnergyLineStringSubEntity> a_cables)
		{
			foreach (EnergyLineStringSubEntity cable in a_cables)
			{
				if (cable.m_entity.Layer != this)
					continue;
				cable.m_entity.PlanLayer.AddNewGeometry(cable.m_entity);
				cable.DrawGameObject(LayerGameObject.transform);
				m_activeEntities.Add((T)cable.m_entity);
			}
		}

		/// <summary>
		/// Removes the connections from non-cables, these are then re-added by the currently active cables.
		/// </summary>
		public override void ResetEnergyConnections()
		{
			if (IsEnergyLineLayer())
				return;

			foreach (T entity in m_activeEntities)
				for (int i = 0; i < entity.GetSubEntityCount(); i++)
					entity.GetSubEntity(i).ClearConnections();
		}

		/// <summary>
		/// Sets CurrentGrid to null for all geometry (not only active)
		/// </summary>
		public override void ResetCurrentGrids()
		{
			if (!IsEnergyLayer())
				return;

			foreach (T entity in Entities)
				((IEnergyDataHolder)entity.GetSubEntity(0)).CurrentGrid = null;

			for(int i = m_lastImplementedPlanIndex + 1; i < m_planLayers.Count; i++)
				foreach(Entity entity in m_planLayers[i].GetNewGeometry())
					((IEnergyDataHolder)entity.GetSubEntity(0)).CurrentGrid = null;
		}

		public Dictionary<int, List<DirectionalConnection>> GetCableNetworkAtTime(int a_month)
		{
			if (!IsEnergyLineLayer() || m_planLayers.Count == 0)
				return null;

			for (int i = m_planLayers.Count - 1; i >= 0; i--)
			{
				//Find last plan at time
				if (m_planLayers[i].Plan.StartTime >= a_month)
				{
					return GetCableNetworkAtPlanIndex(i);
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the cable network at the time of a given plan. Doesn't alter the state of the layer or plan itself.
		/// </summary>
		/// <returns> Per point db ID, a list of connected ables and points. </returns>
		public Dictionary<int, List<DirectionalConnection>> GetCableNetworkForPlan(Plan a_plan)
		{
			if (!IsEnergyLineLayer() || a_plan == null || m_planLayers.Count == 0)
				return null;

			//Find the index of the plan
			int index = -1;
			for (int i = 0; i < m_planLayers.Count; i++)
			{
				if (m_planLayers[i].Plan.ID == a_plan.ID)
				{
					index = i;
					break;
				}
				if (m_planLayers[i].Plan.StartTime > a_plan.StartTime || (m_planLayers[i].Plan.StartTime == a_plan.StartTime && a_plan.ID < m_planLayers[i].Plan.ID))
				{
					//Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
					index = i - 1;
					break;
				}
			}

			if (index < 0) //No base plan for cables exists
				return null;

			return GetCableNetworkAtPlanIndex(index);
		}

		private Dictionary<int, List<DirectionalConnection>> GetCableNetworkAtPlanIndex(int a_index)
		{
			Dictionary<int, List<DirectionalConnection>> network = new Dictionary<int, List<DirectionalConnection>>();
			HashSet<int> excludedPersistentIDs = new HashSet<int>(m_planLayers[a_index].RemovedGeometry);

			for (int entityIndex = 0; entityIndex < m_planLayers[a_index].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = m_planLayers[a_index].GetNewGeometryByIndex(entityIndex);

				EnergyLineStringSubEntity cable = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
				if (cable.Connections == null || cable.Connections.Count < 2)
				{
					Debug.LogError("Cable without connections in current plan layer. Ignored for grids. ID: " + cable.GetDatabaseID().ToString());
					continue;
				}
				EnergyPointSubEntity p0 = cable.Connections[0].point;
				EnergyPointSubEntity p1 = cable.Connections[1].point;

				//Add connections from p0 to p1 and vive versa to network
				if (network.ContainsKey(p0.GetDatabaseID()))
					network[p0.GetDatabaseID()].Add(new DirectionalConnection(cable, p1));
				else
					network.Add(p0.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p1) });
				if (network.ContainsKey(p1.GetDatabaseID()))
					network[p1.GetDatabaseID()].Add(new DirectionalConnection(cable, p0));
				else
					network.Add(p1.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p0 )});

				excludedPersistentIDs.Add(cable.GetPersistentID());
			}

			//Add previous plan layer entities and excluded entities
			for (int i = a_index - 1; i > m_lastImplementedPlanIndex; i--)
			{
				if (!m_planLayers[i].Plan.InInfluencingState)
					continue;

				for (int entityIndex = 0; entityIndex < m_planLayers[i].GetNewGeometryCount(); ++entityIndex)
				{
					Entity entity = m_planLayers[i].GetNewGeometryByIndex(entityIndex);

					int id = entity.GetSubEntity(0).GetPersistentID();
					if (excludedPersistentIDs.Contains(id)) //Makes geometry active that was not removed or replaced yet
						continue;
					EnergyLineStringSubEntity cable = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
					if (cable.Connections == null || cable.Connections.Count < 2)
					{
						Debug.LogError("Cable without connections in previous plan layer. Ignored for grids. ID: " + cable.GetDatabaseID().ToString());
						continue;
					}
					EnergyPointSubEntity p0 = cable.Connections[0].point;
					EnergyPointSubEntity p1 = cable.Connections[1].point;

					//Add connections from p0 to p1 and vice versa to network
					if (network.ContainsKey(p0.GetDatabaseID()))
						network[p0.GetDatabaseID()].Add(new DirectionalConnection(cable, p1));
					else
						network.Add(p0.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p1 )});
					if (network.ContainsKey(p1.GetDatabaseID()))
						network[p1.GetDatabaseID()].Add(new DirectionalConnection(cable, p0));
					else
						network.Add(p1.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p0 )});

					excludedPersistentIDs.Add(id);
				}
				excludedPersistentIDs.UnionWith(m_planLayers[i].RemovedGeometry);
			}

			//Add geom from the base layer that has not been removed or altered
			foreach (T entity in Entities)
			{
				if (excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
					continue;
				EnergyLineStringSubEntity cable = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
				if (cable.Connections == null || cable.Connections.Count < 2)
				{
					Debug.LogError("Cable without connections in base data. Ignored for grids. ID: " + cable.GetDatabaseID().ToString());
					continue;
				}
				EnergyPointSubEntity p0 = cable.Connections[0].point;
				EnergyPointSubEntity p1 = cable.Connections[1].point;

				//Add connections from p0 to p1 and vice versa to network
				if (network.ContainsKey(p0.GetDatabaseID()))
					network[p0.GetDatabaseID()].Add(new DirectionalConnection(cable, p1));
				else
					network.Add(p0.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p1 )});
				if (network.ContainsKey(p1.GetDatabaseID()))
					network[p1.GetDatabaseID()].Add(new DirectionalConnection(cable, p0));
				else
					network.Add(p1.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p0 )});
			}

			return network;
		}

		/// <summary>
		/// Gets the cable network at the time of a given plan. Doesn't alter the state of the layer or plan itself.
		/// </summary>
		/// <returns> Per point db ID, a list of connected cables. </returns>
		public Dictionary<int, List<EnergyLineStringSubEntity>> GetNodeConnectionsForPlan(Plan a_plan, Dictionary<int, List<EnergyLineStringSubEntity>> a_networkToMerge = null)
		{
			if (!IsEnergyLineLayer() || a_plan == null || m_planLayers.Count == 0)
				return a_networkToMerge;

			//Find the index of the plan
			int index = -1;
			for (int i = 0; i < m_planLayers.Count; i++)
			{
				if (m_planLayers[i].Plan == a_plan)
				{
					index = i;
					break;
				}
				if (m_planLayers[i].Plan.StartTime > a_plan.StartTime || (m_planLayers[i].Plan.StartTime == a_plan.StartTime && a_plan.ID < m_planLayers[i].Plan.ID))
				{
					//Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
					index = i - 1;
					break;
				}
			}

			if (index < 0) //No base plan for cables exists
				return a_networkToMerge;

			Dictionary<int, List<EnergyLineStringSubEntity>> network = a_networkToMerge == null ? new Dictionary<int, List<EnergyLineStringSubEntity>>() : a_networkToMerge;
			HashSet<int> excludedPersistentIDs = new HashSet<int>(m_planLayers[index].RemovedGeometry);

			for (int entityIndex = 0; entityIndex < m_planLayers[index].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = m_planLayers[index].GetNewGeometryByIndex(entityIndex);

				EnergyLineStringSubEntity subEnt = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
				if (subEnt.Connections.Count < 2)
				{
					Debug.LogError("Cable with less than 2 connections encountered when removing energy layer. Cable ID: " + subEnt.GetDatabaseID());
					continue;
				}
				EnergyPointSubEntity p0 = subEnt.Connections[0].point;
				EnergyPointSubEntity p1 = subEnt.Connections[1].point;

				//Add connections from p0 to p1 and vice versa to network
				if (network.ContainsKey(p0.GetDatabaseID()))
					network[p0.GetDatabaseID()].Add(subEnt);
				else
					network.Add(p0.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });
				if (network.ContainsKey(p1.GetDatabaseID()))
					network[p1.GetDatabaseID()].Add(subEnt);
				else
					network.Add(p1.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });

				excludedPersistentIDs.Add(subEnt.GetPersistentID());
			}

			//Add previous plan layer entities and excluded entities
			for (int i = index - 1; i > m_lastImplementedPlanIndex; i--)
			{
				for (int entityIndex = 0; entityIndex < m_planLayers[i].GetNewGeometryCount(); ++entityIndex)
				{
					Entity entity = m_planLayers[i].GetNewGeometryByIndex(entityIndex);

					int id = entity.GetSubEntity(0).GetPersistentID();
					if (excludedPersistentIDs.Contains(id)) //Makes geometry active that was not removed or replaced yet
						continue;
					EnergyLineStringSubEntity subEnt = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
					if (subEnt.Connections.Count < 2)
					{
						Debug.LogError("Cable with less than 2 connections encountered when removing energy layer. Cable ID: " + subEnt.GetDatabaseID());
						continue;
					}
					EnergyPointSubEntity p0 = subEnt.Connections[0].point;
					EnergyPointSubEntity p1 = subEnt.Connections[1].point;

					//Add connections from p0 to p1 and vice versa to network
					if (network.ContainsKey(p0.GetDatabaseID()))
						network[p0.GetDatabaseID()].Add(subEnt);
					else
						network.Add(p0.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });
					if (network.ContainsKey(p1.GetDatabaseID()))
						network[p1.GetDatabaseID()].Add(subEnt);
					else
						network.Add(p1.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });

					excludedPersistentIDs.Add(id);
				}
				excludedPersistentIDs.UnionWith(m_planLayers[i].RemovedGeometry);
			}

			//Add geom from the base layer that has not been removed or altered
			foreach (T entity in Entities)
				if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
				{
					EnergyLineStringSubEntity subEnt = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
					if (subEnt.Connections.Count < 2)
						continue;

					EnergyPointSubEntity p0 = subEnt.Connections[0].point;
					EnergyPointSubEntity p1 = subEnt.Connections[1].point;

					//Add connections from p0 to p1 and vice versa to network
					if (network.ContainsKey(p0.GetDatabaseID()))
						network[p0.GetDatabaseID()].Add(subEnt);
					else
						network.Add(p0.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });
					if (network.ContainsKey(p1.GetDatabaseID()))
						network[p1.GetDatabaseID()].Add(subEnt);
					else
						network.Add(p1.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });
				}

			return network;
		}

		/// <summary>
		/// Returns the latest instances of geometry with the given persistent IDs before the given planlayer.
		/// </summary>
		public override List<SubEntity> GetFirstInstancesOfPersisIDBeforePlan(PlanLayer a_planLayer, HashSet<int> a_persistentIDs)
		{
			//Required for alternative method
			List<SubEntity> result = new List<SubEntity>();

			if (m_currentPlanLayer != null && m_currentPlanLayer.ID == a_planLayer.ID)
			{
				foreach (T t in PreModifiedEntities)
				{
					if (a_persistentIDs.Contains(t.PersistentID))
					{
						result.Add(t.GetSubEntity(0));
					}
				}
			}
			else
			{
				//This is an alternative method for if the assumption that the planlayer is active doesn't hold in the future.
				int targetsLeft = a_persistentIDs.Count;
				HashSet<int> targetIDs = new HashSet<int>(a_persistentIDs);


				//Find the index of the layer
				int index = -1;
				for (int i = 0; i < m_planLayers.Count; i++)
				{
					if (m_planLayers[i].Plan.ID != a_planLayer.Plan.ID)
						continue;
					index = i;
					break;
				}

				for (int i = index - 1; i >= 0 && targetsLeft > 0; i--)
				{
					foreach (Entity entity in m_planLayers[i].GetNewGeometry())
					{
						SubEntity subEnt = entity.GetSubEntity(0);
						if (targetIDs.Contains(subEnt.GetPersistentID()))
						{
							result.Add(subEnt);
							targetsLeft--;
							targetIDs.Remove(subEnt.GetPersistentID());
						}
					}
				}

				if (targetsLeft <= 0)
					return result;
				{
					foreach (Entity entity in Entities)
					{
						SubEntity subEnt = entity.GetSubEntity(0);
						if (targetIDs.Contains(subEnt.GetPersistentID()))
						{
							result.Add(subEnt);
							targetsLeft--;
							if (targetsLeft == 0)
								break;
							targetIDs.Remove(subEnt.GetPersistentID());
						}
					}
				}
			}
			return result;
		}
	}

	class SourceSummary
	{
		private int m_country;
		private EntityType m_type;
		private long m_capacity;
		public SourceSummary(int a_country, EntityType a_type, long a_capacity)
		{
			m_country = a_country;
			m_type = a_type;
			m_capacity = a_capacity;
		}

		public bool Matches(int a_country, EntityType a_type, long a_capacity)
		{
			return m_country == a_country && m_type == a_type && m_capacity == a_capacity;
		}
	}
}
