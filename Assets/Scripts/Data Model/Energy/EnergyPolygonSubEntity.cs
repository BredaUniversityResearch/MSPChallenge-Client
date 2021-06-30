using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class EnergyPolygonSubEntity : PolygonSubEntity, IEnergyDataHolder
{
	public EnergyPointSubEntity sourcePoint;
    public long cachedMaxCapacity;

	public EnergyPolygonSubEntity(Entity entity, int persistentID = -1)
		: base(entity, persistentID)
	{
		CreateSourcePoint();
	}

	public EnergyPolygonSubEntity(Entity entity, SubEntityObject geometry, int databaseID)
		: base(entity, geometry, databaseID)
	{
		//Base calls initialise
		LayerManager.AddEnergySubEntityReference(databaseID, this);
	}
	public override void Initialise()
	{
		CreateSourcePoint();
		base.Initialise();
	}

	public override void SetDatabaseID(int databaseID)
	{
		LayerManager.RemoveEnergySubEntityReference(databaseID);
		this.databaseID = databaseID;
		LayerManager.AddEnergySubEntityReference(databaseID, this);
	}

	public override void SubmitNew(BatchRequest batch)
	{
		base.SubmitNew(batch);
	}

	public override void SubmitDelete(BatchRequest batch)
	{
		base.SubmitDelete(batch);

		// Delete energy_output
		JObject dataObject = new JObject();
		dataObject.Add("id", databaseID);
		batch.AddRequest(Server.DeleteEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_DELETE);
	}
	
	public override void SubmitData(BatchRequest batch)
	{
		base.SubmitData(batch);

		//Set energy_output
		JObject dataObject = new JObject();
		dataObject.Add("id", GetDataBaseOrBatchIDReference());
		dataObject.Add("capacity", 0);
		dataObject.Add("maxcapacity", Capacity.ToString());
		batch.AddRequest(Server.SetEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
	}
	
	protected override void UpdateBoundingBox()
	{
		base.UpdateBoundingBox();

        Vector3 center = Util.Compute2DPolygonCentroidAlt(polygon);
        sourcePoint.SetPosition(BoundingBox.center);
	}

	public override void RemoveDependencies()
	{
		//Remove sourcePoint
		if (sourcePoint != null)
		{
			PointLayer centerPointLayer = ((EnergyPolygonLayer)Entity.Layer).centerPointLayer;
			PointEntity sourceEntity = sourcePoint.Entity as PointEntity;
			centerPointLayer.activeEntities.Remove(sourceEntity);
		}
	}

	public override void RestoreDependencies()
	{
		LayerManager.AddEnergySubEntityReference(databaseID, this);

		//Restore sourcePoint
		if (sourcePoint != null)
		{
			PointLayer centerPointLayer = ((EnergyPolygonLayer)Entity.Layer).centerPointLayer;
			PointEntity sourceEntity = sourcePoint.Entity as PointEntity;
			centerPointLayer.activeEntities.Add(sourceEntity);
            sourcePoint.SetPosition(BoundingBox.center);
			sourcePoint.RedrawGameObject(); //Redraws to new position
		}
	}

	public void CreateSourcePoint()
	{
		if (sourcePoint == null)
		{
			PointLayer centerPointLayer = ((EnergyPolygonLayer)Entity.Layer).centerPointLayer;
			PointEntity ent = new PointEntity(centerPointLayer, null, Vector3.zero, new List<EntityType>() { centerPointLayer.EntityTypes[0] }, this);
			sourcePoint = ent.GetSubEntity(0) as EnergyPointSubEntity;
		}
	}

    public override void ClearConnections()
    {
        sourcePoint.ClearConnections();
    }

    public override void UpdateScale(Camera targetCamera)
	{
		base.UpdateScale(targetCamera);
		sourcePoint.UpdateScale(targetCamera);
	}

	public void DeactivateSourcePoint()
	{
		((EnergyPolygonLayer)Entity.Layer).centerPointLayer.activeEntities.Remove(sourcePoint.Entity as PointEntity);
		sourcePoint.RedrawGameObject();
	}

	public override void DrawGameObject(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null)
	{
		PointLayer centerPointLayer = (PointLayer)sourcePoint.Entity.Layer;
		PointEntity centerPoint = (PointEntity)sourcePoint.Entity;
		centerPointLayer.Entities.Add(centerPoint);
		centerPointLayer.activeEntities.Add(centerPoint);
		sourcePoint.DrawGameObject(centerPointLayer.LayerGameObject.transform);
		base.DrawGameObject(parent, drawMode, selectedPoints, hoverPoints);
	}

	public override void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
	{
		base.RedrawGameObject(drawMode, selectedPoints, hoverPoints, updatePlanState);

		GameObject sourcePointObject = sourcePoint.GetGameObject();
		if (sourcePointObject != null)
		{
            sourcePoint.SetPlanState(planState);

            //Redraw sourcepoints without updating its plan state
			sourcePoint.RedrawGameObject(SubEntityDrawMode.Default, null, null, false);
		}
	}

    public override void RemoveGameObject()
    {
        base.RemoveGameObject();
        if (sourcePoint != null)
        {
            ((PointLayer)sourcePoint.Entity.Layer).Entities.Remove((PointEntity)sourcePoint.Entity);
            sourcePoint.RemoveGameObject();
        }
    }

    public override void ForceGameObjectVisibility(bool value)
    {
        base.ForceGameObjectVisibility(value);
        sourcePoint.SetPlanState(value ? SubEntityPlanState.NotInPlan : SubEntityPlanState.NotShown);
        sourcePoint.ForceGameObjectVisibility(value);
    }

    public long Capacity
	{
		get
		{
            if (edited)
                return (long)((double)Entity.EntityTypes[0].capacity * (double)SurfaceAreaSqrKm);
            else
                return cachedMaxCapacity;
		}
        set
        {
            cachedMaxCapacity = value;
        }
	}

	public long UsedCapacity
	{
		get { return sourcePoint.UsedCapacity; }
		set { sourcePoint.UsedCapacity = value; }
	}

	public EnergyGrid LastRunGrid
	{
		get { return sourcePoint.LastRunGrid; }
		set { sourcePoint.LastRunGrid = value; }
	}

	public EnergyGrid CurrentGrid
	{
		get { return sourcePoint.CurrentGrid; }
		set { sourcePoint.CurrentGrid = value; }
	}

    public override void FinishEditing()
    {
        if (edited)
            cachedMaxCapacity = Capacity;
        base.FinishEditing();
    }
    
    public override Dictionary<string, object> GetGeoJSONProperties()
    {
        Dictionary<string, object> properties = new Dictionary<string, object>();
        properties.Add("type", "windfarm");
        return properties;
    }
}

