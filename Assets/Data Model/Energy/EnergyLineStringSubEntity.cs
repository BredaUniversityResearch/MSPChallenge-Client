using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class EnergyLineStringSubEntity : LineStringSubEntity, IEnergyDataHolder
{
	public const string NUMBER_CABLES_META_KEY = "NumberCables";
    public List<Connection> connections { get; private set; }
	long usedCapacity;
	EnergyGrid lastRunGrid;
	EnergyGrid currentGrid;
    private int numberCables;

	public EnergyLineStringSubEntity(Entity entity) : base(entity)
    {
        connections = new List<Connection>();
        CalculationPropertyUpdated();
    }

    public EnergyLineStringSubEntity(Entity entity, SubEntityObject geometry, int databaseID) : base(entity, geometry, databaseID)
    {
        connections = new List<Connection>();
        LayerManager.AddEnergySubEntityReference(databaseID, this);
        CalculationPropertyUpdated();
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

		//Delete cable
		dataObject = new JObject();
		dataObject.Add("cable", databaseID);
		batch.AddRequest(Server.DeleteEnergyConection(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_DELETE);
	}

	public override void SubmitData(BatchRequest batch)
	{
		base.SubmitData(batch);

		//Set energy_output
		JObject dataObject = new JObject();
		dataObject.Add("id", GetDataBaseOrBatchIDReference());
		dataObject.Add("maxcapacity", Capacity.ToString());
		batch.AddRequest(Server.AddEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
        //Added connections are handled by the FSM, as they require all geom to have database or batch call IDs
	}

    public void RemoveConnection(Connection con)
    {
        connections.Remove(con);
    }

    public void AddConnection(Connection newCon)
    {
        //Make sure we dont add connections multiple times
        foreach (Connection con in connections)
            if (con.point == newCon.point)
            { 
                //Debug.Log("Duplicate connections added to cable");
                return;
            }

        connections.Add(newCon);
    }

    public void SetEndPointsToConnections()
    {
        //if (connections == null || connections.Count < 2 || m_points == null || m_points.Count < 2)
        //{
        //    Debug.LogError("Couldn't set end positions for cable while it should be possible");
        //    return;
        //}

        //if (connections[0].connectedToFirst)
        //{
        //    m_points[0] = connections[0].point.GetPosition();
        //    m_points[m_points.Count - 1] = connections[1].point.GetPosition();
        //}
        //else
        //{
        //    m_points[0] = connections[1].point.GetPosition();
        //    m_points[m_points.Count - 1] = connections[0].point.GetPosition();
        //}
        //OnPointsDataChanged();
        //RedrawGameObject();
    }

    public override void ActivateConnections()
    {
        foreach (Connection con in connections)
            con.point.AddConnection(con);
    }

    /// <summary>
    /// Called by entity before removal to clear all references to other subentities
    /// </summary>
    public override void RemoveDependencies()
    {
        //LayerManager.RemoveEnergySubEntityReference(databaseID);
        foreach (Connection con in connections)
            con.point.RemoveConnection(con);
    }

    public override void RestoreDependencies()
    {
        LayerManager.AddEnergySubEntityReference(databaseID, this);
        foreach (Connection con in connections)
            con.point.AddConnection(con);
    }

    public Vector3 GetFirstPoint()
    {
        return m_points[0];
    }

    public void SetPoint(Vector3 pos, bool firstPoint)
    {
        if (firstPoint)
            SetPointPosition(0, pos);
        else
            SetPointPosition(m_points.Count - 1, pos);
        //RedrawGameObject(SubEntityDrawMode.PlanReference);
        RedrawGameObject();
    }

    public void AddModifyLineUndoOperation(FSM fsm)
    {
        fsm.AddToUndoStack(new ModifyEnergyLineStringOperation(this, Entity.PlanLayer, GetDataCopy(), UndoOperation.EditMode.Modify));
    }

    public HashSet<int> GetLastPointIndex()
    {
        return new HashSet<int> { m_points.Count - 1 };
    }

    public override HashSet<int> GetPointsInBox(Vector3 min, Vector3 max)
    {
        HashSet<int> result = new HashSet<int>();

        //Ignores first and last point, as they cant be moved
        for (int i = 1; i < m_points.Count - 1; ++i)
        {
            Vector3 position = m_points[i];
            if (position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y)
            {
                result.Add(i);
            }
        }
        return result.Count > 0 ? result : null;
    }

    public Connection GetConnection(bool first)
    {
        foreach (Connection c in connections)
            if (c.connectedToFirst == first)
                return c;
        return null;
    }

    //Move the cable so that the given endpoint lies on top of its matching point again
    //Might be called for both endpoints in one frame
    public void MoveWithEndPoint(bool firstPoint)
    {
        EnergyPointSubEntity point = GetConnection(firstPoint).point;
        Vector3 offset = point.GetPosition() - (firstPoint ? m_points[0] : m_points[m_points.Count - 1]);
        for (int i = 0; i < m_points.Count; i++)
            m_points[i] += offset;
        OnPointsDataChanged();
        //RedrawGameObject(SubEntityDrawMode.PlanReference);
        RedrawGameObject();
    }

    public bool FirstPointAt(Vector3 position)
    {
        return (Mathf.Approximately(m_points[0].x, position.x) && Mathf.Approximately(m_points[0].y, position.y));
    }

    public Vector3 GetPointAtEnd(bool firstPoint)
    {
        return firstPoint ? m_points[0] : m_points[m_points.Count - 1];
    }

    public void DuplicateCableToPlanLayer(PlanLayer cablePlanLayer, EnergyPointSubEntity newPoint, FSM fsm)
    {
        LineStringLayer cableBaseLayer = cablePlanLayer.BaseLayer as LineStringLayer;

		//Copy data
		SubEntityDataCopy dataCopy = GetDataCopy();

        //Create new entity
        LineStringEntity newEntity = cableBaseLayer.CreateNewLineStringEntity(dataCopy.entityTypeCopy, cablePlanLayer);
        EnergyLineStringSubEntity newCable = new EnergyLineStringSubEntity(newEntity);
        newCable.SetPersistentID(persistentID);
        (newCable.Entity as LineStringEntity).AddSubEntity(newCable);
		newCable.SetDataToCopy(dataCopy);
        fsm.AddToUndoStack(new CreateEnergyLineStringOperation(newCable, cablePlanLayer, UndoOperation.EditMode.Modify, true));

        //Change active entities and (re)draw
        cableBaseLayer.activeEntities.Remove(Entity as LineStringEntity);
        cableBaseLayer.preModifiedEntities.Add(Entity as LineStringEntity);
        cableBaseLayer.activeEntities.Add(newEntity);
        RedrawGameObject();
        newCable.DrawGameObject(cableBaseLayer.LayerGameObject.transform);

        //Replace connections to old cable with connections to new cable
        int pointID = newPoint.GetPersistentID();
        foreach (Connection con in connections)
        {
            if (con.point.GetPersistentID() == pointID)//Connect to the new point
            {
                Connection newCon = new Connection(newCable, newPoint, con.connectedToFirst);
                con.point.RemoveConnection(con);
                newCable.AddConnection(newCon);
                newPoint.AddConnection(newCon);
            }
            else//Connect the the other point
            {
                Connection newCon = new Connection(newCable, con.point, con.connectedToFirst);
                fsm.AddToUndoStack(new ReconnectCableToPoint(con, newCon));
                con.point.RemoveConnection(con);
                con.point.AddConnection(newCon);
                newCable.AddConnection(newCon);
            }
        }
    }

    public override void CalculationPropertyUpdated()
    {
        EntityPropertyMetaData propertyMeta = Entity.Layer.FindPropertyMetaDataByName(NUMBER_CABLES_META_KEY);
        int defaultValue = 1;
        if (propertyMeta != null)
        {
            defaultValue = Util.ParseToInt(propertyMeta.DefaultValue, 1);
        }

        if (Entity.DoesPropertyExist(NUMBER_CABLES_META_KEY))
        {
            numberCables = Util.ParseToInt(Entity.GetMetaData(NUMBER_CABLES_META_KEY), defaultValue);
        }
        else
            numberCables = defaultValue;
    }

    public long UsedCapacity
	{
		get { return usedCapacity; }
		set { usedCapacity = value; }
	}

	public long Capacity
	{
		get { return Entity.EntityTypes[0].capacity * numberCables; }
        set { }
	}

	public EnergyGrid LastRunGrid
	{
		get { return lastRunGrid; }
		set { lastRunGrid = value; }
	}

	public EnergyGrid CurrentGrid
	{
		get { return currentGrid; }
		set { currentGrid = value; }
	}
}

