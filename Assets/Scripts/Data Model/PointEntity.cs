using System.Collections.Generic;
using UnityEngine;

public class PointEntity : Entity
{
	List<PointSubEntity> pointSubEntities;

	public PointEntity(PointLayer layer, PlanLayer planLayer, Vector3 point, List<EntityType> entityType, EnergyPolygonSubEntity sourcepoly) : base(layer, entityType, sourcepoly == null ? planLayer.Plan.Country : sourcepoly.Entity.Country)
	{
		PlanLayer = planLayer;
		if (layer.IsEnergyPointLayer())
		{
			pointSubEntities = new List<PointSubEntity>() { new EnergyPointSubEntity(this, point, sourcepoly) };
			//if (layer.editingType == AbstractLayer.EditingType.Socket && LayerManager.EEZLayer != null)
			//{
			//    //Determine country based on the EEZ we were placed in
			//    List<Entity> eez = LayerManager.EEZLayer.GetEntitiesAt(point);
			//    if (eez != null && eez.Count > 0)
			//    {
			//        country = eez[0].country;
			//    }
			//    else
			//    {
			//        Debug.LogError("Socket placed outside of EEZs. Country was based on plan owner.");
			//    }
			//}
		}
		else
			pointSubEntities = new List<PointSubEntity>() { new PointSubEntity(this, point) };
	}

	public PointEntity(PointLayer layer, SubEntityObject layerObject) : base(layer, layerObject)
	{
		pointSubEntities = new List<PointSubEntity>();
		if (layer.IsEnergyPointLayer())
			for (int i = 0; i < layerObject.geometry.Count; i++)
				pointSubEntities.Add(new EnergyPointSubEntity(this, layerObject, layerObject.id));
		else
			for (int i = 0; i < layerObject.geometry.Count; i++)
				pointSubEntities.Add(new PointSubEntity(this, layerObject, layerObject.id));

	}

	public override SubEntity GetSubEntity(int index)
	{
		return pointSubEntities[index];
	}

	public PointSubEntity GetPointSubEntity()
	{
		return pointSubEntities[0];
	}

	public override int GetSubEntityCount()
	{
		return pointSubEntities.Count;
	}

	public List<PointSubEntity> GetSubEntities()
	{
		return pointSubEntities;
	}

	public override void RemoveGameObjects()
	{
		foreach (PointSubEntity pse in pointSubEntities)
		{
			pse.RemoveGameObject();
		}
		pointSubEntities = null;
	}

	public override void DrawGameObjects(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default)
	{
		foreach (PointSubEntity pse in pointSubEntities)
		{
			pse.DrawGameObject(parent, drawMode);
		}
	}

	public override void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, bool forceScaleUpdate = false)
	{
        if (forceScaleUpdate)
        {
            foreach (PointSubEntity pse in pointSubEntities)
            {
                pse.RedrawGameObject(drawMode);
                pse.UpdateScale(targetCamera);
            }
        }
        else
            foreach (PointSubEntity pse in pointSubEntities)
                pse.RedrawGameObject(drawMode);
    }

	public override float GetRestrictionAreaSurface()
	{
		return Mathf.Pow(Mathf.Max(GetCurrentRestrictionSize(), 0.25f), 2) * Mathf.PI;
	}

	//public override float GetInvestmentCost()
	//{
	//	return EntityTypes[0].investmentCost;
	//}
}
