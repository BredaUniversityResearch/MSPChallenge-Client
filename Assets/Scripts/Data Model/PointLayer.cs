using System.Collections.Generic;
using UnityEngine;

public class PointLayer : Layer<PointEntity>
{
    public EnergyPolygonLayer sourcePolyLayer;

    public PointLayer(LayerMeta layerMeta, List<SubEntityObject> layerObjects, EnergyPolygonLayer sourcePolyLayer = null) : base(layerMeta)
    {
        LoadLayerObjects(layerObjects);
        this.sourcePolyLayer = sourcePolyLayer;
    }

    public override void LoadLayerObjects(List<SubEntityObject> layerObjects)
    {
        base.LoadLayerObjects(layerObjects);

		if (layerObjects != null)
		{
			foreach (SubEntityObject layerObject in layerObjects)
			{
				PointEntity ent = new PointEntity(this, layerObject);
				Entities.Add(ent);
				initialEntities.Add(ent);
			}
		}
    }

    public PointEntity CreateNewPointEntity(Vector3 point, List<EntityType> entityType, PlanLayer planLayer)
    {
        PointEntity pointEntity = new PointEntity(this, planLayer, point, entityType, null);
        if (TeamManager.AreWeGameMaster)
            pointEntity.Country = UIManager.GetCurrentTeamSelection();
        
        planLayer.AddNewGeometry(pointEntity);
        return pointEntity;
    }

    public override SubEntity GetSubEntityAt(Vector2 position)
    {
        return GetPointAt(position);
    }

    public PointSubEntity GetPointAt(Vector3 position)
    {
        float threshold = VisualizationUtil.GetSelectMaxDistance();
        threshold *= threshold;

        PointSubEntity closestSubEntity = null;
        float closestDistanceSquared = float.MaxValue;

        foreach (PointEntity pointEntity in activeEntities)
        {
            List<PointSubEntity> subEntities = pointEntity.GetSubEntities();
            foreach (PointSubEntity subEntity in subEntities)
            {
	            if (subEntity.planState != SubEntityPlanState.NotShown)
	            {
					float distanceSquared = (subEntity.GetPosition() - position).sqrMagnitude;
	                if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
	                {
	                    closestSubEntity = subEntity;
	                    closestDistanceSquared = distanceSquared;
	                }
	            }
            }
        }

        return closestSubEntity;
    }

    public override List<SubEntity> GetSubEntitiesAt(Vector2 position)
    {
        Vector3 pos = position;

        float threshold = VisualizationUtil.GetSelectMaxDistance();
        threshold *= threshold;

        List<SubEntity> closestSubEntities = new List<SubEntity>();
        float closestDistanceSquared = float.MaxValue;

        foreach (PointEntity pointEntity in activeEntities)
        {
            List<PointSubEntity> subEntities = pointEntity.GetSubEntities();
            foreach (PointSubEntity subEntity in subEntities)
            {
                float distanceSquared = (subEntity.GetPosition() - pos).sqrMagnitude;
                if (subEntity.planState != SubEntityPlanState.NotShown && distanceSquared < threshold && distanceSquared <= closestDistanceSquared)
                {
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
        }

        return closestSubEntities;
    }

    public HashSet<PointSubEntity> GetPointsInBox(Vector3 boxCornerA, Vector3 boxCornerB)
    {
        Vector3 min = Vector3.Min(boxCornerA, boxCornerB);
        Vector3 max = Vector3.Max(boxCornerA, boxCornerB);

        HashSet<PointSubEntity> result = new HashSet<PointSubEntity>();

        foreach (PointEntity pointEntity in activeEntities)
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

    public override Entity GetEntityAt(Vector2 position)
    {
        PointSubEntity subEntity = GetPointAt(position);
        return subEntity != null ? subEntity.Entity : null;
    }

    public override Entity CreateEntity(SubEntityObject obj)
    {
        return new PointEntity(this, obj);
    }

    public override void UpdateScale(Camera targetCamera)
    {
        foreach (PointEntity point in activeEntities)
        {
            List<PointSubEntity> subpoints = point.GetSubEntities();

            foreach (PointSubEntity subpoint in subpoints)
            {
                subpoint.UpdateScale(targetCamera);
            }
        }
    }

    public List<PointSubEntity> GetAllSubEntities()
    {
        List<PointSubEntity> subEntities = new List<PointSubEntity>();
        foreach (PointEntity entity in activeEntities)
        {
            foreach (PointSubEntity subent in entity.GetSubEntities())
                if (!subent.IsPlannedForRemoval())
                    subEntities.Add(subent);
        }
        return subEntities;
    }

    public override LayerManager.GeoType GetGeoType()
    {
        return LayerManager.GeoType.point;
    }

    #region Legacy stuff
    //public override void MoveAllGeometryTo(Layer destination, int entityTypeOffset)
    //{
    //    if (!(destination is PointLayer)) { Debug.LogError("destination is not a point layer"); return; }
    //    PointLayer dst = destination as PointLayer;

    //    foreach (PointEntity entity in PointEntities)
    //    {
    //        dst.PointEntities.Add(entity);
    //        entity.MovedToLayer(destination, entity.GetEntityTypeKeys()[0] + entityTypeOffset);
    //    }
    //    PointEntities.Clear();
    //}
    #endregion
}
