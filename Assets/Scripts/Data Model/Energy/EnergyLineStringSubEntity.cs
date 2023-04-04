using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EnergyLineStringSubEntity : LineStringSubEntity, IEnergyDataHolder
	{
		private const string NUMBER_CABLES_META_KEY = "NumberCables";
		public List<Connection> Connections { get; private set; }
		private int m_numberCables;
		
		public long UsedCapacity {
			get;
			set;
		}

		public long Capacity
		{
			get => m_entity.EntityTypes[0].capacity * m_numberCables;
			set { }
		}

		public EnergyGrid LastRunGrid {
			get;
			set;
		}

		public EnergyGrid CurrentGrid {
			get;
			set;
		}		

		public EnergyLineStringSubEntity(Entity a_entity) : base(a_entity)
		{
			Connections = new List<Connection>();
			CalculationPropertyUpdated();
		}

		public EnergyLineStringSubEntity(Entity a_entity, SubEntityObject a_geometry, int a_databaseID) : base(a_entity, a_geometry, a_databaseID)
		{
			Connections = new List<Connection>();
			PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
			CalculationPropertyUpdated();
		}

		protected override void SetDatabaseID(int a_databaseID)
		{
			PolicyLogicEnergy.Instance.RemoveEnergySubEntityReference(a_databaseID);
			m_databaseID = a_databaseID;
			PolicyLogicEnergy.Instance.AddEnergySubEntityReference(a_databaseID, this);
		}

		public override Action<BatchRequest> SubmitNew(BatchRequest a_batch)
		{
			base.SubmitNew(a_batch);
			return SubmitAddConnection;
		}

		public override Action<BatchRequest> SubmitUpdate(BatchRequest a_batch)
		{
			base.SubmitUpdate(a_batch);
			return SubmitUpdateConnection;
		}

		public override Action<BatchRequest> SubmitDelete(BatchRequest a_batch)
		{
			// Delete energy_output
			JObject dataObject = new JObject();
			dataObject.Add("id", m_databaseID);
			a_batch.AddRequest(Server.DeleteEnergyOutput(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_DELETE);

			//Delete cable
			dataObject = new JObject();
			dataObject.Add("cable", m_databaseID);
			a_batch.AddRequest(Server.DeleteEnergyConnection(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_DELETE);

			return base.SubmitDelete(a_batch);
		}

		void SubmitAddConnection(BatchRequest a_batch)
		{
			SubmitAddOrChangeConnection(Server.CreateConnection(), a_batch);
		}

		void SubmitUpdateConnection(BatchRequest a_batch)
		{
			SubmitAddOrChangeConnection(Server.UpdateConnection(), a_batch);
		}

		void SubmitAddOrChangeConnection(string a_endPoint, BatchRequest a_batch)
		{
			if (Connections == null || Connections.Count == 0)
			{
				Debug.LogError($"Trying to submit a cable with no connections. Cable ID: {GetDatabaseID()}");
			}
			else if (Connections.Count < 2)
			{
				Debug.LogError($"Trying to submit a cable with a missing connection. Cable ID: {GetDatabaseID()}. Existing connection to point with ID: {Connections[0].point.GetDatabaseID()}");
			}
			else
			{
				EnergyPointSubEntity first = null, second = null;
				foreach (Connection conn in Connections)
				{
					if (conn.connectedToFirst)
						first = conn.point;
					else
						second = conn.point;
				}

				Vector2 coordinate = first.GetPosition();

				JObject dataObject = new JObject();
				dataObject.Add("start", first.GetDataBaseOrBatchIDReference());
				dataObject.Add("end", second.GetDataBaseOrBatchIDReference());
				dataObject.Add("cable", GetDataBaseOrBatchIDReference());
				dataObject.Add("coords", $"[{coordinate.x},{coordinate.y}]");
				a_batch.AddRequest(a_endPoint, dataObject, BatchRequest.BATCH_GROUP_CONNECTIONS);
			}
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
			//Added connections are handled by the FSM, as they require all geom to have database or batch call IDs
		}

		public void RemoveConnection(Connection a_con)
		{
			Connections.Remove(a_con);
		}

		public void AddConnection(Connection a_newCon)
		{
			//Make sure we dont add connections multiple times
			foreach (Connection con in Connections)
				if (con.point == a_newCon.point)
				{
					return;
				}
			Connections.Add(a_newCon);
		}

		public override void ActivateConnections()
		{
			foreach (Connection con in Connections)
				con.point.AddConnection(con);
		}

		/// <summary>
		/// Called by entity before removal to clear all references to other subentities
		/// </summary>
		public override void RemoveDependencies()
		{
			//LayerManager.Instance.RemoveEnergySubEntityReference(databaseID);
			foreach (Connection con in Connections)
				con.point.RemoveConnection(con);
		}

		public override void RestoreDependencies()
		{
			PolicyLogicEnergy.Instance.AddEnergySubEntityReference(m_databaseID, this);
			foreach (Connection con in Connections)
				con.point.AddConnection(con);
		}

		public void SetPoint(Vector3 a_pos, bool a_firstPoint)
		{
			if (a_firstPoint)
				SetPointPosition(0, a_pos);
			else
				SetPointPosition(m_points.Count - 1, a_pos);
			//RedrawGameObject(SubEntityDrawMode.PlanReference);
			RedrawGameObject();
		}

		public void AddModifyLineUndoOperation(FSM a_fsm)
		{
			a_fsm.AddToUndoStack(new ModifyEnergyLineStringOperation(this, m_entity.PlanLayer, GetDataCopy(), UndoOperation.EditMode.Modify));
		}

		public override HashSet<int> GetPointsInBox(Vector3 a_min, Vector3 a_max)
		{
			HashSet<int> result = new HashSet<int>();

			//Ignores first and last point, as they cant be moved
			for (int i = 1; i < m_points.Count - 1; ++i)
			{
				Vector3 position = m_points[i];
				if (position.x >= a_min.x && position.x <= a_max.x && position.y >= a_min.y && position.y <= a_max.y)
				{
					result.Add(i);
				}
			}
			return result.Count > 0 ? result : null;
		}

		public Connection GetConnection(bool a_first)
		{
			foreach (Connection c in Connections)
				if (c.connectedToFirst == a_first)
					return c;
			return null;
		}

		//Move the cable so that the given endpoint lies on top of its matching point again
		//Might be called for both endpoints in one frame
		public void MoveWithEndPoint(bool a_firstPoint)
		{
			EnergyPointSubEntity point = GetConnection(a_firstPoint).point;
			Vector3 offset = point.GetPosition() - (a_firstPoint ? m_points[0] : m_points[m_points.Count - 1]);
			for (int i = 0; i < m_points.Count; i++)
				m_points[i] += offset;
			OnPointsDataChanged();
			//RedrawGameObject(SubEntityDrawMode.PlanReference);
			RedrawGameObject();
		}

		public void DuplicateCableToPlanLayer(PlanLayer a_cablePlanLayer, EnergyPointSubEntity a_newPoint, FSM a_fsm)
		{
			LineStringLayer cableBaseLayer = a_cablePlanLayer.BaseLayer as LineStringLayer;

			//Copy data
			SubEntityDataCopy dataCopy = GetDataCopy();

			//Create new entity
			LineStringEntity newEntity = cableBaseLayer.CreateNewLineStringEntity(dataCopy.m_entityTypeCopy, a_cablePlanLayer);
			EnergyLineStringSubEntity newCable = new EnergyLineStringSubEntity(newEntity);
			newCable.SetPersistentID(m_persistentID);
			(newCable.m_entity as LineStringEntity).AddSubEntity(newCable);
			newCable.SetDataToCopy(dataCopy);
			a_fsm.AddToUndoStack(new CreateEnergyLineStringOperation(newCable, a_cablePlanLayer, UndoOperation.EditMode.Modify, true));

			//Change active entities and (re)draw
			cableBaseLayer.m_activeEntities.Remove(m_entity as LineStringEntity);
			cableBaseLayer.PreModifiedEntities.Add(m_entity as LineStringEntity);
			cableBaseLayer.m_activeEntities.Add(newEntity);
			RedrawGameObject();
			newCable.DrawGameObject(cableBaseLayer.LayerGameObject.transform);

			//Replace connections to old cable with connections to new cable
			int pointID = a_newPoint.GetPersistentID();
			foreach (Connection con in Connections)
			{
				if (con.point.GetPersistentID() == pointID)//Connect to the new point
				{
					Connection newCon = new Connection(newCable, a_newPoint, con.connectedToFirst);
					con.point.RemoveConnection(con);
					newCable.AddConnection(newCon);
					a_newPoint.AddConnection(newCon);
				}
				else//Connect the the other point
				{
					Connection newCon = new Connection(newCable, con.point, con.connectedToFirst);
					a_fsm.AddToUndoStack(new ReconnectCableToPoint(con, newCon));
					con.point.RemoveConnection(con);
					con.point.AddConnection(newCon);
					newCable.AddConnection(newCon);
				}
			}
		}

		public override void CalculationPropertyUpdated()
		{
			EntityPropertyMetaData propertyMeta = m_entity.Layer.FindPropertyMetaDataByName(NUMBER_CABLES_META_KEY);
			int defaultValue = 1;
			if (propertyMeta != null)
			{
				defaultValue = Util.ParseToInt(propertyMeta.DefaultValue, 1);
			}

			if (m_entity.DoesPropertyExist(NUMBER_CABLES_META_KEY))
			{
				m_numberCables = Util.ParseToInt(m_entity.GetMetaData(NUMBER_CABLES_META_KEY), defaultValue);
			}
			else
				m_numberCables = defaultValue;
		}
	}
}
