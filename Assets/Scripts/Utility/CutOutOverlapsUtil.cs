using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutOutOverlapsUtil
{
    public enum CompareType { Area, EntityType }

    public static void CutOutOverlaps(CompareType compareType = CompareType.Area, bool keepClip = false)
    {
        List<AbstractLayer> layers = LayerManager.GetVisibleLayersSortedByDepth();
        if (layers.Count != 1 || !(layers[0] is PolygonLayer))
        {
            Debug.Log("Please have a single polygon layer visible to perform overlap cutting");
        }
        else
        {
            CutOutOverlaps(layers[0] as PolygonLayer, compareType, keepClip);
        }
    }

    public static void CutOutOverlaps(PolygonLayer polygonLayer, CompareType compareType, bool keepClip)
    {
        //Debug.Log("Cutting out overlaps for layer " + polygonLayer.FileName);
        //Dictionary<PolygonEntity, HashSet<PolygonEntity>> subtract = new Dictionary<PolygonEntity, HashSet<PolygonEntity>>();

        //foreach (PolygonEntity entity in polygonLayer.PolygonEntities)
        //{
        //    int entityTypeID = entity.EntityTypeKey;
        //    if (entity.GetSubEntityCount() == 1)
        //    {
        //        PolygonSubEntity subEntity = entity.GetSubEntity(0) as PolygonSubEntity;
        //        float area = Util.GetPolygonArea(subEntity.GetPolygon());
        //        Vector3 point = Vector3.zero;
        //        if (subEntity.GetPolygonPointCount() > 0)
        //        {
        //            point = subEntity.GetPolygon()[0];
        //        }
        //        if (point == Vector3.zero) { continue; }

        //        List<Entity> otherEntities = polygonLayer.GetEntitiesAt(point);
        //        otherEntities.Remove(entity);

        //        foreach (Entity otherEntity in otherEntities)
        //        {
        //            int otherEntityTypeID = otherEntity.EntityTypeKey;
        //            if (otherEntityTypeID > entityTypeID || compareType != CompareType.EntityType)
        //            {
        //                PolygonEntity otherPolygonEntity = otherEntity as PolygonEntity;
        //                foreach (PolygonSubEntity otherSubEntity in otherPolygonEntity.GetSubEntities())
        //                {
        //                    float otherArea = Util.GetPolygonArea(otherSubEntity.GetPolygon());
        //                    if ((compareType != CompareType.Area || otherArea > area) && 
        //                        Util.PointInPolygon(point, otherSubEntity.GetPolygon(), otherSubEntity.GetHoles()))
        //                    {
        //                        if (!subtract.ContainsKey(otherPolygonEntity)) { subtract.Add(otherPolygonEntity, new HashSet<PolygonEntity>()); }

        //                        subtract[otherPolygonEntity].Add(entity);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //foreach (var kvp in subtract)
        //{
        //    PolygonEntity subject = kvp.Key;
        //    HashSet<PolygonEntity> clip = kvp.Value;

        //    PolygonEntity newEntity = SetOperations.BooleanP(subject, clip, ClipperLib.ClipType.ctDifference);
        //    if (newEntity != null)
        //    {
        //        foreach (SubEntity subEntity in newEntity.GetSubEntities())
        //        {
        //            subEntity.SubmitNew();
        //        }
        //        newEntity.DrawGameObjects(newEntity.Layer.LayerGameObject.transform);
        //    }

        //    removeEntity(subject);
        //    if (!keepClip)
        //    {
        //        foreach (PolygonEntity clipEntity in clip)
        //        {
        //            removeEntity(clipEntity);
        //        }
        //    }
        //}
    }

    private static void removeEntity(PolygonEntity entity)
    {
        //List<PolygonSubEntity> subEntities = new List<PolygonSubEntity>(entity.GetSubEntities());
        //foreach (PolygonSubEntity subEntity in subEntities)
        //{
        //    (entity.Layer as PolygonLayer).RemovePolygonSubEntity(subEntity);
        //    subEntity.RemoveGameObject();
        //    subEntity.SubmitDelete();
        //}
    }
}
