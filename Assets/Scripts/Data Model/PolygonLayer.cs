using System.Collections.Generic;
using UnityEngine;

public class PolygonLayer : Layer<PolygonEntity>
{
    public Texture2D InnerGlowTexture = null;
    public Rect InnerGlowBounds = new Rect();

    public PolygonLayer(LayerMeta layerMeta, List<SubEntityObject> layerObjects) : base(layerMeta)
    {
        LoadLayerObjects(layerObjects);
        presetProperties.Add("Area", (subent) =>
        {
            PolygonSubEntity polygonEntity = (PolygonSubEntity)subent;
            return polygonEntity.SurfaceAreaSqrKm.ToString("0.00") + " km²";
        });
    }

    public override void LoadLayerObjects(List<SubEntityObject> layerObjects)
    {
        base.LoadLayerObjects(layerObjects);

		if(layerObjects != null)
		{
			foreach (SubEntityObject layerObject in layerObjects)
			{
				PolygonEntity ent = new PolygonEntity(this, layerObject);
				Entities.Add(ent);
				initialEntities.Add(ent);
			}
		}
    }

    public bool HasEntityTypeWithInnerGlow()
    {
        foreach (var kvp in EntityTypes)
        {
            if (kvp.Value.DrawSettings.InnerGlowEnabled) { return true; }
        }
        return false;
    }

    public void UpdateInnerGlowWithFirstEntityTypeSettings(bool forceRecalculate = false)
    {
        SubEntityDrawSettings s = EntityTypes.GetFirstValue().DrawSettings;
        UpdateInnerGlow(s.InnerGlowRadius, s.InnerGlowIterations, s.InnerGlowMultiplier, s.InnerGlowPixelSize, forceRecalculate);
    }

    public void UpdateInnerGlow(int innerGlowRadius, int innerGlowIterations, float innerGlowMultiplier, float pixelSize, bool forceRecalculate = false)
    {
		if (innerGlowIterations == 0 && innerGlowMultiplier <= 0 && innerGlowRadius == 0)
		{
			Debug.LogError("Inner glow enabled on layer " + FileName + " but is set with default parameters of 0 iterations, 0 multiplier and 0 radius. Is this correct? Ignoring the inner glow for now.");
			return;
		}

		if (forceRecalculate)
        {
            MaterialManager.CalculateInnerGlowTextureData(this, innerGlowRadius, innerGlowIterations, innerGlowMultiplier, pixelSize);
        }
        InnerGlowTexture = MaterialManager.GetInnerGlowTexture(this, innerGlowRadius, innerGlowIterations, innerGlowMultiplier, pixelSize);
        InnerGlowBounds = MaterialManager.GetInnerGlowTextureBounds(this, innerGlowRadius, innerGlowIterations, innerGlowMultiplier, pixelSize);
    }

    public PolygonEntity CreateNewPolygonEntity(Vector3 initialPoint, List<EntityType> entityType, PlanLayer planLayer)
    {
        PolygonEntity polygonEntity = new PolygonEntity(this, planLayer, entityType);
        PolygonSubEntity subEntity = editingType == EditingType.SourcePolygon ? new EnergyPolygonSubEntity(polygonEntity) : new PolygonSubEntity(polygonEntity);
        polygonEntity.AddSubEntity(subEntity);

        if (TeamManager.IsGameMaster)
            polygonEntity.Country = UIManager.GetCurrentTeamSelection();

        subEntity.AddPoint(initialPoint);
        subEntity.AddPoint(initialPoint);
        planLayer.AddNewGeometry(polygonEntity);
        return polygonEntity;
    }

    public PolygonEntity CreateNewPolygonEntity(List<EntityType> entityType, PlanLayer planLayer)
    {
        PolygonEntity polygonEntity = new PolygonEntity(this, planLayer, entityType);
        planLayer.AddNewGeometry(polygonEntity);
        return polygonEntity;
    }

    public override Entity CreateEntity(SubEntityObject obj)
    {
        return new PolygonEntity(this, obj);
    }

