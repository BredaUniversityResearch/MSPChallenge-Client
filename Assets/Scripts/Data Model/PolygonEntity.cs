using System;
using System.Collections.Generic;
using UnityEngine;

public class PolygonEntity : Entity
{
	List<PolygonSubEntity> polygonSubEntities;

	public PolygonEntity(PolygonLayer layer, PlanLayer planLayer, List<EntityType> entityType) : base(layer, entityType, planLayer.Plan.Country)
	{
		polygonSubEntities = new List<PolygonSubEntity>();
		PlanLayer = planLayer;
	}

	public PolygonEntity(PolygonLayer layer, SubEntityObject layerObject) : base(layer, layerObject)
	{
		polygonSubEntities = new List<PolygonSubEntity>();
		if (layer.editingType == AbstractLayer.EditingType.SourcePolygon)
			polygonSubEntities.Add(new EnergyPolygonSubEntity(this, layerObject, layerObject.id));
		else
			polygonSubEntities.Add(new PolygonSubEntity(this, layerObject, layerObject.id));
	}

	public override SubEntity GetSubEntity(int index)
	{
		return polygonSubEntities[index];
	}

	public PolygonSubEntity GetPolygonSubEntity()
	{
		return polygonSubEntities[0];
	}

	public override int GetSubEntityCount()
	{
		return polygonSubEntities.Count;
	}

	public void AddSubEntity(PolygonSubEntity subEntity)
	{
		polygonSubEntities.Add(subEntity);
	}

	public List<PolygonSubEntity> GetSubEntities()
	{
		return polygonSubEntities;
	}

	public override void RemoveGameObjects()
	{
		foreach (PolygonSubEntity pse in polygonSubEntities)
		{
			pse.RemoveGameObject();
		}
		polygonSubEntities = null;
	}

	public override void DrawGameObjects(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default)
	{
		foreach (PolygonSubEntity pse in polygonSubEntities)
		{
			pse.DrawGameObject(parent, drawMode);
		}
	}

	public override void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, bool forceScaleUpdate = false)
	{
        if (forceScaleUpdate)
        {
            foreach (PolygonSubEntity pse in polygonSubEntities)
            {
                pse.RedrawGameObject(drawMode);
                pse.UpdateScale(targetCamera);
            }
        }
        else
            foreach (PolygonSubEntity pse in polygonSubEntities)
                pse.RedrawGameObject(drawMode);
    }

	public void Rasterize(int drawValue, int[,] raster, Rect rasterBounds)
	{
		foreach (PolygonSubEntity subEntity in polygonSubEntities)
		{
			subEntity.Rasterize(drawValue, raster, rasterBounds);
		}
	}

	public void ValidateWindingOrders()
	{
		foreach (PolygonSubEntity subEntity in polygonSubEntities)
		{
			subEntity.ValidateWindingOrders();
		}
	}

	public override float GetRestrictionAreaSurface()
	{
		if (polygonSubEntities[0].restrictionArea != null)
			return InterfaceCanvas.Instance.mapScale.GetRealWorldPolygonAreaInSquareKm(polygonSubEntities[0].restrictionArea.polygon);
		return InterfaceCanvas.Instance.mapScale.GetRealWorldPolygonAreaInSquareKm(polygonSubEntities[0].GetPoints());
	}

	//public override float GetInvestmentCost()
	//{
	//	return Util.GetPolygonArea(polygonSubEntities[0].GetPoints()) * EntityTypes[0].investmentCost;
	//}
}