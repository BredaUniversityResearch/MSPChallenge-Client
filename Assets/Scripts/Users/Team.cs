using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Team
{
    public Color color      { get; private set; }
    public int ID           { get; private set; }
    //public List<Entity> EEZ { get; private set; }
    public string name      { get; private set; }


    public Team(int ID, Color color, string name)
    {
        this.color = color;
        this.ID = ID;
        this.name = name;
    }

    public bool IsGameMaster
    { get { return ID == TeamManager.GM_ID; } }

    public bool IsAreaManager
    { get { return ID == TeamManager.AM_ID; } }

    public bool IsManager
    { get { return ID == TeamManager.AM_ID || ID == TeamManager.GM_ID; } }

    //public bool AssignEEZ()
    //{
    //    EEZ = new List<Entity>();
    //    PolygonLayer eezLayer = LayerManager.EEZLayer;

    //    if (eezLayer == null)
    //    {
    //        //Debug.Log("EEZ Layer not found!");
    //        return false;
    //    }

    //    foreach (PolygonEntity entity in eezLayer.Entities)
    //    {
    //        foreach (EntityType type in entity.EntityTypes)
    //        {
    //            int id = eezLayer.GetEntityTypeKey(type);
    //            if (id == ID)
    //            {
    //                if (!EEZ.Contains(entity))
    //                    EEZ.Add(entity);
    //            }
    //        }
    //    }

    //    return true;
    //}
}
