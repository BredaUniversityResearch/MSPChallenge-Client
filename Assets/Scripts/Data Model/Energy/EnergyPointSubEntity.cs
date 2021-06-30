using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class EnergyPointSubEntity : PointSubEntity, IEnergyDataHolder
{
    public List<Connection> connections { get; private set; }
    public EnergyPolygonSubEntity sourcePolygon;
    public int gridID; //Only used if we are a socket or source
    long usedCapacity;
	EnergyGrid lastRunGrid;
	EnergyGrid currentGrid;

	public EnergyPointSubEntity(Entity entity, Vector3 position, EnergyPolygonSubEntity sourcepoly) : base(entity, position)
    {
        connections = new List<Connection>();
        sourcePolygon = sourcepoly;
    }

    public EnergyPointSubEntity(Entity entity, SubEntityObject geometry, int databaseID) : base(entity, geometry, databaseID)
    {
        if (Entity.Layer.editingType != AbstractLayer.EditingType.SourcePolygonPoint)
            LayerManager.AddEnergySubEntityReference(databaseID, this);
        connections = new List<Connection>();
    }

    public override void SetDatabaseID(int databaseID)
    {
        if (Entity.Layer.editingType != AbstractLayer.EditingType.SourcePolygonPoint)
            LayerManager.RemoveEnergySubEntityReference(databaseID);
        this.databaseID = databaseID;
        if (Entity.Layer.editingType != AbstractLayer.EditingType.SourcePolygonPoint)
            LayerManager.AddEnergySubEntityReference(databaseID, this);
    }

    /// <summary>
    /// Called on points being placed to check if they can be connected to a certain endpoint
    /// </summary>
    public bool CanConnectToEnergySubEntity(EnergyPointSubEntity cableOrigin)
    {
        //Sources can't connect to sources
        if ((Entity.Layer.editingType == AbstractLayer.EditingType.SourcePoint || Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint) &&
            (cableOrigin.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePoint || cableOrigin.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint))
            return false;

        //Points cannot connect to themselves
        if (cableOrigin == this)
            return false;

        //Green energy can't connect to grey
        if (Entity.GreenEnergy != cableOrigin.Entity.GreenEnergy)
            return false;

        return true;
    }

    /// <summary>
    /// Called on points to see if they can serve as the start point for a cable
    /// </summary>
    public bool CanCableStartAtSubEntity(bool greenCable)
    {
        //Sockets and generators can only have 1 connection
        //if ((Entity.Layer.editingType == AbstractLayer.EditingType.Socket || Entity.Layer.editingType == AbstractLayer.EditingType.SourcePoint || Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint) && connections.Count > 0)
        //    return false;

        //Green cables cant connect to grey energy and vice versa
        if (Entity.GreenEnergy != greenCable)
            return false;

        return true;
    }

    public override void RemoveDependencies()
    {
        ClearConnections();
    }

    public void RemoveConnection(Connection con)
    {
        connections.Remove(con);
    }

    public void AddConnection(Connection newCon)
    {
        //Make sure we dont add connections multiple times
        foreach (Connection con in connections)
            if (con.cable == newCon.cable)
            {
                //Debug.LogWarning("Duplicate connections added to point");
                return;
            }

        connections.Add(newCon);
    }

    public override void ClearConnections()
    {
        connections = new List<Connection>();
    }

    public override void RestoreDependencies()
    {
        if (Entity.Layer.editingType != AbstractLayer.EditingType.SourcePolygonPoint)
            LayerManager.AddEnergySubEntityReference(databaseID, this);
    }

    public override int GetDatabaseID()
    {
        if (Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
            return sourcePolygon.GetDatabaseID();
        return base.GetDatabaseID();
    }

	public override string GetDataBaseOrBatchIDReference()
	{
		if (Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
		{
			if (sourcePolygon.HasDatabaseID())
				return sourcePolygon.GetDatabaseID().ToString();
			else
				return BatchRequest.FormatCallIDReference(sourcePolygon.Entity.creationBatchCallID);
		}
		return base.GetDataBaseOrBatchIDReference();
	}

	public override int GetPersistentID()
    {
        if (Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
            return sourcePolygon.GetPersistentID();
        return base.GetPersistentID();
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

	public long Capacity
	{
		get
		{
			if (sourcePolygon != null)
				return sourcePolygon.Capacity;
			return Entity.EntityTypes[0].capacity;
		}
        set { }
	}

	public long UsedCapacity
	{
		get { return usedCapacity; }
		set { usedCapacity = value; }
	}

	public EnergyGrid LastRunGrid
	{
		get{ return lastRunGrid; }
		set{ lastRunGrid = value; }
	}

	public EnergyGrid CurrentGrid
	{
		get{ return currentGrid; }
		set{ currentGrid = value; }
	}
}

