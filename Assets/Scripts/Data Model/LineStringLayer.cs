using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LineStringLayer : Layer<LineStringEntity>
	{
		public LineStringLayer(LayerMeta a_layerMeta, List<SubEntityObject> a_layerObjects) : base(a_layerMeta)
		{
			LoadLayerObjects(a_layerObjects);
			m_presetProperties.Add("Length", (a_subent) =>
			{
				LineStringSubEntity lineEntity = (LineStringSubEntity)a_subent;
				return lineEntity.LineLengthKm.ToString("0.00") + " km";
			});
		}

		public override void LoadLayerObjects(List<SubEntityObject> a_layerObjects)
		{
			base.LoadLayerObjects(a_layerObjects);

			if (a_layerObjects == null)
				return;
			foreach (SubEntityObject layerObject in a_layerObjects)
			{
				LineStringEntity ent = null;
				try {
					ent = (LineStringEntity)CreateEntity(layerObject);
				}
				catch (InvalidPolygonException e) {
					// If the polygon is invalid, we do not add it
					//   Note that there will already be a log message of this exception from
					//   PolygonSubEntity::ValidatePolygon
					continue;
				}
				Entities.Add(ent);
				InitialEntities.Add(ent);
			}
		}

		public LineStringEntity CreateNewLineStringEntity(Vector3 a_initialPoint, List<EntityType> a_entityType, PlanLayer a_planLayer)
		{
			LineStringEntity lineStringEntity = (LineStringEntity)CreateEntity(a_planLayer, a_entityType);
			LineStringSubEntity subEntity = new LineStringSubEntity(lineStringEntity);
			if (SessionManager.Instance.AreWeGameMaster)
				lineStringEntity.Country = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SelectedTeam;
			lineStringEntity.AddSubEntity(subEntity);
			subEntity.AddPoint(a_initialPoint);
			subEntity.AddPoint(a_initialPoint);
			a_planLayer.AddNewGeometry(lineStringEntity);
			return lineStringEntity;
		}

		public LineStringEntity CreateNewEnergyLineStringEntity(Vector3 a_initialPoint, List<EntityType> a_entityType, EnergyPointSubEntity a_origin, PlanLayer a_planLayer)
		{
			LineStringEntity lineStringEntity = (LineStringEntity)CreateEntity(a_planLayer, a_entityType);
			EnergyLineStringSubEntity subEntity = new EnergyLineStringSubEntity(lineStringEntity);
			if (SessionManager.Instance.AreWeGameMaster)
				lineStringEntity.Country = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SelectedTeam;
			lineStringEntity.AddSubEntity(subEntity);
			subEntity.AddPoint(a_initialPoint);

			//Second point that is being edited
			subEntity.AddPoint(a_initialPoint);

			//Connection from and to origin
			Connection con = new Connection(subEntity, a_origin, true);
			subEntity.AddConnection(con);
			a_origin.AddConnection(con);
			a_planLayer.AddNewGeometry(lineStringEntity);
			return lineStringEntity;
		}

		public LineStringEntity CreateNewLineStringEntity(List<EntityType> a_entityType, PlanLayer a_planLayer)
		{
			LineStringEntity lineStringEntity = (LineStringEntity)CreateEntity(a_planLayer, a_entityType);
			a_planLayer.AddNewGeometry(lineStringEntity);
			return lineStringEntity;
		}

		public override Entity CreateEntity(SubEntityObject a_obj)
		{
			return new LineStringEntity(this, a_obj);
		}

		protected virtual Entity CreateEntity(PlanLayer a_planLayer, List<EntityType> a_entityType)
		{
			return new LineStringEntity(this, a_planLayer, a_entityType);
		}

		public HashSet<LineStringSubEntity> GetSubEntitiesInBox(Vector2 a_boxCornerA, Vector2 a_boxCornerB)
		{
			Vector2 min = Vector2.Min(a_boxCornerA, a_boxCornerB);
			Vector2 max = Vector2.Max(a_boxCornerA, a_boxCornerB);

			Rect boxBounds = new Rect(min, max - min);

			List<LineStringSubEntity> collisions = new List<LineStringSubEntity>();

			foreach (LineStringEntity entity in m_activeEntities)
			{
				List<LineStringSubEntity> subEntities = entity.GetSubEntities();
				foreach (LineStringSubEntity subEntity in subEntities)
				{
					if (!subEntity.IsPlannedForRemoval() && boxBounds.Overlaps(subEntity.m_boundingBox))
					{
						collisions.Add(subEntity);
					}
				}
			}

			if (collisions.Count == 0) { return new HashSet<LineStringSubEntity>(); }

			HashSet<LineStringSubEntity> result = new HashSet<LineStringSubEntity>();

			foreach (LineStringSubEntity collision in collisions)
			{
				if (collision.CollidesWithRect(boxBounds))
				{
					result.Add(collision);
				}
			}

			return result;
		}

		public override Entity GetEntityAt(Vector2 a_position)
		{
			SubEntity subEntity = GetSubEntityAt(a_position);
			return subEntity?.m_entity;
		}

		public override SubEntity GetSubEntityAt(Vector2 a_position)
		{
			SubEntity result = null;
			float closestDistance = float.MaxValue;

			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();
			Rect positionBounds = new Rect(a_position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

			foreach (LineStringEntity entity in m_activeEntities)
			{
				List<LineStringSubEntity> subEntities = entity.GetSubEntities();
				foreach (LineStringSubEntity subEntity in subEntities)
					if (subEntity.PlanState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.m_boundingBox))
					{
						float dist = subEntity.DistanceToPoint(a_position);
						if (dist < closestDistance)
						{
							result = subEntity;
							closestDistance = dist;
						}
					}
			}

			//None found close enough
			return closestDistance > maxDistance ? null : result;
		}

		public override List<SubEntity> GetSubEntitiesAt(Vector2 a_position)
		{
			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();

			Rect positionBounds = new Rect(a_position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

			List<LineStringSubEntity> collisions = new List<LineStringSubEntity>();

			foreach (LineStringEntity entity in m_activeEntities)
			{
				List<LineStringSubEntity> subEntities = entity.GetSubEntities();
				foreach (LineStringSubEntity subEntity in subEntities)
				{
					if (subEntity.PlanState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.m_boundingBox))
					{
						collisions.Add(subEntity);
					}
				}
			}

			if (collisions.Count == 0) { return new List<SubEntity>(); }

			List<SubEntity> result = new List<SubEntity>();
			foreach (LineStringSubEntity collision in collisions)
			{
				if (collision.CollidesWithPoint(a_position, maxDistance))
				{
					result.Add(collision);
				}
			}

			return result;
		}

		public List<LineStringSubEntity> GetAllSubEntities()
		{
			List<LineStringSubEntity> subEntities = new List<LineStringSubEntity>();
			foreach (LineStringEntity entity in m_activeEntities)
			{
				foreach (LineStringSubEntity subent in entity.GetSubEntities())
					if (!subent.IsPlannedForRemoval())
						subEntities.Add(subent);
			}
			return subEntities;
		}

		public override void UpdateScale(Camera a_targetCamera)
		{
			foreach (LineStringEntity entity in m_activeEntities)
			{
				List<LineStringSubEntity> subEntities = entity.GetSubEntities();

				foreach (LineStringSubEntity subEntity in subEntities)
				{
					subEntity.UpdateScale(a_targetCamera);
				}
			}
		}

		public override  LayerManager.EGeoType GetGeoType()
		{
			return  LayerManager.EGeoType.Line;
		}
	}
}
