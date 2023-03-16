using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PointLayer : Layer<PointEntity>
	{
		public EnergyPolygonLayer m_sourcePolyLayer;

		public PointLayer(LayerMeta a_layerMeta, List<SubEntityObject> a_layerObjects, EnergyPolygonLayer a_sourcePolyLayer = null) : base(a_layerMeta)
		{
			LoadLayerObjects(a_layerObjects);
			m_sourcePolyLayer = a_sourcePolyLayer;
		}

		public override void LoadLayerObjects(List<SubEntityObject> a_layerObjects)
		{
			base.LoadLayerObjects(a_layerObjects);

			if (a_layerObjects == null)
				return;
			foreach (SubEntityObject layerObject in a_layerObjects)
			{
				PointEntity ent = new PointEntity(this, layerObject);
				Entities.Add(ent);
				InitialEntities.Add(ent);
			}
		}

		public PointEntity CreateNewPointEntity(Vector3 a_point, List<EntityType> a_entityType, PlanLayer a_planLayer)
		{
			PointEntity pointEntity = new PointEntity(this, a_planLayer, a_point, a_entityType, null);
			if (SessionManager.Instance.AreWeGameMaster)
				pointEntity.Country = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SelectedTeam;
        
			a_planLayer.AddNewGeometry(pointEntity);
			return pointEntity;
		}

		public override SubEntity GetSubEntityAt(Vector2 a_position)
		{
			return GetPointAt(a_position);
		}

		public PointSubEntity GetPointAt(Vector3 a_position)
		{
			float threshold = VisualizationUtil.Instance.GetSelectMaxDistance();
			threshold *= threshold;

			PointSubEntity closestSubEntity = null;
			float closestDistanceSquared = float.MaxValue;

			foreach (PointEntity pointEntity in m_activeEntities)
			{
				List<PointSubEntity> subEntities = pointEntity.GetSubEntities();
				foreach (PointSubEntity subEntity in subEntities)
				{
					if (subEntity.PlanState == SubEntityPlanState.NotShown)
						continue;
					float distanceSquared = (subEntity.GetPosition() - a_position).sqrMagnitude;
					if (!(distanceSquared < threshold) || !(distanceSquared < closestDistanceSquared))
						continue;
					closestSubEntity = subEntity;
					closestDistanceSquared = distanceSquared;
				}
			}

			return closestSubEntity;
		}

		public override List<SubEntity> GetSubEntitiesAt(Vector2 a_position)
		{
			Vector3 pos = a_position;

			float threshold = VisualizationUtil.Instance.GetSelectMaxDistance();
			threshold *= threshold;

			List<SubEntity> closestSubEntities = new List<SubEntity>();
			float closestDistanceSquared = float.MaxValue;

			foreach (PointEntity pointEntity in m_activeEntities)
			{
				List<PointSubEntity> subEntities = pointEntity.GetSubEntities();
				foreach (PointSubEntity subEntity in subEntities)
				{
					float distanceSquared = (subEntity.GetPosition() - pos).sqrMagnitude;
					if (subEntity.PlanState == SubEntityPlanState.NotShown || !(distanceSquared < threshold) ||
						!(distanceSquared <= closestDistanceSquared))
						continue;
					if (distanceSquared < closestDistanceSquared)
					{
						closestSubEntities = new List<SubEntity>() { subEntity };
						closestDistanceSquared = distanceSquared;
					}
					else // distanceSquared == closestDistanceSquared
					{
						closestSubEntities.Add(subEntity);
					}
				}
			}

			return closestSubEntities;
		}

		public HashSet<PointSubEntity> GetPointsInBox(Vector3 a_boxCornerA, Vector3 a_boxCornerB)
		{
			Vector3 min = Vector3.Min(a_boxCornerA, a_boxCornerB);
			Vector3 max = Vector3.Max(a_boxCornerA, a_boxCornerB);

			HashSet<PointSubEntity> result = new HashSet<PointSubEntity>();

			foreach (PointEntity pointEntity in m_activeEntities)
			{
				List<PointSubEntity> subEntities = pointEntity.GetSubEntities();
				foreach (PointSubEntity subEntity in subEntities)
				{
					Vector3 position = subEntity.GetPosition();

					if (!subEntity.IsPlannedForRemoval() && position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y)
					{
						result.Add(subEntity);
					}
				}
			}

			return result;
		}

		public override Entity GetEntityAt(Vector2 a_position)
		{
			PointSubEntity subEntity = GetPointAt(a_position);
			return subEntity?.m_entity;
		}

		public override Entity CreateEntity(SubEntityObject a_obj)
		{
			return new PointEntity(this, a_obj);
		}

		public override void UpdateScale(Camera a_targetCamera)
		{
			foreach (PointEntity point in m_activeEntities)
			{
				List<PointSubEntity> subpoints = point.GetSubEntities();

				foreach (PointSubEntity subpoint in subpoints)
				{
					subpoint.UpdateScale(a_targetCamera);
				}
			}
		}

		public List<PointSubEntity> GetAllSubEntities()
		{
			List<PointSubEntity> subEntities = new List<PointSubEntity>();
			foreach (PointEntity entity in m_activeEntities)
			{
				foreach (PointSubEntity subent in entity.GetSubEntities())
					if (!subent.IsPlannedForRemoval())
						subEntities.Add(subent);
			}
			return subEntities;
		}

		public override  LayerManager.EGeoType GetGeoType()
		{
			return  LayerManager.EGeoType.Point;
		}
	}
}
