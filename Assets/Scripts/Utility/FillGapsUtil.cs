using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FillGapsUtil
{
    //public static void FillGaps()
    //{
    //    //List<Layer> layers = LayerManager.GetLoadedLayers();

    //    //if (layers.Count != 3) { displayRequirements(); return; }

    //    //PolygonLayer playAreaLayer = null;
    //    //PolygonLayer countriesLayer = null;
    //    //PolygonLayer bathymetryLayer = null;

    //    //foreach (Layer l in layers)
    //    //{
    //    //    if (l.ShortName == "PLAY_AREA") { playAreaLayer = l as PolygonLayer; }
    //    //    if (l.ShortName == "COUNTRIES") { countriesLayer = l as PolygonLayer; }
    //    //    if (l.ShortName == "BATHYMETRY") { bathymetryLayer = l as PolygonLayer; }
    //    //}

    //    //if (playAreaLayer == null || countriesLayer == null || bathymetryLayer == null) { displayRequirements(); return; }

    //    //LayerManager.AddNewLayer("NEW_BATHYMETRY", LayerManager.GeoType.polygon, (layer) => fillGaps(layer, playAreaLayer, countriesLayer, bathymetryLayer));
    //}

    //private static void displayRequirements()
    //{
    //    Debug.Log("Could not execute fill gaps code; needs the following three layers to be loaded: PLAY_AREA, COUNTRIES and BATHYMETRY");
    //}

    //private static void fillGaps(AbstractLayer newLayer, PolygonLayer playAreaLayer, PolygonLayer countriesLayer, PolygonLayer oldBathymetryLayer)
    //{
    ////    if (playAreaLayer.PolygonEntities.Count != 1)
    ////    {
    ////        Debug.Log("Could not execute fill gaps code: PLAY_AREA layer should only contain 1 polygon");
    ////    }

    ////    PolygonLayer newBathymetryLayer = newLayer as PolygonLayer;

    ////    cloneBathymetry(newBathymetryLayer, oldBathymetryLayer);

    ////    PolygonEntity playAreaEntity = playAreaLayer.GetEntity(0) as PolygonEntity;
    ////    HashSet<PolygonEntity> countryEntities = new HashSet<PolygonEntity>(countriesLayer.PolygonEntities);

    ////    PolygonEntity bathymetryArea = SetOperations.BooleanP(playAreaEntity, countryEntities, ClipperLib.ClipType.ctDifference);

    ////    List<Gap> gaps = calculateGaps(newBathymetryLayer, bathymetryArea);

    ////    clipBathymetry(newBathymetryLayer, bathymetryArea);
    ////    removeEntity(bathymetryArea);

    ////    mergeGapsIntoBathymetry(newBathymetryLayer, gaps);

    ////    newBathymetryLayer.DrawGameObjects();
    ////    submitNewBathymetry(newBathymetryLayer);

    ////    UIManager.CreateConfirmWindow("Done!", "Bathymetry fix complete.", 200, () => { });
    //}

    //private static void cloneBathymetry(PolygonLayer newBathymetryLayer, PolygonLayer oldBathymetryLayer)
    //{
    //    //newBathymetryLayer.EntityTypes = new Dictionary<int, EntityType>();// new List<EntityType>();

    //    ////foreach (EntityType et in oldBathymetryLayer.EntityTypes)
    //    //foreach (var kvp in oldBathymetryLayer.EntityTypes)
    //    //{
    //    //    newBathymetryLayer.EntityTypes.Add(kvp.Key, kvp.Value.GetClone());
    //    //}

    //    //foreach (PolygonEntity oldEntity in oldBathymetryLayer.PolygonEntities)
    //    //{
    //    //    PolygonEntity newEntity = newBathymetryLayer.CreateNewPolygonEntity(newBathymetryLayer.EntityTypes[oldEntity.EntityTypeKey]);

    //    //    foreach (PolygonSubEntity oldSubEntity in oldEntity.GetSubEntities())
    //    //    {
    //    //        List<Vector3> polygonCopy; List<List<Vector3>> holesCopy; EntityType entityTypeCopy;
    //    //        oldSubEntity.CopyDataValues(out polygonCopy, out holesCopy, out entityTypeCopy);

    //    //        PolygonSubEntity newSubEntity = newBathymetryLayer.editingType == Layer.EditingType.SourcePolygon ? new EnergyPolygonSubEntity(newEntity) : new PolygonSubEntity(newEntity);
    //    //        newSubEntity.SetDataToCopiedValues(polygonCopy, holesCopy, entityTypeCopy);
    //    //        newEntity.AddSubEntity(newSubEntity);
    //    //    }

    //    //    newEntity.ReplaceMetaData(oldEntity.GetMetaDataDictionary());
    //    //}
    //    //newBathymetryLayer.SubmitMetaData();
    //}

    private class Gap
    {
        public List<Vector3> Polygon;
        public List<List<Vector3>> Holes;

        public Gap(List<Vector3> polygon, List<List<Vector3>> holes)
        {
            this.Polygon = polygon;
            this.Holes = holes;
        }
    }

    //private static List<Gap> calculateGaps(PolygonLayer newBathymetryLayer, PolygonEntity bathymetryArea)
    //{
    //    HashSet<PolygonEntity> clipEntities = new HashSet<PolygonEntity>(newBathymetryLayer.Entities);

    //    PolygonEntity gapsEntity = SetOperations.BooleanP(newBathymetryLayer.Entities[0], new HashSet<PolygonEntity> { bathymetryArea }, ClipperLib.ClipType.ctUnion);
    //    PolygonEntity newEntity = SetOperations.BooleanP(gapsEntity, new HashSet<PolygonEntity> { bathymetryArea }, ClipperLib.ClipType.ctIntersection);
    //    //removeEntity(gapsEntity);
    //    gapsEntity = newEntity;

    //    newEntity = SetOperations.BooleanP(gapsEntity, clipEntities, ClipperLib.ClipType.ctDifference);
    //    //removeEntity(gapsEntity);
    //    gapsEntity = newEntity;

    //    List<Gap> gaps = new List<Gap>();
    //    foreach (PolygonSubEntity subEntity in gapsEntity.GetSubEntities())
    //    {
    //        List<Vector3> polygonCopy; List<List<Vector3>> holesCopy; List<EntityType> entityTypeCopy;
    //        subEntity.CopyDataValues(out polygonCopy, out holesCopy, out entityTypeCopy);
    //        gaps.Add(new Gap(polygonCopy, holesCopy));
    //    }

    //    //removeEntity(gapsEntity);

    //    return gaps;
    //}

    //private static void clipBathymetry(PolygonLayer newBathymetryLayer, PolygonEntity bathymetryArea)
    //{
    //    //// clip bathymetry with bathymetry area
    //
    //    //clipEntities(newBathymetryLayer.PolygonEntities, new HashSet<PolygonEntity> { bathymetryArea }, ClipperLib.ClipType.ctIntersection);
    //
    //    //// group bathymetry entities by type
    //
    //    //List<HashSet<PolygonEntity>> entitiesPerType = new List<HashSet<PolygonEntity>>();
    //    //for (int i = 0; i < newBathymetryLayer.EntityTypes.Count; ++i)
    //    //{
    //    //    entitiesPerType.Add(new HashSet<PolygonEntity>());
    //    //}
    //    //foreach (PolygonEntity entity in newBathymetryLayer.PolygonEntities)
    //    //{
    //
    //    //    entitiesPerType[entity.EntityTypeKey].Add(entity);
    //    //}
    //    //for (int i = entitiesPerType.Count - 1; i >= 0; --i)
    //    //{
    //    //    if (entitiesPerType[i].Count == 0) { entitiesPerType.RemoveAt(i); }
    //    //}
    //
    //    //// clip each entity with all entities that have types with lower indices
    //
    //    //HashSet<PolygonEntity> clip = entitiesPerType[0];
    //    //for (int i = 1; i < entitiesPerType.Count; ++i)
    //    //{
    //    //    clipEntities(entitiesPerType[i], clip, ClipperLib.ClipType.ctDifference);
    //    //    clip.UnionWith(entitiesPerType[i]);
    //    //}
    //}

    private static void clipEntities(IEnumerable<PolygonEntity> subjects, HashSet<PolygonEntity> clip, ClipperLib.ClipType clipType)
    {
        List<PolygonEntity> entities = new List<PolygonEntity>(subjects);
        foreach (PolygonEntity entity in entities)
        {
            SetOperations.BooleanP(entity, clip, clipType);

            //removeEntity(entity);
        }
    }

    //private static void removeEntity(PolygonEntity entity)
    //{
    //    //List<PolygonSubEntity> subEntities = new List<PolygonSubEntity>(entity.GetSubEntities());
    //    //foreach (PolygonSubEntity subEntity in subEntities)
    //    //{
    //    //    (entity.Layer as PolygonLayer).RemovePolygonSubEntity(subEntity);
    //    //}
    //}

    private static void mergeGapsIntoBathymetry(PolygonLayer newBathymetryLayer, List<Gap> gaps)
    {
        foreach (Gap gap in gaps)
        {
            Vector3 min = float.MaxValue * Vector3.one;
            Vector3 max = float.MinValue * Vector3.one;
            foreach (Vector3 position in gap.Polygon)
            {
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
            }

            // increase the search area a little bit
            min -= (max - min) * 0.1f;
            max += (max - min) * 0.1f;

            HashSet<PolygonSubEntity> subEntities = newBathymetryLayer.GetSubEntitiesInBox(min, max);

            Dictionary<PolygonEntity, int> entityOverlap = getEntityOverlap(gap, subEntities);

            //if (entityOverlap.Count == 0)
            //{
            //    Debug.Log("no entity overlap :(");
            //}
            //else
            //{
            //    string overlaps = "";
            //    foreach (var kvp in entityOverlap)
            //    {
            //        if (overlaps != "")
            //        {
            //            overlaps += ", ";
            //        }
            //        overlaps += kvp.Value;
            //    }
            //    Debug.Log("overlaps: " + overlaps);
            //}

            PolygonEntity closestEntity = null;
            int closestEntityOverlaps = -1;
            foreach (var kvp in entityOverlap)
            {
                if (kvp.Value > closestEntityOverlaps)
                {
                    closestEntityOverlaps = kvp.Value;
                    closestEntity = kvp.Key;
                }
            }

            if (closestEntity != null)
            {
                mergeGapWithEntity(newBathymetryLayer, closestEntity, gap);
            }
        }
    }

    private static void mergeGapWithEntity(PolygonLayer newBathymetryLayer, PolygonEntity entity, Gap gap)
    {
        //PolygonEntity gapEntity = newBathymetryLayer.CreateNewPolygonEntity(newBathymetryLayer.EntityTypes[entity.EntityTypeKey]);
        //PolygonSubEntity gapSubEntity = newBathymetryLayer.editingType == Layer.EditingType.SourcePolygon ? new EnergyPolygonSubEntity(entity) : new PolygonSubEntity(entity);
        //gapSubEntity.SetDataToCopiedValues(gap.Polygon, gap.Holes, gapEntity.EntityType);
        //gapEntity.AddSubEntity(gapSubEntity);

        //PolygonEntity mergedEntity = SetOperations.BooleanP(entity, new HashSet<PolygonEntity> { gapEntity }, ClipperLib.ClipType.ctUnion);
        //removeEntity(entity);
        //removeEntity(gapEntity);
    }

    private static Dictionary<PolygonEntity, int> getEntityOverlap(Gap gap, HashSet<PolygonSubEntity> subEntities)
    {
        Dictionary<PolygonEntity, int> entityOverlap = new Dictionary<PolygonEntity, int>();

        foreach (Vector3 gapPoint in gap.Polygon)
        {
            foreach (PolygonSubEntity subEntity in subEntities)
            {
                foreach (Vector3 subEntityPoint in subEntity.GetPoints())
                {
                    if (subEntityPoint == gapPoint)
                    {
                        incrementEntityOverlap(entityOverlap, subEntity.Entity as PolygonEntity);
                    }
                }
                if (subEntity.GetHoleCount() > 0)
                {
                    foreach (List<Vector3> hole in subEntity.GetHoles())
                    {
                        foreach (Vector3 holePoint in hole)
                        {
                            if (holePoint == gapPoint)
                            {
                                incrementEntityOverlap(entityOverlap, subEntity.Entity as PolygonEntity);
                            }
                        }
                    }
                }
            }
        }

        //if (entityOverlap.Count > 0) { return entityOverlap; }

        //foreach (Vector3 gapPoint in gap.Polygon)
        //{
        //    float closestDistSq = float.MaxValue;
        //    PolygonEntity closestEntity = null;

        //    foreach (PolygonSubEntity subEntity in subEntities)
        //    {
        //        Vector3 closestPoint = subEntity.GetPointClosestTo(gapPoint);
        //        float distSq = (closestPoint - gapPoint).sqrMagnitude;
        //        if (distSq < closestDistSq)
        //        {
        //            closestDistSq = distSq;
        //            closestEntity = subEntity.Entity as PolygonEntity;
        //        }
        //    }

        //    incrementEntityOverlap(entityOverlap, closestEntity);
        //}

        return entityOverlap;
    }

    private static void incrementEntityOverlap(Dictionary<PolygonEntity, int> entityOverlap, PolygonEntity entity)
    {
        if (!entityOverlap.ContainsKey(entity))
        {
            entityOverlap.Add(entity, 0);
        }
        entityOverlap[entity]++;
    }

    private static void submitNewBathymetry(PolygonLayer newBathymetryLayer)
    {
        //foreach (PolygonEntity entity in newBathymetryLayer.PolygonEntities)
        //{
        //    foreach (SubEntity subEntity in entity.GetSubEntities())
        //    {
        //        subEntity.SubmitNew();
        //        System.Threading.Thread.Sleep(10);
        //    }
        //}
    }
}
