using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EnergyPointSubEntity : PointSubEntity, IEnergyDataHolder
	{
		public List<Connection> Connections { get; private set; }
		public EnergyPolygonSubEntity m_sourcePolygon;
		public int m_gridID; //Only used if we are a socket or source
		
		public long Capacity
		{
			get
			{
				if (m_sourcePolygon != null)
					return m_sourcePolygon.Capacity;
				return Entity.EntityTypes[0].capacity;
			}
			set { }
		}

		public long UsedCapacity {
			get;
			set;
		}

		public EnergyGrid LastRunGrid {
			get;
			set;
		}

		public EnergyGrid CurrentGrid {
			get;
			set;
		}		

		public EnergyPointSubEntity(Entity a_entity, Vector3 a_position, EnergyPolygonSubEntity a_sourcepoly) : base(a_entity, a_position)
		{
			Connections = new List<Connection>();
			m_sourcePolygon = a_sourcepoly;
		}

		public EnergyPointSubEntity(Entity a_entity, SubEntityObject a_geometry, int a_databaseID) : base(a_entity, a_geometry, a_databaseID)
		{
			if (Entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
			Connections = new List<Connection>();
		}

		public override void SetDatabaseID(int a_databaseID)
		{
			if (Entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.RemoveEnergySubEntityReference(a_databaseID);
			databaseID = a_databaseID;
			if (Entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
		}

		/// <summary>
		/// Called on points being placed to check if they can be connected to a certain endpoint
		/// </summary>
		public bool CanConnectToEnergySubEntity(EnergyPointSubEntity a_cableOrigin)
		{
			//Sources can't connect to sources
			if ((Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePoint || Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint) &&
			    (a_cableOrigin.Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePoint || a_cableOrigin.Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint))
				return false;

			//Points cannot connect to themselves
			if (a_cableOrigin == this)
				return false;

			//Green energy can't connect to grey
			return Entity.GreenEnergy == a_cableOrigin.Entity.GreenEnergy;
		}

		/// <summary>
		/// Called on points to see if they can serve as the start point for a cable
		/// </summary>
		public bool CanCableStartAtSubEntity(bool a_greenCable)
		{
			//Green cables cant connect to grey energy and vice versa
			return Entity.GreenEnergy == a_greenCable;
		}

		public override void RemoveDependencies()
		{
			ClearConnections();
		}

		public void RemoveConnection(Connection a_con)
		{
			Connections.Remove(a_con);
		}

		public void AddConnection(Connection a_newCon)
		{
			//Make sure we dont add connections multiple times
			foreach (Connection con in Connections)
				if (con.cable == a_newCon.cable)
				{
					//Debug.LogWarning("Duplicate connections added to point");
					return;
				}

			Connections.Add(a_newCon);
		}

		public override void ClearConnections()
		{
			Connections = new List<Connection>();
		}

		public override void RestoreDependencies()
		{
			if (Entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.AddEnergySubEntityReference(databaseID, this);
		}

		public override int GetDatabaseID()
		{
			if (Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
				return m_sourcePolygon.GetDatabaseID();
			return base.GetDatabaseID();
		}

		public override string GetDataBaseOrBatchIDReference()
		{
			if (Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
			{
				return m_sourcePolygon.GetDataBaseOrBatchIDReference();
			}
			return base.GetDataBaseOrBatchIDReference();
		}

		public override int GetPersistentID()
		{
			if (Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
				return m_sourcePolygon.GetPersistentID();
			return base.GetPersistentID();
		}

		public override Action<BatchRequest> SubmitDelete(BatchRequest a_batch)
		{
			// Delete energy_output
			JObject dataObject = new JObject();
			dataObject.Add("id", databaseID);
			a_batch.AddRequest(Server.DeleteEnergyOutput(), dataObject, BatchRequest.BatchGroupEnergyDelete);
			return base.SubmitDelete(a_batch);
		}
	
		public override void SubmitData(BatchRequest a_batch)
		{
			base.SubmitData(a_batch);

			//Set energy_output
			JObject dataObject = new JObject();
			dataObject.Add("id", GetDataBaseOrBatchIDReference());
			dataObject.Add("capacity", 0);
			dataObject.Add("maxcapacity", Capacity.ToString());
			a_batch.AddRequest(Server.SetEnergyOutput(), dataObject, BatchRequest.BatchGroupGeometryData);
		}
	}
}
