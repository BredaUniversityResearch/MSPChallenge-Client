using System;
using System.Collections.Generic;
using UnityEngine;

public class LineStringEntity : Entity
{
	List<LineStringSubEntity> lineStringSubEntities;

	public LineStringEntity(LineStringLayer layer, PlanLayer planLayer, List<EntityType> entityType) : base(layer, entityType, planLayer.Plan.Country)
	{
		PlanLayer = planLayer;
		lineStringSubEntities = new List<LineStringSubEntity>();
	}

	public LineStringEntity(LineStringLayer layer, SubEntityObject layerObject) : base(layer, layerObject)
	{
		lineStringSubEntities = new List<LineStringSubEntity>();
		//for (int i = 0; i < layerObject.geometry.Count; ++i)
		//{
		if (layer.editingType == AbstractLayer.EditingType.Cable)
			lineStringSubEntities.Add(new EnergyLineStringSubEntity(this, layerObject, layerObject.id));
		else
			lineStringSubEntities.Add(new LineStringSubEntity(this, layerObject, layerObject.id));
		//}
	}

	public override SubEntity GetSubEntity(int index)
	{
		return lineStringSubEntities[index];
	}

	public LineStringSubEntity GetLineStringSubEntity()
	{
		return lineStringSubEntities[0];
	}

	public override int GetSubEntityCount()
	{
		return lineStringSubEntities.Count;
	}

	public void AddSubEntity(LineStringSubEntity subEntity)
	{
		lineStringSubEntities.Add(subEntity);
	}

	public List<LineStringSubEntity> GetSubEntities()
	{
		return lineStringSubEntities;
	}

	public override void RemoveGameObjects()
	{
		foreach (LineStringSubEntity lsse in lineStringSubEntities)
		{
			lsse.RemoveGameObject();
		}
		lineStringSubEntities = null;
	}

	public override void DrawGameObjects(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default)
	{
		foreach (LineStringSubEntity lsse in lineStringSubEntities)
		{
			lsse.DrawGameObject(parent, drawMode);
		}
	}

	public override void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, bool forceScaleUpdate = false)
	{
        if (forceScaleUpdate)
        {
            foreach (LineStringSubEntity lsse in lineStringSubEntities)
            {
                lsse.RedrawGameObject(drawMode);
                lsse.UpdateScale(targetCamera);
            }
        }
        else
            foreach (LineStringSubEntity lsse in lineStringSubEntities)
                lsse.RedrawGameObject(drawMode);
    }

	public override float GetRestrictionAreaSurface()
	{
		if (lineStringSubEntities[0].m_restrictionArea)
			return InterfaceCanvas.Instance.mapScale.GetRealWorldPolygonAreaInSquareKm(lineStringSubEntities[0].m_restrictionArea.polygon);
		else
			return lineStringSubEntities[0].LineLengthKm * Mathf.Max(GetCurrentRestrictionSize(), 0.25f);
		//TODO: optimize this so it no longer uses the polygon area, but one only based on restriction size and length
	}

	//public override float GetInvestmentCost()
	//{
	//	return Util.GetLineStringLength(lineStringSubEntities[0].GetPoints()) * EntityTypes[0].investmentCost;
	//}
}
