using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PlanLayer
	{
		public const string STATE_WAIT = "WAIT";
		public const string STATE_ASSEMBLY = "ASSEMBLY";
		public const string STATE_ACTIVE = "ACTIVE";
		public const string STATE_INACTIVE = "INACTIVE";

		public int ID = -1;
		public int creationBatchCallID;

		public Plan Plan { get; set; }
		public AbstractLayer BaseLayer; //Pointer
		private List<Entity> newGeometry; //Object
		public HashSet<int> RemovedGeometry; //Indexed by persistent ID
		public HashSet<PlanIssueObject> issues;
    
		bool isEnabled = false;
		public bool updating = false;

		public PlanLayer(Plan plan, PlanLayerObject layerObject, Dictionary<AbstractLayer, int> layerUpdateTimes)
		{
			Plan = plan;
			BaseLayer = LayerManager.Instance.GetLayerByID(layerObject.original);
			//State = layerObject.state;
			ID = layerObject.layerid;
			issues = new HashSet<PlanIssueObject>(new IssueObjectEqualityComparer());
			foreach (PlanIssueObject issue in layerObject.issues)
			{ 
				if(issue.active)
					issues.Add(issue);
			}

			newGeometry = new List<Entity>();

			bool layerNeedsUpdate = false;

			//new geometry
			int entityCount = layerObject.geometry.Count;
			if (entityCount > 0)
			{
				layerNeedsUpdate = true;
				for (int i = 0; i < entityCount; i++)
				{
					Entity entity = BaseLayer.CreateEntity(layerObject.geometry[i]);
					entity.PlanLayer = this;
					newGeometry.Add(entity);
				}
			}

			//Removed geometry
			if (layerObject.deleted == null || layerObject.deleted.Count == 0)
				RemovedGeometry = new HashSet<int>();
			else
			{
				RemovedGeometry = new HashSet<int>(layerObject.deleted);
				layerNeedsUpdate = true;
			}

			//Add a layer update to the tracker if any changes were on this layer
			if (layerNeedsUpdate && !layerUpdateTimes.ContainsKey(BaseLayer))
			{
				layerUpdateTimes.Add(BaseLayer, plan.StartTime);
			}
		}

		public PlanLayer(Plan a_plan, AbstractLayer a_baseLayer)
		{
			Plan = a_plan;
			BaseLayer = a_baseLayer;
			ClearContent();
		}

		public void ClearContent()
		{
			RemovedGeometry = new HashSet<int>();
			newGeometry = new List<Entity>();
			issues = new HashSet<PlanIssueObject>(new IssueObjectEqualityComparer());
		}

		private SubEntity getSubentityOfNewGeometry(int ID)
		{
			foreach (Entity ent in newGeometry)
			{
				for (int i = 0; i < ent.GetSubEntityCount(); i++)
				{
					SubEntity sub = ent.GetSubEntity(i);
					if (sub.GetDatabaseID() == ID)
						return sub;
				}
			}
			return null;
		}

		private SubEntity getSubentityOfNewGeometryPersistent(int persistentID)
		{
			foreach (Entity ent in newGeometry)
			{
				for (int i = 0; i < ent.GetSubEntityCount(); i++)
				{
					SubEntity sub = ent.GetSubEntity(i);
					if (sub.GetPersistentID() == persistentID)
						return sub;
				}
			}
			return null;
		}

		public bool IsDatabaseIDInNewGeometry(int databaseID)
		{
			foreach (Entity ent in newGeometry)
				for (int i = 0; i < ent.GetSubEntityCount(); i++)
					if (ent.GetSubEntity(i).GetDatabaseID() == databaseID)
						return true;

			return false;
		}

		public bool IsPersistentIDInNewGeometry(int persistentId)
		{
			for (int i = 0; i < newGeometry.Count; ++i)
			{
				Entity ent = newGeometry[i];
				if (ent.PersistentID == persistentId)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsPersistentIDInRemovedGeometry(int persistentID)
		{
			return RemovedGeometry.Contains(persistentID);
		}

		public bool AllNewSubEntitiesHaveIDs()
		{
			foreach (Entity ent in newGeometry)
			{
				int count = ent.GetSubEntityCount();
				for (int i = 0; i < count; i++)
				{
					SubEntity sub = ent.GetSubEntity(i);
					if (!sub.HasDatabaseID())
						return false;
				}
			}
			return true;
		}

		public void UpdatePlanLayer(PlanLayerObject updatedData, Dictionary<AbstractLayer, int> layerUpdateTimes)
		{
			// Puts existing geometry in a dictionary
			Dictionary<int, SubEntity> noLongerAddedGeometry = new Dictionary<int, SubEntity>();
			for (int i = 0; i < newGeometry.Count; ++i)
			{
				SubEntity subEntity = newGeometry[i].GetSubEntity(0);
				noLongerAddedGeometry.Add(subEntity.GetDatabaseID(), subEntity);
			}

			//Updates or creates geometry in updatedGeometry
			for (int i = 0; i < updatedData.geometry.Count; ++i)
			{
				SubEntityObject subEntObj = updatedData.geometry[i];
				if (noLongerAddedGeometry.ContainsKey(subEntObj.id))
				{
					//Update existing entity
					updateSubEntity(noLongerAddedGeometry[subEntObj.id], subEntObj);
					noLongerAddedGeometry.Remove(subEntObj.id); //Removed from no longer added if it was found in the updated geom
				}
				else
				{
					//Create new entity
					Entity entity = BaseLayer.CreateEntity(subEntObj);
					entity.PlanLayer = this;
					newGeometry.Add(entity);
					entity.DrawGameObjects(BaseLayer.LayerGameObject.transform);
				}
			}

			//Remove entities no longer in new geometry
			foreach (var kvp in noLongerAddedGeometry)
				removeNewEntity(kvp.Value.m_entity);

			bool layerNeedsUpdate = updatedData.geometry.Count > 0 || noLongerAddedGeometry.Count > 0;
			if (!layerNeedsUpdate)
			{
				//Check if old and new removed geometry are the same
				if (updatedData.deleted == null)
				{
					if (RemovedGeometry.Count != 0)
						layerNeedsUpdate = true;
				}
				else if (RemovedGeometry.Count != updatedData.deleted.Count)
					layerNeedsUpdate = true;
				else
				{
					foreach (int removedID in updatedData.deleted)
						if (!RemovedGeometry.Contains(removedID))
						{
							layerNeedsUpdate = true;
							break;
						}
				}
			}

			//Copies over the removed geometry
			RemovedGeometry = updatedData.deleted == null ? new HashSet<int>() : new HashSet<int>(updatedData.deleted);

			//Update issues, but only if changes received
			if (updatedData.issues != null)
			{
				foreach (PlanIssueObject issue in updatedData.issues)
				{
					if (issue.active)
						issues.Add(issue); //This will not update existing, so database IDs are not updated here.
					else
						issues.Remove(issue);
				}
			}

			//Add an update request for when the update has been resolved
			if (layerNeedsUpdate && !layerUpdateTimes.ContainsKey(BaseLayer))
				layerUpdateTimes.Add(BaseLayer, Plan.StartTime);
		}

		private void updateSubEntity(SubEntity existingSubEntity, SubEntityObject newSubEntityObj)
		{
			// Update Entity Type
			existingSubEntity.m_entity.EntityTypes = newSubEntityObj.GetEntityType(BaseLayer);

			//Update country
			existingSubEntity.m_entity.Country = newSubEntityObj.country;

			//Update metadata
			if(newSubEntityObj.data != null)
				existingSubEntity.m_entity.metaData = newSubEntityObj.data;

			// Update Geometry
			existingSubEntity.SetDataToObject(newSubEntityObj);
		}

		private void removeNewEntity(Entity existingNewEntity)
		{
			newGeometry.Remove(existingNewEntity);

			int count = existingNewEntity.GetSubEntityCount();
			for (int i = 0; i < count; i++)
			{
				SubEntity sub = existingNewEntity.GetSubEntity(i);
				PolicyLogicEnergy.Instance.RemoveEnergySubEntityReference(sub.GetDatabaseID());
				sub.RemoveGameObject();
			}
		}

		public void SubmitMarkForDeletion(int persistentID, BatchRequest batch)
		{
			if (persistentID == -1)
			{
				Debug.LogError("Trying to mark subentity with persistent ID -1 for deletion. This is impossible.");
				return;
			}

			JObject dataObject = new JObject();
			dataObject.Add("id", persistentID);
			dataObject.Add("plan", Plan.GetDataBaseOrBatchIDReference());
			dataObject.Add("layer", GetDataBaseOrBatchIDReference());
			batch.AddRequest(Server.MarkForDelete(), dataObject, BatchRequest.BatchGroupGeometryDelete);
		}

		public void SubmitUnmarkForDeletion(int persistentID, BatchRequest batch)
		{
			if (persistentID == -1)
			{
				Debug.LogError("Trying to unmark subentity with persistent ID -1 for deletion. This is impossible.");
				return;
			}

			JObject dataObject = new JObject();
			dataObject.Add("id", persistentID);
			dataObject.Add("plan", Plan.GetDataBaseOrBatchIDReference());
			batch.AddRequest(Server.UnmarkForDelete(), dataObject, BatchRequest.BatchGroupGeometryDelete);
		}

		public Rect GetBounds()
		{
			Rect result = BaseLayer.GetLayerBounds();

			foreach (int subEntityID in RemovedGeometry)
			{
				SubEntity subEntity = BaseLayer.GetSubEntityByPersistentID(subEntityID);
				Vector2 min = Vector2.Min(result.min, subEntity.m_boundingBox.min);
				Vector2 max = Vector2.Max(result.max, subEntity.m_boundingBox.max);
				result = new Rect(min, max - min);
			}

			return result;
		}

		public void DrawGameObjects()
		{
			if (BaseLayer.LayerGameObject != null) //Allows plans to be loaded for layers that aren't
				foreach (Entity e in newGeometry)
					e.DrawGameObjects(BaseLayer.LayerGameObject.transform);
		}

		public void RemoveGameObjects()
		{
			if (BaseLayer.LayerGameObject != null)
				foreach (Entity e in newGeometry)
				{
					e.RemoveGameObjects();
				}
			newGeometry = null;
		}

		public bool IsEnabled
		{
			get { return isEnabled; }
		}

		public void SetEnabled(bool enabled)
		{
			if (enabled == isEnabled)
			{
				return;
			}
			isEnabled = enabled;
		}

		public List<SubEntity> GetInstancesOfRemovedGeometry()
		{
			if (RemovedGeometry == null || RemovedGeometry.Count == 0)
				return new List<SubEntity>();
			return BaseLayer.GetFirstInstancesOfPersisIDBeforePlan(this, RemovedGeometry);
		}

		public void AddNewGeometry(Entity entity)
		{
			if (!newGeometry.Contains(entity))
			{
				newGeometry.Add(entity);
			}
		}

		public int GetNewGeometryCount()
		{
			return newGeometry.Count;
		}

		public int GetAddedGeometryCount()
		{
			int result = 0;
			foreach (Entity ent in newGeometry)
			{
				if (ent.DatabaseID == ent.PersistentID)
					result++;
			}
			return result;
		}

		public int GetAlteredGeometryCount()
		{
			int result = 0;
			foreach (Entity ent in newGeometry)
			{
				if (ent.DatabaseID != ent.PersistentID)
					result++;
			}
			return result;
		}

		public Entity GetNewGeometryByIndex(int index)
		{
			return newGeometry[index];
		}

		public IEnumerable<Entity> GetNewGeometry()
		{
			return newGeometry;
		}

		public void RemoveNewGeometry(Entity entity)
		{
			newGeometry.Remove(entity);
		}

		public void ClearNewGeometry()
		{
			newGeometry.Clear();
		}

		public void SubmitNewPlanLayer(BatchRequest batch)
		{
			JObject dataObject = new JObject();
			dataObject.Add("id", Plan.GetDataBaseOrBatchIDReference());
			dataObject.Add("layerid", BaseLayer.m_id);
			creationBatchCallID = batch.AddRequest<int>(Server.AddPlanLayer(), dataObject, BatchRequest.BatchGroupLayerAdd, HandleDatabaseIDResult);
		}
		void HandleDatabaseIDResult(int a_result)
		{
			ID = a_result;
		}

		public string GetDataBaseOrBatchIDReference()
		{
			if (ID != -1)
				return ID.ToString();
			else
				return BatchRequest.FormatCallIDReference(creationBatchCallID);
		}
	}
}