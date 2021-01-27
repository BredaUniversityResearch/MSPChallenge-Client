using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using UnityEngine.Networking;

public abstract class Layer<T> : AbstractLayer where T : Entity
{
	public List<T> initialEntities				{ get; private set; }	//Entities that existed on the base original layer
	public List<T> Entities						{ get; private set; }	//Entities that exist at the games current time
	public HashSet<T> activeEntities;									//Entities at the time of currently active plan
	public HashSet<T> preModifiedEntities		{ get; private set; }	//Lastest geometry with the same persisID as new geometry in the current active planlayer
	public HashSet<int> preExistingPersisIDs	{ get; private set; }	//Persistent IDs that existed before the current plan

	protected Layer(LayerMeta layerMeta)
		: base(layerMeta)
	{
		Entities = new List<T>();
		initialEntities = new List<T>();
		activeEntities = new HashSet<T>();

		Initialise();
	}

	public override void Initialise()
	{
		Entities.Clear();
		initialEntities.Clear();
		activeEntities.Clear();
	}

	public override bool IsEnergyPointLayer()
	{
		return editingType == EditingType.Transformer || editingType == EditingType.Socket || editingType == EditingType.SourcePoint || editingType == EditingType.SourcePolygonPoint;
	}

	public override bool IsEnergyLineLayer()
	{
		return editingType == EditingType.Cable;
	}

	public override bool IsEnergyPolyLayer()
	{
		return editingType == EditingType.SourcePolygon;
	}

	public override bool IsEnergyLayer()
	{
		return editingType != EditingType.Normal;
	}

	public override void LoadLayerObjects(List<SubEntityObject> layerObjects)
	{
	}

	public override List<EntityType> GetEntityTypesByKeys(params int[] keys)
	{
		List<EntityType> types = new List<EntityType>();

		foreach (int key in keys)
		{
			if (EntityTypes.ContainsKey(key))
			{
				types.Add(EntityTypes[key]);
			}
		}
		return types;
	}

	public override EntityType GetEntityTypeByKey(int key)
	{
		if (EntityTypes.ContainsKey(key))
		{
			return EntityTypes[key];
		}

		return null;
	}

	public override EntityType GetEntityTypeByName(string name)
	{
		foreach (var kvp in EntityTypes)
		{
			if (kvp.Value.Name == name) { return kvp.Value; }
		}

		return null;
	}

	public override int GetEntityTypeKey(EntityType entityType)
	{
		foreach (var kvp in this.EntityTypes)
		{
			if (kvp.Value.Name == entityType.Name)
			{
				return kvp.Key;
			}
		}

		if (entityType == null) { Debug.LogError("Entity type error in " + FileName); }
		Debug.LogError("Failed to find key from entity type " + entityType.Name + " in " + FileName);

		return 0;
	}

	public override void RemoveSubEntity(SubEntity subEntity, bool uncreate = false)
	{
		T entity = (T)subEntity.Entity;
		subEntity.RemoveDependencies();
		int persisID = subEntity.GetPersistentID();

		//If it wasnt in the curren't plan, simply add to removed geom
		if (entity.PlanLayer == currentPlanLayer)
		{
			if (entity.GetSubEntityCount() != 1)
			{
				//If there are no other subentities left in the entity
				throw new Exception("Entity has an invalid subentity count. Count: " + entity.GetSubEntityCount() + " on entity with database ID " + entity.DatabaseID);
			}
			currentPlanLayer.RemoveNewGeometry(entity);
			activeEntities.Remove(entity);

			//If the entity wasn't created in this plan, active the last entity with that persisID and add it to removedgeom
			if (!subEntity.Entity.Layer.IsPersisIDCurrentlyNew(persisID))
			{
				if (!uncreate) //We are uncreating, only reactivate previous version, don't add to removed geom
				{
					AddPreModifiedEntity(subEntity.Entity);
					entity.PlanLayer.RemovedGeometry.Add(persisID);
				}
				ActivateLastEntityWith(persisID);
			}
		}
		else //Was on another layer and removed by this plan. It's removed and displayed with (-) icons.
		{
			currentPlanLayer.RemovedGeometry.Add(persisID);
			AddPreModifiedEntity(subEntity.Entity);
		}
		entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
	}

	public override void RestoreSubEntity(SubEntity subEntity, bool recreate = false)
	{
		T entity = (T)subEntity.Entity;
		//entity.RestoreSubEntity(subEntity);
		subEntity.RestoreDependencies();
		int persistentID = subEntity.GetPersistentID();

		if (entity.PlanLayer == currentPlanLayer)
		{
			//This entity used to be inactive, and is now made active
			if (entity.GetSubEntityCount() == 1)
			{
				currentPlanLayer.AddNewGeometry(entity);

				// If another object was being displayed because of the deletion, deactivate it and show this instead
				if (!subEntity.Entity.Layer.IsPersisIDCurrentlyNew(persistentID))
				{
					if (!recreate) //We are only recreating a newer version, the ID isn't in removedgeom
					{
						RemovePreModifiedEntity(subEntity.Entity);
						entity.PlanLayer.RemovedGeometry.Remove(persistentID);
					}
					DeactivateCurrentEntityWith(persistentID);
				}

				//Only add to activeentities after calling DeactivateCurrentEntityWith
				activeEntities.Add(entity);
			}
		}
		else //Was on another layer and removed by this plan
		{
			currentPlanLayer.RemovedGeometry.Remove(persistentID);
			RemovePreModifiedEntity(subEntity.Entity);
		}
		entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
	}

	public override List<EntityType> GetEntityTypesSortedByKey()
	{
		List<int> keys = new List<int>(EntityTypes.Keys);
		keys.Sort();

		List<EntityType> result = new List<EntityType>();
		foreach (int key in keys)
		{
			result.Add(EntityTypes[key]);
		}
		return result;
	}

	public override HashSet<Entity> GetEntitiesOfType(EntityType type)
	{
		HashSet<Entity> result = new HashSet<Entity>();

		int typeID = GetEntityTypeKey(type);
		foreach (T entity in Entities)
		{
			if (entity.GetEntityTypeKeys().Contains(typeID))
			{
				result.Add(entity);
			}
		}
		return result;
	}

