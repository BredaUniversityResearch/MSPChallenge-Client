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
				return m_entity.EntityTypes[0].capacity;
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
			if (m_entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
			Connections = new List<Connection>();
		}

		protected override void SetDatabaseID(int a_databaseID)
		{
			if (m_entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.RemoveEnergySubEntityReference(a_databaseID);
			m_databaseID = a_databaseID;
			if (m_entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
		}

		/// <summary>
		/// Called on points being placed to check if they can be connected to a certain endpoint
		/// </summary>
		public bool CanConnectToEnergySubEntity(EnergyPointSubEntity a_cableOrigin)
		{
			//Sources can't connect to sources
			if ((m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePoint || m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint) &&
			    (a_cableOrigin.m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePoint || a_cableOrigin.m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint))
				return false;

			//Points cannot connect to themselves
			if (a_cableOrigin == this)
				return false;

			//Green energy can't connect to grey
			return m_entity.GreenEnergy == a_cableOrigin.m_entity.GreenEnergy;
		}

		/// <summary>
		/// Called on points to see if they can serve as the start point for a cable
		/// </summary>
		public bool CanCableStartAtSubEntity(bool a_greenCable)
		{
			//Green cables cant connect to grey energy and vice versa
			return m_entity.GreenEnergy == a_greenCable;
		}

		public override void RemoveDependencies()
		{
			ClearConnections();
		}

		public void RemoveConnection(Connection a_con)
		{
			//Note: do not compare Connection directly, it is inconsistent
			//Connections.Remove(a_con) often fails when it shouldnt
			int i = 0;
			foreach (Connection con in Connections)
			{
				if (con.cable == a_con.cable)
				{
					break;
				}
				i++;
			}
			if(i >= Connections.Count)
			{
				Debug.LogError($"Trying to remove a connection that doesn't exist. Point id: {a_con.point.m_databaseID}, cable id:{a_con.cable.GetDatabaseID()}");
				return;
			}
			Connections.RemoveAt(i);
		}

		public void AddConnection(Connection a_newCon)
		{
			//Note: do not compare Connection directly, it is inconsistent
			//Make sure we dont add connections multiple times
			foreach (Connection con in Connections)
			{
				if (con.cable == a_newCon.cable)
				{
					//Debug.LogWarning("Duplicate connections added to point");
					return;
				}
			}

			Connections.Add(a_newCon);
		}

		public override void ClearConnections()
		{
			Connections = new List<Connection>();
		}

		public override void RestoreDependencies()
		{
			if (m_entity.Layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				PolicyLogicEnergy.Instance.AddEnergySubEntityReference(m_databaseID, this);
		}

		public override int GetDatabaseID()
		{
			if (m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
				return m_sourcePolygon.GetDatabaseID();
			return base.GetDatabaseID();
		}

		public override string GetDataBaseOrBatchIDReference()
		{
			if (m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
			{
				return m_sourcePolygon.GetDataBaseOrBatchIDReference();
			}
			return base.GetDataBaseOrBatchIDReference();
		}

		public override int GetPersistentID()
		{
			if (m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
				return m_sourcePolygon.GetPersistentID();
			return base.GetPersistentID();
		}

		public override Action<BatchRequest> SubmitDelete(BatchRequest a_batch)
		{
			// Delete energy_output
			JObject dataObject = new JObject();
			dataObject.Add("id", m_databaseID);
			a_batch.AddRequest(Server.DeleteEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_DELETE);
			return base.SubmitDelete(a_batch);
		}

		protected override void SubmitData(BatchRequest a_batch)
		{
			base.SubmitData(a_batch);

			//Set energy_output
			JObject dataObject = new JObject();
			dataObject.Add("id", GetDataBaseOrBatchIDReference());
			dataObject.Add("capacity", 0);
			dataObject.Add("maxcapacity", Capacity.ToString());
			a_batch.AddRequest(Server.SetEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
		}
	}
}
