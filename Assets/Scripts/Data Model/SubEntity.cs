using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MSP2050.Scripts
{
	public enum SubEntityDrawMode { Default, BeingCreated, Hover, Selected, SetOperationSubject, SetOperationClip, Invalid, BeingCreatedInvalid, PlanReference };
	public enum SubEntityPlanState { NotShown, NotInPlan, Added, Removed, Moved }

	public abstract class SubEntity
	{
		private const float LAYER_TYPE_ORDER_MAX_Z_OFFSET = 0.05f;
		private const float FRONT_OF_LAYER_Z_OFFSET = -0.90f;
		private const float FRONT_OF_EVERYTHING_Z_OFFSET = -100.0f;

		public Entity m_entity;
		protected int m_databaseID = -1;
		protected int m_persistentID;
		protected string m_mspID;

		protected GameObject m_gameObject;
		protected SubEntityDrawSettings m_drawSettings;
		public SubEntityDrawSettings DrawSettings => m_drawSettings;
		public SubEntityPlanState PlanState { get; protected set; }
		protected SubEntityPlanState PreviousPlanState { get; private set; } //Temporary until we can move the planState checks out of the RedrawGameObject function
		public bool m_edited; //Has this subentity been altered in the current editing session. Only set for polygons.

		public Rect m_boundingBox;

		public bool SnappingToThisEnabled { get; protected set; }

		protected float m_order = 0;

		public bool m_restrictionNeedsUpdate = false;
		protected bool m_restrictionHidden = false;
		private float m_currentRestrictionSize = 0.0f;

		protected EntityInfoText m_textMesh = null;
		private bool m_textMeshVisibleAtZoom = true;
		
		public bool TextMeshVisibleAtZoom
		{
			get => m_textMeshVisibleAtZoom;
			set {
				if (m_textMeshVisibleAtZoom == value)
					return;
				m_textMeshVisibleAtZoom = value;
				if (value && m_entity.Layer.LayerTextVisible)
					SetTextMeshActivity(true);
				else
					SetTextMeshActivity(false);
			}
		}		

		public delegate void SubEntityVisibilityChangedCallback(SubEntity a_entity, AbstractLayer a_layer, bool a_newVisibility);
		public static event SubEntityVisibilityChangedCallback OnEntityVisibilityChanged;

		protected SubEntity(Entity a_entity, int a_databaseID = -1, int a_persistentID = -1, string a_mspID = null)
		{
			m_entity = a_entity;
			m_databaseID = a_databaseID;
			m_persistentID = a_persistentID;
			m_mspID = a_mspID;
		}

		public abstract void DrawGameObject(Transform a_parent, SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, HashSet<int> a_selectedPoints = null, HashSet<int> a_hoverPoints = null);

		public virtual void RedrawGameObject(SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, HashSet<int> a_selectedPoints = null, HashSet<int> a_hoverPoints = null, bool a_updatePlanState = true)
		{
			if(a_updatePlanState)
				UpdatePlanState();

			float restrictionSize = m_entity.GetCurrentRestrictionSize();
			if (m_restrictionNeedsUpdate || restrictionSize != m_currentRestrictionSize)
			{
				UpdateRestrictionArea(restrictionSize);
			}

			if (m_textMesh == null)
				return;
			m_textMesh.UpdateTextMeshText();
			m_textMesh.SetBackgroundVisibility(a_drawMode == SubEntityDrawMode.Hover || a_drawMode == SubEntityDrawMode.Selected);
		}

		public abstract void UpdateGameObjectForEveryLOD();
		public abstract Vector3 GetPointClosestTo(Vector3 a_position);
		protected abstract void UpdateBoundingBox();
		public abstract void SetOrderBasedOnType();
		protected abstract SubEntityObject GetLayerObject();
		public abstract void UpdateGeometry(GeometryObject a_geo);
		public abstract void SetDataToObject(SubEntityObject a_subEntityObject);

		public void ReAddToEntity()
		{
			m_entity.ReAddSubentity(this);
		}

		protected void calculateOrderBasedOnType()
		{
			// Because keys can have gaps in them
			List<int> entityTypeKeysOrdered = new List<int>();

			foreach (var kvp in m_entity.Layer.m_entityTypes)
			{
				entityTypeKeysOrdered.Add(kvp.Key);
			}

			entityTypeKeysOrdered.Sort(); // smallest first
			entityTypeKeysOrdered.Reverse();

			int currentEntityTypeKey = m_entity.Layer.GetEntityTypeKey(m_entity.EntityTypes[0]);

			m_order = ((float)entityTypeKeysOrdered.IndexOf(currentEntityTypeKey) / (float)m_entity.Layer.m_entityTypes.Count) * LAYER_TYPE_ORDER_MAX_Z_OFFSET;
		}


		public GameObject GetGameObject()
		{
			return m_gameObject;
		}

		public virtual void RemoveGameObject()
		{
			if (m_textMesh != null)
			{
				m_textMesh.Destroy();
				m_textMesh = null;
			}

			Object.Destroy(m_gameObject);
			m_gameObject = null;
		}

		protected virtual void SetDatabaseID(int a_databaseID)
		{
			m_databaseID = a_databaseID;
		}

		public virtual void SetPersistentID(int a_persistentID)
		{
			m_persistentID = a_persistentID;
		}

		public bool HasDatabaseID()
		{
			return m_databaseID != -1;
		}

		public virtual int GetDatabaseID()
		{
			return m_databaseID;
		}

		public virtual string GetDataBaseOrBatchIDReference()
		{
			if (HasDatabaseID())
				return m_databaseID.ToString();
			return BatchRequest.FormatCallIDReference(m_entity.creationBatchCallID);
		}

		public virtual int GetPersistentID()
		{
			return m_persistentID;
		}

		public string GetMspID()
		{
			return m_mspID;
		}

		public bool IsPlannedForRemoval()
		{
			return PlanState == SubEntityPlanState.Removed;
		}

		public bool IsNotShownInPlan()
		{
			return PlanState == SubEntityPlanState.NotShown;
		}

		public bool IsNotAffectedByPlan()
		{
			return PlanState == SubEntityPlanState.NotInPlan;
		}

		public virtual Action<BatchRequest> SubmitUpdate(BatchRequest a_batch)
		{
			JObject dataObject = new JObject();
			if (this is PolygonSubEntity && (this as PolygonSubEntity).GetHoleCount() > 0)
			{
				//Delete the geometry and create a new one
				SubmitDelete(a_batch);
				SubmitNew(a_batch);
			}
			else
			{
				dataObject.Add("geometry", JsonConvert.SerializeObject(GetLayerObject().geometry));
				dataObject.Add("country", m_entity.Country);
				dataObject.Add("id", m_databaseID);

				a_batch.AddRequest<int>(Server.UpdateGeometry(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_UPDATE, HandleDatabaseIDResult);
				SubmitData(a_batch);
			}
			return null;
		}

		public virtual Action<BatchRequest> SubmitDelete(BatchRequest a_batch)
		{
			JObject dataObject = new JObject();
			dataObject.Add("id", m_databaseID);
			a_batch.AddRequest(Server.DeleteGeometry(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DELETE);
			return null;
		}

		protected virtual void SubmitData(BatchRequest a_batch)
		{
			JObject dataObject = new JObject();

			dataObject.Add("id", GetDataBaseOrBatchIDReference());
			dataObject.Add("data", m_entity.MetaToJSON());
			dataObject.Add("type", Util.IntListToString(m_entity.GetEntityTypeKeys()));

			a_batch.AddRequest(Server.SendGeometryData(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
		}

		public virtual Action<BatchRequest> SubmitNew(BatchRequest a_batch)
		{
			JObject dataObject = new JObject();

			dataObject.Add("geometry", JsonConvert.SerializeObject(GetLayerObject().geometry));
			dataObject.Add("country", m_entity.Country);

			if (m_persistentID != -1)
				dataObject.Add("persistent", m_persistentID);

			if (m_entity.PlanLayer != null)
			{
				dataObject.Add("layer", m_entity.PlanLayer.GetDataBaseOrBatchIDReference());
				dataObject.Add("plan", m_entity.PlanLayer.Plan.GetDataBaseOrBatchIDReference());
			}
			else
			{
				dataObject.Add("plan", "");
				dataObject.Add("layer", m_entity.Layer.m_id);
			}

			m_entity.creationBatchCallID = a_batch.AddRequest<int>(Server.PostGeometry(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_ADD, HandleDatabaseIDResult);

			if (this is PolygonSubEntity)
			{
				int total = ((PolygonSubEntity)this).GetHoleCount();

				for (int i = 0; i < total; i++)
				{
					dataObject = new JObject();
					dataObject.Add("geometry", ((PolygonSubEntity)this).HolesToJSON(i));
					dataObject.Add("layer", m_entity.PlanLayer.GetDataBaseOrBatchIDReference());
					dataObject.Add("subtractive", BatchRequest.FormatCallIDReference(m_entity.creationBatchCallID)); 

					a_batch.AddRequest(Server.PostGeometrySub(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
				}
			}
			SubmitData(a_batch);
			return null;
		}

		protected virtual void HandleDatabaseIDResult(int a_result)
		{
			SetDatabaseID(a_result);
			if (GetPersistentID() == -1)
				SetPersistentID(a_result);
		}
	
		public virtual void ForceGameObjectVisibility(bool a_value)
		{
			m_gameObject.SetActive(a_value);
		}
		
		protected void ObjectVisibility(GameObject a_gameObject, float a_distance, Camera a_targetCamera)
		{
			if (a_distance > a_targetCamera.orthographicSize)
			{
				a_gameObject.SetActive(true);
			}
			else
			{
				a_gameObject.SetActive(false);
			}
		}

		protected int GetImportance(string a_entityPropertyName, int a_multiplier, bool a_oneIsLargest = false)
		{
			int importance = int.MaxValue;

			if (!m_entity.DoesPropertyExist(a_entityPropertyName))
				return importance;
			importance = Util.ParseToInt(m_entity.GetMetaData(a_entityPropertyName), int.MaxValue);

			if (a_oneIsLargest)
			{
				importance -= 10;
			}

			importance = (int)Mathf.Abs((float)importance);

			importance *= a_multiplier;

			return importance;
		}

		protected bool IsSnapToDrawMode(SubEntityDrawMode a_drawMode)
		{
			return a_drawMode != SubEntityDrawMode.BeingCreated &&
			       a_drawMode != SubEntityDrawMode.BeingCreatedInvalid &&
			       a_drawMode != SubEntityDrawMode.Selected;
		}

		/// <summary>
		/// Excepts the restriction area to be updated afterwards
		/// </summary>
		public void UnHideRestrictionArea(bool a_forceUpdate = false)
		{
			m_restrictionHidden = false;
			if (a_forceUpdate)
			{
				UpdateRestrictionArea(m_entity.GetCurrentRestrictionSize());
			}
		}

		protected virtual void CreateTextMesh(Transform a_parent, Vector3 a_textPosition, bool a_setWorldPosition = false)
		{
			if (m_textMesh == null)
			{
				m_textMesh = new EntityInfoText(this, m_entity.Layer.m_textInfo, a_parent);
				m_textMesh.SetPosition(a_textPosition, a_setWorldPosition);
			}
    
			ScaleTextMesh();
		}

		public void SetTextMeshActivity(bool a_active)
		{
			if (m_textMesh != null)
				m_textMesh.SetVisibility(a_active);
		}

		protected void ScaleTextMesh(float a_parentScale = 1f)
		{
			if (m_entity.Layer.m_textInfo == null)
			{
				return;
			}

			if (TextMeshVisibleAtZoom)
			{
				if (CameraManager.Instance.cameraZoom.CurrentZoom > m_entity.Layer.m_textInfo.zoomCutoff)
					TextMeshVisibleAtZoom = false;
				else
				{
					if (m_textMesh != null)
					{
						m_textMesh.UpdateTextMeshScale(m_entity.Layer.m_textInfo.UseInverseScale, a_parentScale);
					}
				}
			}
			else
			{
				if (CameraManager.Instance.cameraZoom.CurrentZoom <= m_entity.Layer.m_textInfo.zoomCutoff)
				{
					TextMeshVisibleAtZoom = true;
					if (m_textMesh != null)
					{
						m_textMesh.UpdateTextMeshScale(m_entity.Layer.m_textInfo.UseInverseScale, a_parentScale);
					}
				}
			}
		}

		public void UpdateTextMeshText()
		{
			if (m_textMesh != null)
			{
				m_textMesh.UpdateTextMeshText();
			}
		}

		protected virtual void UpdatePlanState()
		{
			PreviousPlanState = PlanState;
			PlanState = PlanManager.Instance.GetSubEntityPlanState(this);

			if (m_gameObject == null)
				return;
			if (PlanState == SubEntityPlanState.NotShown)
			{
				m_gameObject.SetActive(false);
				NotifySubEntityVisibilityChanged();
			}
			else if (PreviousPlanState == SubEntityPlanState.NotShown)
			{
				m_gameObject.SetActive(true);
				NotifySubEntityVisibilityChanged();
			}
		}

		public void SetPlanState(SubEntityPlanState a_newState)
		{
			PreviousPlanState = PlanState;
			PlanState = a_newState;
			if (m_gameObject == null)
				return;
			if (PlanState == SubEntityPlanState.NotShown)
			{
				m_gameObject.SetActive(false);
				NotifySubEntityVisibilityChanged();
			}
			else if (PreviousPlanState == SubEntityPlanState.NotShown)
			{
				m_gameObject.SetActive(true);
				NotifySubEntityVisibilityChanged();
			}
		}

		public void NotifySubEntityVisibilityChanged()
		{
			bool newVisibility = m_gameObject != null && m_gameObject.activeInHierarchy;
			if (OnEntityVisibilityChanged != null)
			{
				OnEntityVisibilityChanged.Invoke(this, m_entity.Layer, newVisibility);
			}
		}

		public SubEntityDataCopy GetDataCopy()
		{
			return new SubEntityDataCopy(
				new List<EntityType>(m_entity.EntityTypes),
				new List<Vector3>(GetPoints()),
				new Dictionary<string, string>(m_entity.metaData),
				GetHoles(true),
				m_entity.Country,
				m_edited
			);
		}

		public void SetInFrontOfLayer(bool a_isInFrontOfLayer)
		{
			if (m_gameObject == null)
				return;
			Vector3 oldPos = m_gameObject.transform.position;
			if (a_isInFrontOfLayer)
			{
				m_gameObject.transform.localPosition = new Vector3(oldPos.x, oldPos.y, m_order + FRONT_OF_LAYER_Z_OFFSET);
				if (m_textMesh != null)
				{
					m_textMesh.SetZOffset(FRONT_OF_EVERYTHING_Z_OFFSET);
				}
			}
			else
			{
				m_gameObject.transform.localPosition = new Vector3(oldPos.x, oldPos.y, m_order);
				if (m_textMesh != null)
				{
					m_textMesh.SetZOffset(0.0f);
				}
			}
		}

		public virtual void SetDataToCopy(SubEntityDataCopy a_copy)
		{
			SetPoints(a_copy.m_pointsCopy);
			SetHoles(a_copy.m_holesCopy);
			m_entity.EntityTypes = a_copy.m_entityTypeCopy;
			m_entity.metaData = a_copy.m_metaDataCopy;
			m_entity.Country = a_copy.m_country;
			m_edited = a_copy.m_edited;
		}

		public virtual void FinishEditing()
		{
			m_edited = false;
		}

		public string GetProperty(string a_key)
		{
			if (m_entity.Layer.m_presetProperties.ContainsKey(a_key))
			{
				return m_entity.Layer.m_presetProperties[a_key](this);
			}
			else
			{
				string result;
				m_entity.TryGetMetaData(a_key, out result);
				return result;
			}
		}
		public virtual void UpdateScale(Camera a_targetCamera)
		{ }
		protected virtual void SetPoints(List<Vector3> a_points)
		{ }
		protected virtual void SetHoles(List<List<Vector3>> a_holes)
		{ }
		public virtual List<Vector3> GetPoints()
		{ return null; }
		public virtual List<List<Vector3>> GetHoles(bool a_copy = false)
		{ return null; }

		public abstract Feature GetGeoJsonFeature(int a_idToUse);

		public virtual void SetPropertiesToGeoJsonFeature(Feature a_feature)
		{
			foreach (var kvp in a_feature.Properties)
			{
				m_entity.SetMetaData(kvp.Key, kvp.Value.ToString());
			}
		}

		public void WarningIfDeletingExisting(string a_existingType, string a_warningText, Plan a_affectingPlan)
		{
			if (m_entity != null && m_entity.PlanLayer != null && m_entity.PlanLayer.Plan != null && a_affectingPlan.ID != m_entity.PlanLayer.Plan.ID)
			{
				PlayerNotifications.AddNotification(
					$"WarningDeletingExisting{a_existingType}.{a_affectingPlan.ID}",
					$"{a_existingType} Removed",
					string.Format(a_warningText, a_affectingPlan.Name, m_entity.PlanLayer.Plan.StartTime < 0 ? "at the start of the game" : "in plan '" + a_affectingPlan.Name + "'")
				);
			}
		}

		public void WarningIfEditingExisting(string a_existingType, string a_warningText)
		{
			if (GetDatabaseID() != GetPersistentID())
			{
				Plan originalPlan = null;
				SubEntity originalSubEntity = LayerManager.Instance.FindSubEntityByPersistentID(GetPersistentID());
				if (originalSubEntity != null && originalSubEntity.m_entity != null && originalSubEntity.m_entity.PlanLayer != null && originalSubEntity.m_entity.PlanLayer.Plan != null)
				{
					originalPlan = originalSubEntity.m_entity.PlanLayer.Plan;
				}
				PlayerNotifications.AddNotification(
					$"WarningEditingExisting{a_existingType}.{m_entity.PlanLayer.Plan.ID}",
					$"{a_existingType} Edited",
					string.Format(a_warningText, m_entity.PlanLayer.Plan.Name, (originalPlan == null || originalPlan.StartTime < 0) ? "at the start of the game" : "in plan '" + originalPlan.Name + "'")
				);
			}
		}

		public void WarningIfAddingToExisting(string a_existingType, string a_warningText, Plan a_affectingPlan)
		{
			if (m_entity != null && m_entity.PlanLayer != null && m_entity.PlanLayer.Plan != null && a_affectingPlan.ID != m_entity.PlanLayer.Plan.ID)
			{
				PlayerNotifications.AddNotification(
					$"WarningAddingToExisting{a_existingType}.{a_affectingPlan.ID}",
					$"{a_existingType} Affected",
					string.Format(a_warningText, a_affectingPlan.Name, m_entity.PlanLayer.Plan.StartTime < 0 ? "at the start of the game" : "in plan '" + m_entity.PlanLayer.Plan.Name + "'")
				);
			}
		}

		#region Virtual methods
		//Overridden for energy & shipping functions
		public virtual void RemoveDependencies()
		{ }

		//Overridden for energy & shipping functions
		public virtual void RestoreDependencies()
		{ }

		//Overridden for energy & shipping functions
		public virtual void ActivateConnections()
		{ }

		//Clears the list of connections. These are re-added when an energy layer is activated.
		public virtual void ClearConnections()
		{ }

		protected virtual void UpdateRestrictionArea(float a_newRestrictionSize)
		{
			m_currentRestrictionSize = a_newRestrictionSize;
		}

		public virtual void HideRestrictionArea()
		{
			m_restrictionHidden = true;
			//Children should disable the restriction gameobject
		}

		public virtual void CalculationPropertyUpdated(EntityPropertyMetaData a_property)
		{ }
		#endregion

	}

	public class SubEntityDataCopy
	{
		public readonly List<EntityType> m_entityTypeCopy;
		public readonly List<Vector3> m_pointsCopy;
		public readonly List<List<Vector3>> m_holesCopy;
		public readonly Dictionary<string, string> m_metaDataCopy;
		public readonly int m_country;
		public readonly bool m_edited;

		public SubEntityDataCopy(List<EntityType> a_entityTypeCopy, List<Vector3> a_pointsCopy, Dictionary<string, string> a_metaDataCopy, List<List<Vector3>> a_holesCopy, int a_country, bool a_edited)
		{
			m_entityTypeCopy = a_entityTypeCopy;
			m_pointsCopy = a_pointsCopy;
			m_metaDataCopy = a_metaDataCopy;
			m_holesCopy = a_holesCopy;
			m_country = a_country;
			m_edited = a_edited;
		}
	}
}
