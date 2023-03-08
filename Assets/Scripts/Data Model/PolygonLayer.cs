using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolygonLayer : Layer<PolygonEntity>
	{
		public Texture2D m_innerGlowTexture = null;
		public Rect m_innerGlowBounds = new Rect();

		public PolygonLayer(LayerMeta a_layerMeta, List<SubEntityObject> a_layerObjects) : base(a_layerMeta)
		{
			LoadLayerObjects(a_layerObjects);
			m_presetProperties.Add("Area", (a_subent) =>
			{
				PolygonSubEntity polygonEntity = (PolygonSubEntity)a_subent;
				return polygonEntity.SurfaceAreaSqrKm.ToString("0.00") + " km<sup>2</sup>";
			});
		}

		public override void LoadLayerObjects(List<SubEntityObject> a_layerObjects)
		{
			base.LoadLayerObjects(a_layerObjects);

			if (a_layerObjects == null)
				return;
			foreach (SubEntityObject layerObject in a_layerObjects)
			{
				PolygonEntity ent = new PolygonEntity(this, layerObject);
				Entities.Add(ent);
				InitialEntities.Add(ent);
			}
		}

		public bool HasEntityTypeWithInnerGlow()
		{
			foreach (var kvp in m_entityTypes)
			{
				if (kvp.Value.DrawSettings.InnerGlowEnabled) { return true; }
			}
			return false;
		}

		public void UpdateInnerGlowWithFirstEntityTypeSettings(bool a_forceRecalculate = false)
		{
			SubEntityDrawSettings s = m_entityTypes.GetFirstValue().DrawSettings;
			UpdateInnerGlow(s.InnerGlowRadius, s.InnerGlowIterations, s.InnerGlowMultiplier, s.InnerGlowPixelSize, a_forceRecalculate);
		}

		public void UpdateInnerGlow(int a_innerGlowRadius, int a_innerGlowIterations, float a_innerGlowMultiplier, float a_pixelSize, bool a_forceRecalculate = false)
		{
			if (a_innerGlowIterations == 0 && a_innerGlowMultiplier <= 0 && a_innerGlowRadius == 0)
			{
				Debug.LogError("Inner glow enabled on layer " + FileName + " but is set with default parameters of 0 iterations, 0 multiplier and 0 radius. Is this correct? Ignoring the inner glow for now.");
				return;
			}

			if (a_forceRecalculate)
			{
				MaterialManager.Instance.CalculateInnerGlowTextureData(this, a_innerGlowRadius, a_innerGlowIterations, a_innerGlowMultiplier, a_pixelSize);
			}
			m_innerGlowTexture = MaterialManager.Instance.GetInnerGlowTexture(this, a_innerGlowRadius, a_innerGlowIterations, a_innerGlowMultiplier, a_pixelSize);
			m_innerGlowBounds = MaterialManager.Instance.GetInnerGlowTextureBounds(this, a_innerGlowRadius, a_innerGlowIterations, a_innerGlowMultiplier, a_pixelSize);
		}

		public PolygonEntity CreateNewPolygonEntity(Vector3 a_initialPoint, List<EntityType> a_entityType, PlanLayer a_planLayer)
		{
			PolygonEntity polygonEntity = new PolygonEntity(this, a_planLayer, a_entityType);
			PolygonSubEntity subEntity = m_editingType == EditingType.SourcePolygon ? new EnergyPolygonSubEntity(polygonEntity) : new PolygonSubEntity(polygonEntity);
			polygonEntity.AddSubEntity(subEntity);

			if (SessionManager.Instance.AreWeGameMaster)
				polygonEntity.Country = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SelectedTeam;

			subEntity.AddPoint(a_initialPoint);
			subEntity.AddPoint(a_initialPoint);
			a_planLayer.AddNewGeometry(polygonEntity);
			return polygonEntity;
		}

		public PolygonEntity CreateNewPolygonEntity(List<EntityType> a_entityType, PlanLayer a_planLayer)
		{
			PolygonEntity polygonEntity = new PolygonEntity(this, a_planLayer, a_entityType);
			a_planLayer.AddNewGeometry(polygonEntity);
			return polygonEntity;
		}

		public override Entity CreateEntity(SubEntityObject a_obj)
		{
			return new PolygonEntity(this, a_obj);
		}

		public HashSet<PolygonEntity> GetEntitiesInBox(Vector3 a_boxCornerA, Vector3 a_boxCornerB)
		{
			HashSet<PolygonSubEntity> subEntities = GetSubEntitiesInBox(a_boxCornerA, a_boxCornerB);
			HashSet<PolygonEntity> result = new HashSet<PolygonEntity>();
			foreach (PolygonSubEntity subEntity in subEntities)
			{
				result.Add(subEntity.m_entity as PolygonEntity);
			}
			return result;
		}

		public HashSet<PolygonSubEntity> GetSubEntitiesInBox(Vector3 a_boxCornerA, Vector3 a_boxCornerB)
		{
			Vector3 min = Vector3.Min(a_boxCornerA, a_boxCornerB);
			Vector3 max = Vector3.Max(a_boxCornerA, a_boxCornerB);

			Rect boxBounds = new Rect(min, max - min);

			List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

			foreach (PolygonEntity entity in m_activeEntities)
			{
				List<PolygonSubEntity> subEntities = entity.GetSubEntities();
				foreach (PolygonSubEntity subEntity in subEntities)
				{
					if (!subEntity.IsPlannedForRemoval() && boxBounds.Overlaps(subEntity.m_boundingBox))
					{
						collisions.Add(subEntity);
					}
				}
			}

			if (collisions.Count == 0) { return new HashSet<PolygonSubEntity>(); }

			HashSet<PolygonSubEntity> result = new HashSet<PolygonSubEntity>();

			foreach (PolygonSubEntity collision in collisions)
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
			return (subEntity != null) ? subEntity.m_entity : null;
		}

		public override SubEntity GetSubEntityAt(Vector2 a_position)
		{
			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistancePolygon();

			Rect positionBounds = new Rect(a_position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

			List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

			foreach (PolygonEntity entity in m_activeEntities)
			{
				List<PolygonSubEntity> subEntities = entity.GetSubEntities();
				foreach (PolygonSubEntity subEntity in subEntities)
				{
					if (subEntity.PlanState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.m_boundingBox))
					{
						collisions.Add(subEntity);
					}
				}
			}

			if (collisions.Count == 0) { return null; }

			foreach (PolygonSubEntity collision in collisions)
			{
				if (collision.CollidesWithPoint(a_position, maxDistance))
				{
					return collision;
				}
			}

			return null;
		}

		public override List<SubEntity> GetSubEntitiesAt(Vector2 a_position)
		{
			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistancePolygon();

			Rect positionBounds = new Rect(a_position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

			List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

			foreach (PolygonEntity entity in m_activeEntities)
			{
				List<PolygonSubEntity> subEntities = entity.GetSubEntities();
				foreach (PolygonSubEntity subEntity in subEntities)
				{
					if (subEntity.PlanState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.m_boundingBox))
					{
						collisions.Add(subEntity);
					}
				}
			}

			if (collisions.Count == 0) { return new List<SubEntity>(); }

			List<SubEntity> result = new List<SubEntity>();
			foreach (PolygonSubEntity collision in collisions)
			{
				if (collision.CollidesWithPoint(a_position, maxDistance))
				{
					result.Add(collision);
				}
			}

			return result;
		}

		public List<PolygonSubEntity> GetAllSubEntities()
		{
			List<PolygonSubEntity> subEntities = new List<PolygonSubEntity>();
			foreach (PolygonEntity entity in m_activeEntities)
			{
				foreach (PolygonSubEntity subent in entity.GetSubEntities())
					if (!subent.IsPlannedForRemoval())
						subEntities.Add(subent);
			}
			return subEntities;
		}

		public override void UpdateScale(Camera a_targetCamera)
		{
			foreach (PolygonEntity entity in m_activeEntities)
			{
				List<PolygonSubEntity> subEntities = entity.GetSubEntities();
				foreach (PolygonSubEntity subEntity in subEntities)
				{
					subEntity.UpdateScale(a_targetCamera);
				}
			}
		}

		public override  LayerManager.EGeoType GetGeoType()
		{
			return  LayerManager.EGeoType.polygon;
		}
	}
}