	public override HashSet<Entity> GetActiveEntitiesOfType(EntityType type)
	{
		HashSet<Entity> result = new HashSet<Entity>();

		int typeID = GetEntityTypeKey(type);
		foreach (T entity in activeEntities)
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

		LayerGameObject.transform.position = new Vector3(0, 0, -Order);
		if (!(this is RasterLayer))
			DrawGameObjects(LayerGameObject.transform);
	}

	protected override void DrawGameObjects(Transform layerTransform)
	{
		foreach (T entity in Entities)
		{
			entity.DrawGameObjects(layerTransform);
		}
	}

	public override void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode, bool forceScaleUpdate = false)
	{
		//Doesn't redraw geometry removed from Entities

		foreach (T entity in initialEntities)
			entity.RedrawGameObjects(targetCamera, drawMode, forceScaleUpdate);

		foreach (PlanLayer p in planLayers)
		{
			for (int i = 0; i < p.GetNewGeometryCount(); ++i)
			{
				p.GetNewGeometryByIndex(i).RedrawGameObjects(targetCamera, drawMode, forceScaleUpdate);
			}
		}
	}

    public override bool LayerTextVisible
    {
        get { return layerTextVisible; }
        set
        {
            if (value != layerTextVisible)
            {
                layerTextVisible = value;
                if (value)
                {
                    foreach (T entity in Entities)
                    {
                        SubEntity sub = entity.GetSubEntity(0);
                        if (sub.TextMeshVisibleAtZoom)
                            sub.SetTextMeshActivity(true);
                    }

                    foreach (PlanLayer p in planLayers)
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

                    foreach (PlanLayer p in planLayers)                  
                        for (int i = 0; i < p.GetNewGeometryCount(); ++i)
                           p.GetNewGeometryByIndex(i).GetSubEntity(0).SetTextMeshActivity(false);
                }
            }
        }
    }

    public override void SubmitMetaData()
	{
		NetworkForm form = new NetworkForm();

		form.AddField("icon", "");
		form.AddField("category", Category);
		form.AddField("subcategory", SubCategory);
		form.AddField("type", MetaToJSON());
		form.AddField("depth", Depth.ToString());
		form.AddField("short", ShortName);
		form.AddField("id", ID);

		ServerCommunication.DoRequest(Server.PostLayerMeta(), form);
	}

	protected override string MetaToJSON()
	{
		Dictionary<int, EntityTypeValues> types = new Dictionary<int, EntityTypeValues>();

		foreach (var kvp in EntityTypes)
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

	public override List<Entity> GetEntitiesAt(Vector2 position)
	{
		List<SubEntity> subEntities = GetSubEntitiesAt(position);
		List<Entity> result = new List<Entity>();
		foreach (SubEntity subEntity in subEntities)
		{
			if (!result.Contains(subEntity.Entity))
			{
				result.Add(subEntity.Entity);
			}
		}
		return result;
	}

	public override SubEntity GetSubEntityByDatabaseID(int id)
	{
		for (int i = 0; i < GetEntityCount(); i++)
		{
			Entity entity = GetEntity(i);
			for (int j = 0; j < entity.GetSubEntityCount(); j++)
			{
				if (entity.GetSubEntity(j).GetDatabaseID() == id)
				{
					return entity.GetSubEntity(j);
				}
			}
		}

		return null;
	}

	public override SubEntity GetSubEntityByPersistentID(int persistentID)
	{
		for (int i = 0; i < GetEntityCount(); i++)
		{
			Entity entity = GetEntity(i);
			for (int j = 0; j < entity.GetSubEntityCount(); j++)
			{
				if (entity.GetSubEntity(j).GetPersistentID() == persistentID)
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

	public override Entity GetEntity(int index)
	{
		return Entities[index];
	}

	//public override int GetActiveEntityCount()
	//{
	//    return activeEntities.Count;
	//}

	//public override Entity GetActiveEntity(int index)
	//{
	//    return activeEntities[index];
	//}

	public override HashSet<SubEntity> GetActiveSubEntities()
	{
		HashSet<SubEntity> result = new HashSet<SubEntity>();
		foreach (T entity in activeEntities)
		{
			if (!result.Add(entity.GetSubEntity(0)))
			{
				Debug.Log("Adding Duplicate entity in hashset.");
			}
		}
		return result;
	}

	public override void UpdateScale(Camera targetCamera)
	{ }
	public override Entity CreateEntity(SubEntityObject obj)
	{ return null; }
	public override Entity GetEntityAt(Vector2 position)
	{ return null; }
	public override SubEntity GetSubEntityAt(Vector2 position)
	{ return null; }
	public override List<SubEntity> GetSubEntitiesAt(Vector2 position)
	{ return null; }
	public override LayerManager.GeoType GetGeoType()
	{ return LayerManager.GeoType.line; }

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
		return currentPlanLayer;
	}

	/// <summary>
	/// Adds the planlayer and makes sure the layers are sorted by implementation time (and then by the plan's db id)
	/// </summary>
	public override int AddPlanLayer(PlanLayer planLayer)
	{
		if (planLayer.Plan.State == Plan.PlanState.DELETED)
			Debug.LogError("Archived PlanLayer added to layer.");

		if (planLayers.Count == 0)
		{
			planLayers.Add(planLayer);
			return 0;
		}

		for (int i = 0; i < planLayers.Count; i++)
			if (planLayers[i].Plan.StartTime > planLayer.Plan.StartTime ||
				(planLayers[i].Plan.StartTime == planLayer.Plan.StartTime && planLayers[i].Plan.ID > planLayer.Plan.ID)) //If plan time is the same, sort by databaseID of the plan
			{
				if (i <= lastImplementedPlanIndex)
					Debug.LogError("PlanLayer added with an index lower than last implemented plan. Plans before the current time should be impossible.");
				planLayers.Insert(i, planLayer);
				return i;
			}

		//The planlayer should be the last element
		planLayers.Add(planLayer);
		return planLayers.Count - 1;
	}

	/// <summary>
	/// Should be called after a plan changes it's time.
	/// Moves the given plan layer to the correct time. Makes sure layers stay sorted.
	/// </summary>
	/// <returns>The given layer's new index. Can be use to get the PlanState for that layer.</returns>
	public override int UpdatePlanLayerTime(PlanLayer planLayer)
	{
		planLayers.Remove(planLayer);
		return AddPlanLayer(planLayer);
	}

	public override Entity AddObject(SubEntityObject obj)
	{
		T entity = (T)CreateEntity(obj);
		Entities.Add(entity);
		initialEntities.Add(entity);
		return entity;
	}

	public override void RemovePlanLayer(PlanLayer planLayer)
	{
		planLayers.Remove(planLayer);
	}

	/// <summary>
	/// Removes the planlayer from this layer. Removes the planlayer's entities from active entities
	/// </summary>
	public override void RemovePlanLayerAndEntities(PlanLayer planLayer)
	{
		//If the removed planLayer was the current planlayer, set the one before active
		//All the planlayers active geom is deleted anyway, so no need to setActiveTo
		if (planLayer == currentPlanLayer)
		{
			int index = 0;
			for (int i = 0; i < planLayers.Count; i++)
			{
				if (planLayers[i] == planLayer)
				{
					index = i;
					break;
				}
			}
			if (index > 0)
			{
				currentPlanLayer = planLayers[index - 1];
				currentPlanLayer.SetEnabled(true);
			}
			else
				currentPlanLayer = null;
		}
		planLayers.Remove(planLayer);
		for (int i = 0; i < planLayer.GetNewGeometryCount(); ++i)
		{
			Entity entity = planLayer.GetNewGeometryByIndex(i);
			activeEntities.Remove((T)entity);
		}
	}

	public override void SetEntitiesActiveUpToTime(int month)
	{
		for (int i = planLayers.Count-1; i >= 0; i--)
		{
			//Find last plan at time (or a StartingPlan)
			if (planLayers[i].Plan.StartTime < 0 || planLayers[i].Plan.StartTime <= month)
			{
				//Since we are viewing a time, dont show removed geom and dont show geometry in last plan if it's not influencing.
				SetEntitiesActiveUpTo(i, false, false);
				return;
			}
		}

		//If no planlayers are later than chosen month, set intialEntities Active.
		SetEntitiesActiveUpToInitialTime();
	}

	public override void SetEntitiesActiveUpTo(Plan plan)
	{
		if (plan != null)
		{
			int index = planLayers.Count - 1;
            bool showRemoved = false;
			for (int i = 0; i < planLayers.Count; i++)
			{
				if (planLayers[i].Plan == plan)
				{
					index = i;
                    showRemoved = true;
					break;
				}
				else if (planLayers[i].Plan.StartTime > plan.StartTime || (planLayers[i].Plan.StartTime == plan.StartTime && plan.ID < planLayers[i].Plan.ID))
				{
					//Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
					index = i - 1;
					break;
				}
			}
			SetEntitiesActiveUpTo(index, showRemoved, plan.IsLayerpartOfPlan(this));
		}
		else
			SetEntitiesActiveUpTo(-1);
	}

    /// <summary>
    /// Sets activeEntities to encompass all entities from the base and plan layers that will be active at the given time.
    /// </summary>
    public override void SetEntitiesActiveUpTo(int index, bool showRemovedInLatestPlan = true, bool showCurrentIfNotInfluencing = true)
	{
		if (index < 0)
		{
			SetEntitiesActiveUpToCurrentTime();
		}
		else if (index <= lastImplementedPlanIndex)
		{
			//Plan is in the past, go back to initial geom
			SetEntitiesActiveUpToAdv(index, initialEntities, -1, showRemovedInLatestPlan, showCurrentIfNotInfluencing);
		}
		else
		{
			//Plan is in the future, go back to current state
			SetEntitiesActiveUpToAdv(index, Entities, lastImplementedPlanIndex, showRemovedInLatestPlan, showCurrentIfNotInfluencing);
		}
	}

	private void SetEntitiesActiveUpToAdv(int index, List<T> baseEntities, int baseIndex, bool showRemovedInLatestPlan, bool showCurrentIfNotInfluencing)
	{
		if (currentPlanLayer != null)
		{
			currentPlanLayer.SetEnabled(false);
		}
		currentPlanLayer = planLayers[index];
		currentPlanLayer.SetEnabled(true);

		//Go through plans backwards from lastPlanIndex
		//Add addedgeometry to the activeEntities and keep a hashset of persistentIDs that should be excluded
		HashSet<int> excludedPersistentIDs = new HashSet<int>(currentPlanLayer.RemovedGeometry);
        //Keeps IDs that were removed in the most recent plan
        HashSet<int> removedDisplayedIDs = showRemovedInLatestPlan ? new HashSet<int>(currentPlanLayer.RemovedGeometry) : new HashSet<int>(); 
		HashSet<int> preModifiedPersisIDs = new HashSet<int>();
		activeEntities = new HashSet<T>();
		preModifiedEntities = new HashSet<T>();
		preExistingPersisIDs = new HashSet<int>();

        //Add current plan layer's entities if they are influencing or part of the plan we're viewing
        if (showCurrentIfNotInfluencing || currentPlanLayer.Plan.InInfluencingState)
        {
            for (int i = 0; i < currentPlanLayer.GetNewGeometryCount(); ++i)
            {
                Entity entity = currentPlanLayer.GetNewGeometryByIndex(i);
                if (IsEntityTypeVisible(entity.EntityTypes))
                {
                    activeEntities.Add((T)entity);
                }
                int PersisID = entity.GetSubEntity(0).GetPersistentID();
                excludedPersistentIDs.Add(PersisID);
                preModifiedPersisIDs.Add(PersisID);
            }
        }

		//Add previous plan layer entities and excluded entities
		for (int i = index - 1; i > baseIndex; i--)
		{
			if (!planLayers[i].Plan.InInfluencingState) //Plans in DESIGN and DELETES states are ignored
				continue;
			for (int entityIndex = 0; entityIndex < planLayers[i].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = planLayers[i].GetNewGeometryByIndex(entityIndex);
				int ID = entity.GetSubEntity(0).GetPersistentID();
				preExistingPersisIDs.Add(ID);
				if (removedDisplayedIDs.Contains(ID)) //Makes geometry active that was removed in the most recent plan
				{
					T typedEntity = (T)entity;
					if (IsEntityTypeVisible(entity.EntityTypes))
					{
						activeEntities.Add(typedEntity);
					}
					preModifiedEntities.Add(typedEntity);
					removedDisplayedIDs.Remove(ID);
				}
				else
				{
					if (preModifiedPersisIDs.Contains(ID)) //Finds the last previous instances of newgeometry
					{
						preModifiedEntities.Add(entity as T);
						preModifiedPersisIDs.Remove(ID);
					}
					if (!excludedPersistentIDs.Contains(ID))//Makes geometry active that was not removed or replaced yet
					{
						if (IsEntityTypeVisible(entity.EntityTypes))
						{
							activeEntities.Add(entity as T);
						}
						excludedPersistentIDs.Add(ID);
					}
				}
			}
			excludedPersistentIDs.UnionWith(planLayers[i].RemovedGeometry);
		}

		//Add geom from the base layer that has not been removed or altered
		foreach (T entity in baseEntities)
		{
			int ID = entity.GetSubEntity(0).GetPersistentID();
			preExistingPersisIDs.Add(ID);
			if (preModifiedPersisIDs.Contains(ID)) //Finds the previous instances of newgeometry
				preModifiedEntities.Add(entity);

			bool isRemovedEntity = removedDisplayedIDs.Contains(ID);
			if (isRemovedEntity || !excludedPersistentIDs.Contains(ID))
			{
				if (IsEntityTypeVisible(entity.EntityTypes))
				{
					activeEntities.Add(entity);
				}
				if (isRemovedEntity)
				{
					preModifiedEntities.Add(entity);
				}
			}
		}
	}

	public override void SetEntitiesActiveUpToCurrentTime()
	{
		if (currentPlanLayer != null)
			currentPlanLayer.SetEnabled(false);
		currentPlanLayer = null;

		//active entities copies Entities (but not the reference)
		activeEntities = new HashSet<T>();
		for (int i = 0; i < Entities.Count; ++i)
		{
			Entity ent = Entities[i];
			if (IsEntityTypeVisible(ent.EntityTypes))
			{
				activeEntities.Add((T)ent);
			}
		}

		preExistingPersisIDs = new HashSet<int>();
		preModifiedEntities = new HashSet<T>();
	}

	void SetEntitiesActiveUpToInitialTime()
	{
		if (currentPlanLayer != null)
			currentPlanLayer.SetEnabled(false);
		currentPlanLayer = null;

		//active entities copies initialEntities (but not the reference)
		activeEntities = new HashSet<T>();
		for (int i = 0; i < initialEntities.Count; ++i)
		{
			Entity ent = initialEntities[i];
			activeEntities.Add((T)ent);
		}

		preExistingPersisIDs = new HashSet<int>();
		preModifiedEntities = new HashSet<T>();
	}

	public override bool IsIDInActiveGeometry(int ID)
	{
		if (activeEntities == null)
			return false;

		foreach (T entity in activeEntities)
		{
			for (int i = 0; i < entity.GetSubEntityCount(); i++)
				if (entity.GetSubEntity(i).GetDatabaseID() == ID)
					return true;
		}
		return false;
	}

	/// <summary>
	/// Did an object with the given persistentID exist before the current plan?
	/// </summary>
	public override bool IsPersisIDCurrentlyNew(int persisID)
	{
		return !preExistingPersisIDs.Contains(persisID);
	}

	public void AddPreModifiedEntity(Entity entity)
	{
		preModifiedEntities.Add(entity as T);
	}

	private void RemovePreModifiedEntity(Entity entity)
	{
		preModifiedEntities.Remove(entity as T);
	}

	public override bool IsDatabaseIDPreModified(int dataBaseID)
	{
		foreach (T entity in preModifiedEntities)
			if (entity.GetSubEntity(0).GetDatabaseID() == dataBaseID)
				return true;
		return false;
	}

	/// <summary>
	/// Goes back from the current planlayer and activates the first Entity with the given persistent ID encountered.
	/// </summary>
	public override void ActivateLastEntityWith(int persistentID)
	{
		foreach (T entity in preModifiedEntities)
			for (int j = 0; j < entity.GetSubEntityCount(); j++)
			{
				SubEntity subEnt = entity.GetSubEntity(j);
				if (subEnt.GetPersistentID() == persistentID)
				{
					activeEntities.Add(entity);
					entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
					if (subEnt.IsNotAffectedByPlan())
						subEnt.RestoreDependencies();
					return;
				}
			}
	}

	/// <summary>
	/// Removes the entity with the given persistent ID from active geometry and redraws it.
	/// </summary>
	public override void DeactivateCurrentEntityWith(int persistentID)
	{
		//for (int i = 0; i < activeEntities.Count; i++)
		foreach (T entity in activeEntities)
		{
			//T entity = activeEntities[i];
			for (int j = 0; j < entity.GetSubEntityCount(); j++)
			{
				SubEntity subEnt = entity.GetSubEntity(j);
				if (subEnt.GetPersistentID() == persistentID)
				{
					activeEntities.Remove(entity);
					if (subEnt.IsNotAffectedByPlan())
						subEnt.RemoveDependencies();
					entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
					return;
				}
			}
		}
	}

	/// <summary>
	/// Merges all planlayers up to the given time with the base layer (clientside).
	/// Sets lastImplementedPlanIndex to reflect this update.
	/// Returns a dictionary with the investment cost per country if we are an energylayer
	/// </summary>
	public override void AdvanceTimeTo(int time)
	{
		if (planLayers.Count <= lastImplementedPlanIndex + 1 || planLayers[lastImplementedPlanIndex + 1].Plan.StartTime > time)
			return;

		int newPlanIndex = lastImplementedPlanIndex;

		//Find index of the most recent plan at the given planTime
		for (int i = planLayers.Count - 1; i > lastImplementedPlanIndex; i--)
			if (planLayers[i].Plan.StartTime <= time)
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
	protected override void MergePlanWithBaseUpToIndex(int newPlanIndex)
	{
		if (newPlanIndex <= lastImplementedPlanIndex)
			return;

		List<T> newEntities = new List<T>();
		List<T> removedEntities = new List<T>();

		//Go through plans backwards from lastPlanIndex to get the state at the given time
		HashSet<int> excludedPersistentIDs = new HashSet<int>();
		for (int i = newPlanIndex; i > lastImplementedPlanIndex; i--)
		{
			for (int entityIndex = 0; entityIndex < planLayers[i].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = planLayers[i].GetNewGeometryByIndex(entityIndex);

				if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
				{
					newEntities.Add(entity as T);
					excludedPersistentIDs.Add(entity.GetSubEntity(0).GetPersistentID());
				}
			}
			excludedPersistentIDs.UnionWith(planLayers[i].RemovedGeometry);
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

		lastImplementedPlanIndex = newPlanIndex;
		if (LayerManager.LayerIsVisible(this) && PlanManager.planViewing == null && PlanManager.timeViewing < 0)
		{
			SetEntitiesActiveUpToCurrentTime();
			RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default, true);
		}
	}

	/// <summary>
	/// Merges the current plan's changes with the baselayer and submits the changes to the server.
	/// Layer should be reactivated and redrawn after the function is done.
	/// </summary>
	public override void MergePlanWithBaseAndSubmitChanges(FSM fsm)
	{
		//UNSAFE TO USE UNTIL REWRITTEN

		//SubmitCallbackTracker tracker = new SubmitCallbackTracker();
		//List<EnergyLineStringSubEntity> addedCables = new List<EnergyLineStringSubEntity>();

		////Make a hashset with all removed persistent IDs
		//HashSet<int> removedFromBase = new HashSet<int>(currentPlanLayer.RemovedGeometry);
		//List<T> removedFromBaseEntities = new List<T>();
		//for (int entityIndex = 0; entityIndex < currentPlanLayer.GetNewGeometryCount(); ++entityIndex)
		//{
		//	Entity entity = currentPlanLayer.GetNewGeometryByIndex(entityIndex);

		//	for (int i = 0; i < entity.GetSubEntityCount(); i++)
		//		removedFromBase.Add(entity.GetSubEntity(i).GetPersistentID());
		//}

		////Submit deleted geometry
		//foreach (T entity in Entities)
		//{
		//	bool removed = false;
		//	//If one of the subentities' persistent ID is in removed geom
		//	for (int i = 0; i < entity.GetSubEntityCount(); i++)
		//		if (removedFromBase.Contains(entity.GetSubEntity(i).GetPersistentID()))
		//		{
		//			removed = true;
		//			break;
		//		}
		//	//Remove all subentities in the object
		//	if (removed)
		//	{
		//		removedFromBaseEntities.Add(entity);
		//		for (int i = 0; i < entity.GetSubEntityCount(); i++)
		//		{
		//			SubEntity removedSubEntity = entity.GetSubEntity(i);
		//			if (removedFromBase.Contains(removedSubEntity.GetPersistentID()))
		//			{
		//				removedSubEntity.SubmitDelete();
		//				entity.RemoveSubEntity(removedSubEntity);
		//				removedSubEntity.RemoveGameObject();
		//			}
		//		}
		//	}
		//}

		////Remove geometry from baselayer
		//foreach (T removedEntity in removedFromBaseEntities)
		//	Entities.Remove(removedEntity);

		////Submit new geometry
		//for (int entityIndex = 0; entityIndex < currentPlanLayer.GetNewGeometryCount(); ++entityIndex)
		//{
		//	Entity entity = currentPlanLayer.GetNewGeometryByIndex(entityIndex);
		//	entity.PlanLayer = null;
		//	for (int i = 0; i < entity.GetSubEntityCount(); i++)
		//	{
		//		SubEntity newSubEntity = entity.GetSubEntity(i);
		//		tracker.StartedSubmission();
		//		newSubEntity.SubmitNew(tracker);
		//		//If it's a cable it will need another submit
		//		if (newSubEntity is EnergyLineStringSubEntity)
		//			addedCables.Add(newSubEntity as EnergyLineStringSubEntity);
		//	}
		//	Entities.Add(entity as T);
		//}

		//ServerCommunication.WaitForCondition(tracker.CompletedAllSubmissions, () => FSM.SubmitConnections(addedCables));

		//currentPlanLayer.ClearNewGeometry();
		//currentPlanLayer.RemovedGeometry = new HashSet<int>();
		//fsm.ClearUndoRedo();
		//RedrawGameObjects(SubEntityDrawMode.Default);
	}

	public override LayerState GetLayerStateAtTime(int month, Plan treatAsInfluencingState = null)
	{
		int planIndex = -1;
		for (int i = 0; i < planLayers.Count; i++)
		{
			if (planLayers[i].Plan.InInfluencingState || planLayers[i].Plan == treatAsInfluencingState)
			{
				if (planLayers[i].Plan.StartTime <= month)
				{
					planIndex = i;
				}
				else
				{
					break;
				}
			}
		}
		return GetLayerStateAtIndex(planIndex);
	}

	public override LayerState GetLayerStateAtPlan(Plan plan)
	{
		int planIndex = -1;
		if (plan != null)
			for (int i = 0; i < planLayers.Count; i++)
			{
                if (planLayers[i].Plan == plan)
                {
                    planIndex = i;
                    break;
                }
                else if (planLayers[i].Plan.StartTime > plan.StartTime || (planLayers[i].Plan.StartTime == plan.StartTime && plan.ID < planLayers[i].Plan.ID))
                {
                    //Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
                    planIndex = i - 1;
                    break;
                }
                else
                    planIndex = i;
			}

		return GetLayerStateAtIndex(planIndex);
	}

	public override LayerState GetLayerStateAtIndex(int planIndex)
	{
		//See SetEntitiesActiveUpTo for a commented (more elaborate) version of this function
		List<Entity> geometry = new List<Entity>();

		//If planindex = -1. only return base geometry
		if (planIndex == -1)
		{
			foreach (T entity in initialEntities)
				geometry.Add(entity);
		}
		//If planindex is the last implemented one, return the current entities
		else if (planIndex == lastImplementedPlanIndex)
		{
			foreach (T entity in Entities)
				geometry.Add(entity);
		}
		else
		{
			HashSet<int> excludedPersistentIDs = new HashSet<int>(planLayers[planIndex].RemovedGeometry);
			bool addCurrent = false;

			for (int entityIndex = 0; entityIndex < planLayers[planIndex].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = planLayers[planIndex].GetNewGeometryByIndex(entityIndex);
				geometry.Add((T)entity);
				excludedPersistentIDs.Add(entity.GetSubEntity(0).GetPersistentID());
			}

			//Add previous plan layer entities and excluded entities
			for (int i = planIndex - 1; i >= 0; i--)
			{
				//If we reach the last implemented plan, stop and add the current state instead of the initial one
				if (i == lastImplementedPlanIndex)
				{
					addCurrent = true;
					break;
				}
				if (!planLayers[i].Plan.InInfluencingState) //Plans in DESIGN and DELETES states are ignored
					continue;
				for (int entityIndex = 0; entityIndex < planLayers[i].GetNewGeometryCount(); ++entityIndex)
				{
					Entity entity = planLayers[i].GetNewGeometryByIndex(entityIndex);
					int ID = entity.GetSubEntity(0).GetPersistentID();
					if (!excludedPersistentIDs.Contains(ID))//Makes geometry active that was not removed or replaced yet
					{
						geometry.Add(entity as T);
						excludedPersistentIDs.Add(ID);
					}
				}
				excludedPersistentIDs.UnionWith(planLayers[i].RemovedGeometry);
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
				foreach (T entity in initialEntities)
					if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
						geometry.Add(entity);
			}
		}

		return new LayerState(geometry, planIndex, this);
	}

	/// <summary>
	/// Should be called on socket layer.
	/// Returns a list of all grids for the current state. 
	/// Grids will have the same name, persisID and distribution if they existed in the given previousEnergyPlan.
	/// Grids that will be there after the plan are removed from removedGrids.
	/// </summary>
	public override List<EnergyGrid> DetermineGrids(Plan plan, List<EnergyGrid> gridsInPlanPreviously, List<EnergyGrid> gridsBeforePlan, HashSet<int> removedGridsBefore, out HashSet<int> removedGridsAfter)
	{
		removedGridsAfter = removedGridsBefore;
		if (editingType != EditingType.Socket)
		{
			return null;
		}

		if ((currentPlanLayer == null || currentPlanLayer.Plan.ID != plan.ID) && planLayers.Count > 0)
		{
			SetEntitiesActiveUpTo(plan);
		}
		if (currentPlanLayer == null)
		{
			return new List<EnergyGrid>();
		}

		//HashSet<int> ignoredGridPersistentIds;
		List<EnergyGrid> result = new List<EnergyGrid>();
		HashSet<SubEntity> visitedSockets = new HashSet<SubEntity>();

		foreach (T entity in activeEntities)
		{
			//If not visited, create new grid
			if (!visitedSockets.Contains(entity.GetSubEntity(0)) && !currentPlanLayer.RemovedGeometry.Contains(entity.GetSubEntity(0).GetPersistentID()))
			{
				EnergyGrid newGrid = new EnergyGrid((EnergyPointSubEntity)entity.GetSubEntity(0), plan);
				foreach (EnergyPointSubEntity socket in newGrid.sockets)
					visitedSockets.Add(socket);

				//Determine if the new grid is the same as one of the old ones, if so: take over distribution and persistentID
				bool identicalGridFound = false;
				bool initialDistributionSet = false;

				//======================= PREVIOUS VERSION OF THIS PLAN ========================

				//Look for grids that were already in this plan that are identical to the new one.
				//If one is found add it to the results. We don't care about partial identicality.

				if (gridsInPlanPreviously != null)
				{
					foreach (EnergyGrid oldGrid in gridsInPlanPreviously)
					{
						if (!oldGrid.MatchesColor(greenEnergy ? EnergyGrid.GridColor.Green : EnergyGrid.GridColor.Grey))
							continue;
						if (newGrid.SocketWiseIdentical(oldGrid))
						{
							//Sockets are identical, are the sources?
							identicalGridFound = newGrid.SourceWiseIdentical(oldGrid);

							if (identicalGridFound)
							{
								//If sources also identical, take over values from previous version of this plan
								initialDistributionSet = true;
								newGrid.name = oldGrid.name;
								newGrid.distributionOnly = oldGrid.distributionOnly;
								if (oldGrid.DatabaseIDSet())
									newGrid.SetDatabaseID(oldGrid.GetDatabaseID());
								newGrid.persistentID = oldGrid.persistentID;
								if (removedGridsAfter.Contains(oldGrid.persistentID))
									removedGridsAfter.Remove(oldGrid.persistentID);
								newGrid.CalculateInitialDistribution(oldGrid);
								result.Add(newGrid);
							}
							break;
						}
					}
				}

				//============================== PREVIOUS PLANS ================================
				//Even if we already found an identical grid, go through here to find grids that we removed
				if (gridsBeforePlan != null)
				{
					foreach (EnergyGrid oldGrid in gridsBeforePlan)
					{
						if (!oldGrid.MatchesColor(greenEnergy ? EnergyGrid.GridColor.Green : EnergyGrid.GridColor.Grey))
							continue;

						if (newGrid.SocketWiseIdentical(oldGrid))
						{
							//If identicalgridfound we already found the grid and are only looking for partials
							//Since this matches exactly, there will be no partials
							if (identicalGridFound)
								break;

							//If sources are also identical, it was unchanged, don't add it to results
							identicalGridFound = newGrid.SourceWiseIdentical(oldGrid);

							if (identicalGridFound)
							{
								//Oldgrid is still present, remove its ID from removed IDs
								if (removedGridsAfter.Contains(oldGrid.persistentID))
									removedGridsAfter.Remove(oldGrid.persistentID);
							}							
							else	//Grids are not sourcewise identical, so take over values from previous one
							{
								initialDistributionSet = true;
								newGrid.name = oldGrid.name;
								newGrid.persistentID = oldGrid.persistentID;
								newGrid.CalculateInitialDistribution(oldGrid);
							}
							break; //We found a socketwise identical grid, no use in continueing to look for matches
						}
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
					else if (removedGridsAfter.Contains(newGrid.persistentID))
						removedGridsAfter.Remove(newGrid.persistentID);
					result.Add(newGrid);
				}
				else if (removedGridsAfter.Contains(newGrid.persistentID))
					removedGridsAfter.Remove(newGrid.persistentID);
			}
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

		foreach (T entity in activeEntities)
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
        if (!IsEnergyLineLayer() || currentPlanLayer == null)
            return null;

        List<EnergyLineStringSubEntity> cablesToRemove = new List<EnergyLineStringSubEntity>();

        foreach (Entity entity in currentPlanLayer.GetNewGeometry())
        {
            //Check the 2 connections for valid points
            EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)entity.GetSubEntity(0);
            foreach (Connection conn in cable.connections)
            {
                //If point is not in active entities, remove the cable.
                if (!conn.point.Entity.Layer.IsIDInActiveGeometry(conn.point.GetDatabaseID()))
                {
                    cablesToRemove.Add(cable);
                    break;
                }
            }
        }

        foreach (EnergyLineStringSubEntity cable in cablesToRemove)
        {
            //Remove from new geometry
            currentPlanLayer.RemoveNewGeometry(cable.Entity);
            //Remove from active entities
            activeEntities.Remove((T)cable.Entity);
            //Remove GO
            cable.RemoveGameObject();
            //Actual removal from the server is done with the batch submit
        }

        if(cablesToRemove.Count > 0)
            DialogBoxManager.instance.NotificationWindow("Removed invalid cables", "The plan contained cables that were no longer connected to points. They have been removed.", null);
		return cablesToRemove;
    }

	public override void RestoreInvalidCables(List<EnergyLineStringSubEntity> cables)
	{
		foreach (EnergyLineStringSubEntity cable in cables)
		{
			if (cable.Entity.Layer == this)
			{
				cable.Entity.PlanLayer.AddNewGeometry(cable.Entity);
				cable.DrawGameObject(LayerGameObject.transform);
				activeEntities.Add((T)cable.Entity);
			}
		}
	}

	/// <summary>
	/// Removes the connections from non-cables, these are then re-added by the currently active cables.
	/// </summary>
	public override void ResetEnergyConnections()
	{
		if (IsEnergyLineLayer())
			return;

		foreach (T entity in activeEntities)
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

		for(int i = lastImplementedPlanIndex + 1; i < planLayers.Count; i++)
			foreach(Entity entity in planLayers[i].GetNewGeometry())
				((IEnergyDataHolder)entity.GetSubEntity(0)).CurrentGrid = null;
	}

	public Dictionary<int, List<DirectionalConnection>> GetCableNetworkAtTime(int month)
	{
		if (!IsEnergyLineLayer() || planLayers.Count == 0)
			return null;

		for (int i = planLayers.Count - 1; i >= 0; i--)
		{
			//Find last plan at time
			if (planLayers[i].Plan.StartTime >= month)
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
	public Dictionary<int, List<DirectionalConnection>> GetCableNetworkForPlan(Plan plan)
	{
		if (!IsEnergyLineLayer() || plan == null || planLayers.Count == 0)
			return null;

		//Find the index of the plan
		int index = -1;
		for (int i = 0; i < planLayers.Count; i++)
		{
			if (planLayers[i].Plan.ID == plan.ID)
			{
				index = i;
				break;
			}
			else if (planLayers[i].Plan.StartTime > plan.StartTime || (planLayers[i].Plan.StartTime == plan.StartTime && plan.ID < planLayers[i].Plan.ID))
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

	private Dictionary<int, List<DirectionalConnection>> GetCableNetworkAtPlanIndex(int index)
	{  
		Dictionary<int, List<DirectionalConnection>> network = new Dictionary<int, List<DirectionalConnection>>();
		HashSet<int> excludedPersistentIDs = new HashSet<int>(planLayers[index].RemovedGeometry);

		for (int entityIndex = 0; entityIndex < planLayers[index].GetNewGeometryCount(); ++entityIndex)
		{
			Entity entity = planLayers[index].GetNewGeometryByIndex(entityIndex);

			EnergyLineStringSubEntity cable = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
			if (cable.connections == null || cable.connections.Count < 2)
			{
				Debug.LogError("Cable without connections in current plan layer. Ignored for grids. ID: " + cable.GetDatabaseID().ToString());
				continue;
			}
			EnergyPointSubEntity p0 = cable.connections[0].point;
			EnergyPointSubEntity p1 = cable.connections[1].point;

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
		for (int i = index - 1; i > lastImplementedPlanIndex; i--)
		{
			for (int entityIndex = 0; entityIndex < planLayers[i].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = planLayers[i].GetNewGeometryByIndex(entityIndex);

				int ID = entity.GetSubEntity(0).GetPersistentID();
				if (!excludedPersistentIDs.Contains(ID))//Makes geometry active that was not removed or replaced yet
				{
					EnergyLineStringSubEntity cable = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
					if (cable.connections == null || cable.connections.Count < 2)
					{
						Debug.LogError("Cable without connections in previous plan layer. Ignored for grids. ID: " + cable.GetDatabaseID().ToString());
						continue;
					}
					EnergyPointSubEntity p0 = cable.connections[0].point;
					EnergyPointSubEntity p1 = cable.connections[1].point;

					//Add connections from p0 to p1 and vice versa to network
					if (network.ContainsKey(p0.GetDatabaseID()))
						network[p0.GetDatabaseID()].Add(new DirectionalConnection(cable, p1));
					else
						network.Add(p0.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p1 )});
					if (network.ContainsKey(p1.GetDatabaseID()))
						network[p1.GetDatabaseID()].Add(new DirectionalConnection(cable, p0));
					else
						network.Add(p1.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p0 )});

					excludedPersistentIDs.Add(ID);
				}
			}
			excludedPersistentIDs.UnionWith(planLayers[i].RemovedGeometry);
		}

		//Add geom from the base layer that has not been removed or altered
		foreach (T entity in Entities)
			if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
			{
				EnergyLineStringSubEntity cable = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
				if (cable.connections == null || cable.connections.Count < 2)
				{
					Debug.LogError("Cable without connections in base data. Ignored for grids. ID: " + cable.GetDatabaseID().ToString());
					continue;
				}
				EnergyPointSubEntity p0 = cable.connections[0].point;
				EnergyPointSubEntity p1 = cable.connections[1].point;

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
	public Dictionary<int, List<EnergyLineStringSubEntity>> GetNodeConnectionsForPlan(Plan plan, Dictionary<int, List<EnergyLineStringSubEntity>> networkToMerge = null)
	{
		if (!IsEnergyLineLayer() || plan == null || planLayers.Count == 0)
			return networkToMerge;

		//Find the index of the plan
		int index = -1;
		for (int i = 0; i < planLayers.Count; i++)
		{
			if (planLayers[i].Plan == plan)
			{
				index = i;
				break;
			}
			else if (planLayers[i].Plan.StartTime > plan.StartTime || (planLayers[i].Plan.StartTime == plan.StartTime && plan.ID < planLayers[i].Plan.ID))
			{
				//Checked plan has higher startime or equal startime but higher ID, meaning it would be later in the list
				index = i - 1;
				break;
			}
		}

		if (index < 0) //No base plan for cables exists
			return networkToMerge;

		Dictionary<int, List<EnergyLineStringSubEntity>> network = networkToMerge == null ? new Dictionary<int, List<EnergyLineStringSubEntity>>() : networkToMerge;
		HashSet<int> excludedPersistentIDs = new HashSet<int>(planLayers[index].RemovedGeometry);

		for (int entityIndex = 0; entityIndex < planLayers[index].GetNewGeometryCount(); ++entityIndex)
		{
			Entity entity = planLayers[index].GetNewGeometryByIndex(entityIndex);

			EnergyLineStringSubEntity subEnt = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
            if (subEnt.connections.Count < 2)
            {
                Debug.LogError("Cable with less than 2 connections encountered when removing energy layer. Cable ID: " + subEnt.GetDatabaseID());
                continue;
            }
            EnergyPointSubEntity p0 = subEnt.connections[0].point;
			EnergyPointSubEntity p1 = subEnt.connections[1].point;

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
		for (int i = index - 1; i > lastImplementedPlanIndex; i--)
		{
			for (int entityIndex = 0; entityIndex < planLayers[i].GetNewGeometryCount(); ++entityIndex)
			{
				Entity entity = planLayers[i].GetNewGeometryByIndex(entityIndex);

				int ID = entity.GetSubEntity(0).GetPersistentID();
				if (!excludedPersistentIDs.Contains(ID))//Makes geometry active that was not removed or replaced yet
				{
					EnergyLineStringSubEntity subEnt = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
                    if (subEnt.connections.Count < 2)
                    {
                        Debug.LogError("Cable with less than 2 connections encountered when removing energy layer. Cable ID: " + subEnt.GetDatabaseID());
                        continue;
                    }
                    EnergyPointSubEntity p0 = subEnt.connections[0].point;
					EnergyPointSubEntity p1 = subEnt.connections[1].point;

					//Add connections from p0 to p1 and vice versa to network
					if (network.ContainsKey(p0.GetDatabaseID()))
						network[p0.GetDatabaseID()].Add(subEnt);
					else
						network.Add(p0.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });
					if (network.ContainsKey(p1.GetDatabaseID()))
						network[p1.GetDatabaseID()].Add(subEnt);
					else
						network.Add(p1.GetDatabaseID(), new List<EnergyLineStringSubEntity> { subEnt });

					excludedPersistentIDs.Add(ID);
				}
			}
			excludedPersistentIDs.UnionWith(planLayers[i].RemovedGeometry);
		}

		//Add geom from the base layer that has not been removed or altered
		foreach (T entity in Entities)
			if (!excludedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
			{
				EnergyLineStringSubEntity subEnt = (entity.GetSubEntity(0) as EnergyLineStringSubEntity);
				if (subEnt.connections.Count < 2)
					continue;

				EnergyPointSubEntity p0 = subEnt.connections[0].point;
				EnergyPointSubEntity p1 = subEnt.connections[1].point;

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
	public override List<SubEntity> GetFirstInstancesOfPersisIDBeforePlan(PlanLayer planLayer, HashSet<int> persistentIDs)
	{
        //Required for alternative method
        List<SubEntity> result = new List<SubEntity>();

        if (currentPlanLayer != null && currentPlanLayer.ID == planLayer.ID)
        {
            foreach (T t in preModifiedEntities)
            {
                if (persistentIDs.Contains(t.PersistentID))
                {
                    result.Add(t.GetSubEntity(0));
                }
            }
        }
        else
        {
            //This is an alternative method for if the assumption that the planlayer is active doesn't hold in the future.
            int targetsLeft = persistentIDs.Count;
            HashSet<int> targetIDs = new HashSet<int>(persistentIDs);


            //Find the index of the layer
            int index = -1;
            for (int i = 0; i < planLayers.Count; i++)
            {
                if (planLayers[i].ID == planLayer.ID)
                {
                    index = i;
                    break;
                }
            }

            for (int i = index - 1; i >= 0 && targetsLeft > 0; i--)
            {
                foreach (Entity entity in planLayers[i].GetNewGeometry())
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

            if (targetsLeft > 0)
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
		return result;
	}
}

class SourceSummary
{
	public int country;
	public EntityType type;
	public long capacity;
	public SourceSummary(int country, EntityType type, long capacity)
	{
		this.country = country;
		this.type = type;
		this.capacity = capacity;
	}
}