    public HashSet<PolygonEntity> GetEntitiesInBox(Vector3 boxCornerA, Vector3 boxCornerB)
    {
        HashSet<PolygonSubEntity> subEntities = GetSubEntitiesInBox(boxCornerA, boxCornerB);
        HashSet<PolygonEntity> result = new HashSet<PolygonEntity>();
        foreach (PolygonSubEntity subEntity in subEntities)
        {
            result.Add(subEntity.Entity as PolygonEntity);
        }
        return result;
    }

    public HashSet<PolygonSubEntity> GetSubEntitiesInBox(Vector3 boxCornerA, Vector3 boxCornerB)
    {
        Vector3 min = Vector3.Min(boxCornerA, boxCornerB);
        Vector3 max = Vector3.Max(boxCornerA, boxCornerB);

        Rect boxBounds = new Rect(min, max - min);

        List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

        foreach (PolygonEntity entity in activeEntities)
        {
            List<PolygonSubEntity> subEntities = entity.GetSubEntities();
            foreach (PolygonSubEntity subEntity in subEntities)
            {
                if (!subEntity.IsPlannedForRemoval() && boxBounds.Overlaps(subEntity.BoundingBox))
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

    public override Entity GetEntityAt(Vector2 position)
    {
        SubEntity subEntity = GetSubEntityAt(position);
        return (subEntity != null) ? subEntity.Entity : null;
    }

    public override SubEntity GetSubEntityAt(Vector2 position)
    {
        float maxDistance = VisualizationUtil.GetSelectMaxDistancePolygon();

        Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

        List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

        foreach (PolygonEntity entity in activeEntities)
        {
            List<PolygonSubEntity> subEntities = entity.GetSubEntities();
            foreach (PolygonSubEntity subEntity in subEntities)
            {
                if (subEntity.planState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.BoundingBox))
                {
                    collisions.Add(subEntity);
                }
            }
        }

        if (collisions.Count == 0) { return null; }

        foreach (PolygonSubEntity collision in collisions)
        {
            if (collision.CollidesWithPoint(position, maxDistance))
            {
                return collision;
            }
        }

        return null;
    }

    public override List<SubEntity> GetSubEntitiesAt(Vector2 position)
    {
        float maxDistance = VisualizationUtil.GetSelectMaxDistancePolygon();

        Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

        List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

        foreach (PolygonEntity entity in activeEntities)
        {
            List<PolygonSubEntity> subEntities = entity.GetSubEntities();
            foreach (PolygonSubEntity subEntity in subEntities)
            {
                if (subEntity.planState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.BoundingBox))
                {
                    collisions.Add(subEntity);
                }
            }
        }

        if (collisions.Count == 0) { return new List<SubEntity>(); }

        List<SubEntity> result = new List<SubEntity>();
        foreach (PolygonSubEntity collision in collisions)
        {
            if (collision.CollidesWithPoint(position, maxDistance))
            {
                result.Add(collision);
            }
        }

        return result;
    }

    public List<PolygonSubEntity> GetAllSubEntities()
    {
        List<PolygonSubEntity> subEntities = new List<PolygonSubEntity>();
        foreach (PolygonEntity entity in activeEntities)
        {
            foreach (PolygonSubEntity subent in entity.GetSubEntities())
                if (!subent.IsPlannedForRemoval())
                    subEntities.Add(subent);
        }
        return subEntities;
    }

    public override void UpdateScale(Camera targetCamera)
    {
        foreach (PolygonEntity entity in activeEntities)
        {
            List<PolygonSubEntity> subEntities = entity.GetSubEntities();
            foreach (PolygonSubEntity subEntity in subEntities)
            {
                subEntity.UpdateScale(targetCamera);
            }
        }
    }

    public override LayerManager.GeoType GetGeoType()
    {
        return LayerManager.GeoType.polygon;
    }

    #region Legacy Stuff
    //public override void MoveAllGeometryTo(Layer destination, int entityTypeOffset)
    //{
    //    if (!(destination is PolygonLayer)) { Debug.LogError("destination is not a polygon layer"); return; }
    //    PolygonLayer dst = destination as PolygonLayer;

    //    foreach (PolygonEntity entity in Entities)
    //    {
    //        dst.Entities.Add(entity);
    //        entity.MovedToLayer(destination, entity.GetEntityTypeKeys()[0] + entityTypeOffset);
    //    }
    //    Entities.Clear();
    //}

    //public override void TransformAllEntities(float scale, Vector3 translate)
    //{
        //foreach (PolygonEntity entity in PolygonEntities)
        //{
        //    foreach (PolygonSubEntity subEntity in entity.GetSubEntities())
        //    {
        //        int total = subEntity.GetPolygonPointCount();
        //        for (int i = 0; i < total; i++)
        //        {
        //            subEntity.GetPolygon()[i] *= scale;
        //            subEntity.GetPolygon()[i] += translate;
        //        }

        //        subEntity.SubmitUpdate();
        //        Thread.Sleep(15);
        //    }
        //}
        //  RedrawGameObjects();
    //}

    public void CreateInvertedLayer()
    {
        //if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        //{
        //    Debug.LogError("Please press shift while pressing this button if you really want to create an inverted layer");
        //    return;
        //}

        //UIManager.CreateConfirmWindow("Create Inverted Layer", "Are you sure you want to create an inverted layer?", 200, () => { LayerManager.AddNewLayer(FileName + " (inverted)", LayerManager.GeoType.polygon, invertedLayerCreated); });


    }

    private void invertedLayerCreated(AbstractLayer layer)
    {
        //PolygonLayer l = layer as PolygonLayer;
        //l.Selectable = false;

        //if (ShortName.Length > 0) { layer.ShortName = ShortName + " (inverted)"; }

        //EntityType type = layer.EntityTypes.GetFirstValue();


        //type.DrawSettings.PolygonColor = Color.red;
        //type.DrawSettings.LineColor = Color.red; //new Color(153 / 255f, 0, 0, 1);
        //type.DrawSettings.DisplayPoints = false;
        //type.DrawSettings.DisplayPolygon = false;


        //layer.SubmitMetaData();

        //Rect layerBounds = GetLayerBounds();
        //layerBounds.position = layerBounds.position - layerBounds.size * 0.05f;
        //layerBounds.size = layerBounds.size * 1.1f;

        //PolygonEntity subjectEntity = l.CreateNewPolygonEntity(new Vector3(layerBounds.min.x, layerBounds.min.y), new List<EntityType>() { type }, planLayer);
        //PolygonSubEntity subjectSubEntity = subjectEntity.GetSubEntity(0) as PolygonSubEntity;
        //subjectSubEntity.AddPoint(new Vector3(layerBounds.min.x, layerBounds.max.y));
        //subjectSubEntity.AddPoint(new Vector3(layerBounds.max.x, layerBounds.max.y));
        //subjectSubEntity.AddPoint(new Vector3(layerBounds.max.x, layerBounds.min.y));

        //HashSet<PolygonEntity> clipEntities = new HashSet<PolygonEntity>(PolygonEntities);

        ////foreach (PolygonEntity clipEntity in clipEntities)
        ////{
        ////    PolygonEntity newEntity = SetOperations.BooleanP(subjectEntity, new HashSet<PolygonEntity> { clipEntity }, ClipperLib.ClipType.ctDifference);

        ////    List<PolygonSubEntity> subjectSubEntities = new List<PolygonSubEntity>(subjectEntity.GetSubEntities());
        ////    foreach (PolygonSubEntity subEntity in subjectSubEntities)
        ////    {
        ////        l.RemovePolygonSubEntity(subEntity);
        ////    }

        ////    subjectEntity = newEntity;
        ////}

        ////subjectEntity.DrawGameObjects(l.LayerGameObject.transform);

        //PolygonEntity newEntity = SetOperations.BooleanP(subjectEntity, clipEntities, ClipperLib.ClipType.ctDifference);

        //l.RemovePolygonSubEntity(subjectSubEntity, null);

        //newEntity.DrawGameObjects(l.LayerGameObject.transform);
    }
    #endregion
}