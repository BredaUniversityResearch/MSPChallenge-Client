using System;
using System.Collections.Generic;
using UnityEngine;

public class LayerState
{
    public List<Entity> newGeometry; 
    public List<Entity> removedGeometry; //When geometry is updated, the old version is added to this list
    public List<Entity> baseGeometry;
    public int lastMergedPlanIndex;
    public AbstractLayer layer;
	public int lastMonthUpdated = -1;

    /// <summary>
    /// Creates a planstate, starting at the given state. 
    /// Both new- and removedgeometry will initially be null.
    /// </summary>
    public LayerState(List<Entity> baseGeometry, int lastMergedPlanIndex, AbstractLayer layer)
    {
        this.baseGeometry = baseGeometry;
        this.lastMergedPlanIndex = lastMergedPlanIndex;
        this.layer = layer;
    }

    /// <summary>
    /// Merges the given plan layer with the PlanState. 
    /// Should be called with a plan that has a planIndex 1 higher than lastMergedPlanIndex.
    /// </summary>
    private void AddPlanLayer(PlanLayer planLayer)
    {
        List<Entity> newBaseGeometry = new List<Entity>();
		int size = Math.Max(0, baseGeometry.Count + planLayer.GetNewGeometryCount() - planLayer.RemovedGeometry.Count);
		removedGeometry = new List<Entity>(size);
        HashSet<int> addedPersistentIDs = new HashSet<int>();

        //Takes over new geometry and keeps track of added persis IDs
        newGeometry = new List<Entity>(planLayer.GetNewGeometryCount());
		for (int entityIndex = 0; entityIndex < planLayer.GetNewGeometryCount(); ++entityIndex)
		{
			Entity entity = planLayer.GetNewGeometryByIndex(entityIndex);

			newGeometry.Add(entity);
            addedPersistentIDs.Add(entity.GetSubEntity(0).GetPersistentID());
        }

        foreach (Entity entity in baseGeometry)
        {
            //Check if geom was added or removed in new plan layer
            if (planLayer.RemovedGeometry.Contains(entity.GetSubEntity(0).GetPersistentID()) || addedPersistentIDs.Contains(entity.GetSubEntity(0).GetPersistentID()))
                removedGeometry.Add(entity);
            else
                newBaseGeometry.Add(entity);
        }       
        newBaseGeometry.AddRange(newGeometry);
        baseGeometry = newBaseGeometry;
    }

    public void AdvanceStateToMonth(int month)
    {
		lastMonthUpdated = month;
        while (true)
        {
            if (layer.planLayers.Count > lastMergedPlanIndex + 1)
            {
                PlanLayer newlayer = layer.planLayers[lastMergedPlanIndex + 1];
                if (newlayer.Plan.StartTime <= month)
                {
                    if (newlayer.Plan.InInfluencingState) //Plans in DESIGN and DELETES states are ignored
                        AddPlanLayer(newlayer);
                    lastMergedPlanIndex++;
                }
                else
                    return;
            }
            else
                return;
        }    
    }

	public Dictionary<int, List<DirectionalConnection>> GetCableNetworkForState()
	{
		if (!layer.IsEnergyLineLayer())
			return null;

		Dictionary<int, List<DirectionalConnection>> network = new Dictionary<int, List<DirectionalConnection>>();

		for (int i = 0; i < baseGeometry.Count; ++i)
		{
			EnergyLineStringSubEntity cable = (baseGeometry[i].GetSubEntity(0) as EnergyLineStringSubEntity);
			if (cable.connections == null || cable.connections.Count < 2)
			{
                Debug.LogWarning("Cable (ID: " + cable.GetDatabaseID().ToString() + ") doesn't have 2 connections when constructing network.");
				continue;
			}
			EnergyPointSubEntity p0 = cable.connections[0].point;
			EnergyPointSubEntity p1 = cable.connections[1].point;

			//Add connections from p0 to p1 and vive versa to network
			if (network.ContainsKey(p0.GetDatabaseID()))
				network[p0.GetDatabaseID()].Add(new DirectionalConnection(cable, p1));
			else
				network.Add(p0.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p1) });

			if (network.ContainsKey(p1.GetDatabaseID()))
				network[p1.GetDatabaseID()].Add(new DirectionalConnection(cable, p0));
			else
				network.Add(p1.GetDatabaseID(), new List<DirectionalConnection> { new DirectionalConnection(cable, p0) });
		}
		return network;
	}
}

