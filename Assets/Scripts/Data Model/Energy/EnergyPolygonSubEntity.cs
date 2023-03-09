using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EnergyPolygonSubEntity : PolygonSubEntity, IEnergyDataHolder
	{
		public EnergyPointSubEntity m_sourcePoint;
		private long m_cachedMaxCapacity;
		
		public long Capacity
		{
			get
			{
				if (m_edited)
					return (long)((double)m_entity.EntityTypes[0].capacity * (double)SurfaceAreaSqrKm);
				return m_cachedMaxCapacity;
			}
			set => m_cachedMaxCapacity = value;
		}

		public long UsedCapacity
		{
			get => m_sourcePoint.UsedCapacity;
			set => m_sourcePoint.UsedCapacity = value;
		}

		public EnergyGrid LastRunGrid
		{
			get => m_sourcePoint.LastRunGrid;
			set => m_sourcePoint.LastRunGrid = value;
		}

		public EnergyGrid CurrentGrid
		{
			get => m_sourcePoint.CurrentGrid;
			set => m_sourcePoint.CurrentGrid = value;
		}		

		public EnergyPolygonSubEntity(Entity a_entity, int a_persistentID = -1)
			: base(a_entity, a_persistentID)
		{
			CreateSourcePoint();
		}

		public EnergyPolygonSubEntity(Entity a_entity, SubEntityObject a_geometry, int a_databaseID)
			: base(a_entity, a_geometry, a_databaseID)
		{
			//Base calls initialise
			PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
		}
		public override void Initialise()
		{
			CreateSourcePoint();
			m_cachedMaxCapacity = (long)((double)m_entity.EntityTypes[0].capacity * (double)SurfaceAreaSqrKm);
			base.Initialise();
		}

		protected override void SetDatabaseID(int a_databaseID)
		{
			PolicyLogicEnergy.Instance.RemoveEnergySubEntityReference(a_databaseID);
			m_databaseID = a_databaseID;
			PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
		}

		public override Action<BatchRequest> SubmitDelete(BatchRequest a_batch)
		{
			// Delete energy_output
			JObject dataObject = new JObject {
				{
					"id", m_databaseID
				}
			};
			a_batch.AddRequest(Server.DeleteEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_DELETE);

			return base.SubmitDelete(a_batch);
		}

		protected override void SubmitData(BatchRequest a_batch)
		{
			base.SubmitData(a_batch);

			//Set energy_output
			JObject dataObject = new JObject {
				{
					"id", GetDataBaseOrBatchIDReference()
				},
				{
					"capacity", 0
				},
				{
					"maxcapacity", Capacity.ToString()
				}
			};
			a_batch.AddRequest(Server.SetEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
		}
	
		protected override void UpdateBoundingBox()
		{
			base.UpdateBoundingBox();
			m_sourcePoint.SetPosition(m_boundingBox.center);
		}

		public override void RemoveDependencies()
		{
			//Remove sourcePoint
			if (m_sourcePoint == null)
				return;
			PointLayer centerPointLayer = ((EnergyPolygonLayer)m_entity.Layer).m_centerPointLayer;
			PointEntity sourceEntity = m_sourcePoint.m_entity as PointEntity;
			centerPointLayer.m_activeEntities.Remove(sourceEntity);
		}

		public override void RestoreDependencies()
		{
			PolicyLogicEnergy.Instance.AddEnergySubEntityReference(m_databaseID, this);

			//Restore sourcePoint
			if (m_sourcePoint == null)
				return;
			PointLayer centerPointLayer = ((EnergyPolygonLayer)m_entity.Layer).m_centerPointLayer;
			PointEntity sourceEntity = m_sourcePoint.m_entity as PointEntity;
			centerPointLayer.m_activeEntities.Add(sourceEntity);
			m_sourcePoint.SetPosition(m_boundingBox.center);
			m_sourcePoint.RedrawGameObject(); //Redraws to new position
		}

		private void CreateSourcePoint()
		{
			if (m_sourcePoint != null)
				return;
			PointLayer centerPointLayer = ((EnergyPolygonLayer)m_entity.Layer).m_centerPointLayer;
			PointEntity ent = new PointEntity(centerPointLayer, null, Vector3.zero, new List<EntityType>() { centerPointLayer.m_entityTypes[0] }, this);
			m_sourcePoint = ent.GetSubEntity(0) as EnergyPointSubEntity;
		}

		public override void ClearConnections()
		{
			m_sourcePoint.ClearConnections();
		}

		public override void UpdateScale(Camera a_targetCamera)
		{
			base.UpdateScale(a_targetCamera);
			m_sourcePoint.UpdateScale(a_targetCamera);
		}

		public void DeactivateSourcePoint()
		{
			((EnergyPolygonLayer)m_entity.Layer).m_centerPointLayer.m_activeEntities.Remove(m_sourcePoint.m_entity as PointEntity);
			m_sourcePoint.RedrawGameObject();
		}

		public override void DrawGameObject(Transform a_parent, SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, HashSet<int> a_selectedPoints = null, HashSet<int> a_hoverPoints = null)
		{
			PointLayer centerPointLayer = (PointLayer)m_sourcePoint.m_entity.Layer;
			PointEntity centerPoint = (PointEntity)m_sourcePoint.m_entity;
			centerPointLayer.Entities.Add(centerPoint);
			centerPointLayer.m_activeEntities.Add(centerPoint);
			m_sourcePoint.DrawGameObject(centerPointLayer.LayerGameObject.transform);
			base.DrawGameObject(a_parent, a_drawMode, a_selectedPoints, a_hoverPoints);
		}

		public override void RedrawGameObject(SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, HashSet<int> a_selectedPoints = null, HashSet<int> a_hoverPoints = null, bool a_updatePlanState = true)
		{
			base.RedrawGameObject(a_drawMode, a_selectedPoints, a_hoverPoints, a_updatePlanState);

			GameObject sourcePointObject = m_sourcePoint.GetGameObject();
			if (sourcePointObject == null)
				return;
			m_sourcePoint.SetPlanState(PlanState);

			//Redraw sourcepoints without updating its plan state
			m_sourcePoint.RedrawGameObject(SubEntityDrawMode.Default, null, null, false);
		}

		public override void RemoveGameObject()
		{
			base.RemoveGameObject();
			if (m_sourcePoint == null)
				return;
			((PointLayer)m_sourcePoint.m_entity.Layer).Entities.Remove((PointEntity)m_sourcePoint.m_entity);
			m_sourcePoint.RemoveGameObject();
		}

		public override void ForceGameObjectVisibility(bool a_value)
		{
			base.ForceGameObjectVisibility(a_value);
			m_sourcePoint.SetPlanState(a_value ? SubEntityPlanState.NotInPlan : SubEntityPlanState.NotShown);
			m_sourcePoint.ForceGameObjectVisibility(a_value);
		}

		public override void FinishEditing()
		{
			if (m_edited)
				m_cachedMaxCapacity = Capacity;
			base.FinishEditing();
		}
    
		public override Dictionary<string, object> GetGeoJSONProperties()
		{
			Dictionary<string, object> properties = new Dictionary<string, object>();
			properties.Add("type", "windfarm");
			return properties;
		}
	}
}
