using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LineStringLayer : Layer<LineStringEntity>
{
    public LineStringLayer(LayerMeta layerMeta, List<SubEntityObject> layerObjects) : base(layerMeta)
    {
        LoadLayerObjects(layerObjects);
        presetProperties.Add("Length", (subent) =>
        {
            LineStringSubEntity lineEntity = (LineStringSubEntity)subent;
            return lineEntity.LineLengthKm.ToString("0.00") + " km";
        });
    }

    public override void LoadLayerObjects(List<SubEntityObject> layerObjects)
    {
        base.LoadLayerObjects(layerObjects);

		if (layerObjects != null)
		{
			foreach (SubEntityObject layerObject in layerObjects)
			{
				LineStringEntity ent = (LineStringEntity)CreateEntity(layerObject);
				Entities.Add(ent);
				initialEntities.Add(ent);
			}
		}
    }

    public LineStringEntity CreateNewLineStringEntity(Vector3 initialPoint, List<EntityType> entityType, PlanLayer planLayer)
    {
		LineStringEntity lineStringEntity = (LineStringEntity)CreateEntity(planLayer, entityType);
        LineStringSubEntity subEntity = new LineStringSubEntity(lineStringEntity);
        if (TeamManager.IsGameMaster)
            lineStringEntity.Country = UIManager.GetCurrentTeamSelection();
        lineStringEntity.AddSubEntity(subEntity);
        subEntity.AddPoint(initialPoint);
        subEntity.AddPoint(initialPoint);
        planLayer.AddNewGeometry(lineStringEntity);
        return lineStringEntity;
    }

    public LineStringEntity CreateNewEnergyLineStringEntity(Vector3 initialPoint, List<EntityType> entityType, EnergyPointSubEntity origin, PlanLayer planLayer)
    {
        LineStringEntity lineStringEntity = (LineStringEntity)CreateEntity(planLayer, entityType);
        EnergyLineStringSubEntity subEntity = new EnergyLineStringSubEntity(lineStringEntity);
        if (TeamManager.IsGameMaster)
            lineStringEntity.Country = UIManager.GetCurrentTeamSelection();
        lineStringEntity.AddSubEntity(subEntity);
        subEntity.AddPoint(initialPoint);

        //Second point that is being edited
        subEntity.AddPoint(initialPoint);

        //Connection from and to origin
        Connection con = new Connection(subEntity, origin, true);
        subEntity.AddConnection(con);
        origin.AddConnection(con);
        planLayer.AddNewGeometry(lineStringEntity);
        return lineStringEntity;
    }

    public LineStringEntity CreateNewLineStringEntity(List<EntityType> entityType, PlanLayer planLayer)
    {
        LineStringEntity lineStringEntity = (LineStringEntity)CreateEntity(planLayer, entityType);
        planLayer.AddNewGeometry(lineStringEntity);
        return lineStringEntity;
    }

    public override Entity CreateEntity(SubEntityObject obj)
    {
        return new LineStringEntity(this, obj);
    }

	public virtual Entity CreateEntity(PlanLayer planLayer, List<EntityType> entityType)
	{
		return new LineStringEntity(this, planLayer, entityType);
	}

	public HashSet<LineStringEntity> GetEntitiesInBox(Vector2 boxCornerA, Vector2 boxCornerB)
    {
        HashSet<LineStringSubEntity> subEntities = GetSubEntitiesInBox(boxCornerA, boxCornerB);
        HashSet<LineStringEntity> result = new HashSet<LineStringEntity>();
        foreach (LineStringSubEntity subEntity in subEntities)
        {
            result.Add(subEntity.Entity as LineStringEntity);
        }
        return result;
    }

    public HashSet<LineStringSubEntity> GetSubEntitiesInBox(Vector2 boxCornerA, Vector2 boxCornerB)
    {
        Vector2 min = Vector2.Min(boxCornerA, boxCornerB);
        Vector2 max = Vector2.Max(boxCornerA, boxCornerB);

        Rect boxBounds = new Rect(min, max - min);

        List<LineStringSubEntity> collisions = new List<LineStringSubEntity>();

        foreach (LineStringEntity entity in activeEntities)
        {
            List<LineStringSubEntity> subEntities = entity.GetSubEntities();
            foreach (LineStringSubEntity subEntity in subEntities)
            {
                if (!subEntity.IsPlannedForRemoval() && boxBounds.Overlaps(subEntity.BoundingBox))
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

    public override Entity GetEntityAt(Vector2 position)
    {
        SubEntity subEntity = GetSubEntityAt(position);
        return (subEntity != null) ? subEntity.Entity : null;
    }

    public override SubEntity GetSubEntityAt(Vector2 position)
    {
        SubEntity result = null;
        float closestDistance = float.MaxValue;

        float maxDistance = VisualizationUtil.GetSelectMaxDistance();
        Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

        foreach (LineStringEntity entity in activeEntities)
        {
            List<LineStringSubEntity> subEntities = entity.GetSubEntities();
            foreach (LineStringSubEntity subEntity in subEntities)
                if (subEntity.planState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.BoundingBox))
                {
                    float dist = subEntity.DistanceToPoint(position);
                    if (dist < closestDistance)
                    {
                        result = subEntity;
                        closestDistance = dist;
                    }
                }
        }

        //None found close enough
        if (closestDistance > maxDistance)
            return null;

        return result;
    }

    public override List<SubEntity> GetSubEntitiesAt(Vector2 position)
    {
        float maxDistance = VisualizationUtil.GetSelectMaxDistance();

        Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

        List<LineStringSubEntity> collisions = new List<LineStringSubEntity>();

        foreach (LineStringEntity entity in activeEntities)
        {
            List<LineStringSubEntity> subEntities = entity.GetSubEntities();
            foreach (LineStringSubEntity subEntity in subEntities)
            {
                if (subEntity.planState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.BoundingBox))
                {
                    collisions.Add(subEntity);
                }
            }
        }

        if (collisions.Count == 0) { return new List<SubEntity>(); }

        List<SubEntity> result = new List<SubEntity>();
        foreach (LineStringSubEntity collision in collisions)
        {
            if (collision.CollidesWithPoint(position, maxDistance))
            {
                result.Add(collision);
            }
        }

        return result;
    }

    public List<LineStringSubEntity> GetAllSubEntities()
    {
        List<LineStringSubEntity> subEntities = new List<LineStringSubEntity>();
        foreach (LineStringEntity entity in activeEntities)
        {
            foreach (LineStringSubEntity subent in entity.GetSubEntities())
                if (!subent.IsPlannedForRemoval())
                    subEntities.Add(subent);
        }
        return subEntities;
    }

    public override void UpdateScale(Camera targetCamera)
    {
        foreach (LineStringEntity entity in activeEntities)
        {
            List<LineStringSubEntity> subEntities = entity.GetSubEntities();

            foreach (LineStringSubEntity subEntity in subEntities)
            {
                subEntity.UpdateScale(targetCamera);
            }
        }
    }

    public override LayerManager.GeoType GetGeoType()
    {
        return LayerManager.GeoType.line;
    }

    #region Legacy Stuff
    //public override void MoveAllGeometryTo(Layer destination, int entityTypeOffset)
    //{
    //    if (!(destination is LineStringLayer)) { Debug.LogError("destination is not a linestring layer"); return; }
    //    LineStringLayer dst = destination as LineStringLayer;

    //    foreach (LineStringEntity entity in LineStringEntities)
    //    {
    //        dst.LineStringEntities.Add(entity);
    //        entity.MovedToLayer(destination, entity.GetEntityTypeKeys()[0] + entityTypeOffset);
    //    }
    //    LineStringEntities.Clear();
    //}
    #endregion
}
