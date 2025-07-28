using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	class EditEnergyPolygonState : EditPolygonsState
	{
		private PlanLayer m_cablePlanLayer;

		public EditEnergyPolygonState(FSM a_fsm, PlanLayer a_planLayer, HashSet<PolygonSubEntity> a_selectedSubEntities) : base(a_fsm, a_planLayer, a_selectedSubEntities)
		{
			m_cablePlanLayer = a_planLayer.BaseLayer.m_greenEnergy ? PolicyLogicEnergy.Instance.m_energyCableLayerGreen.CurrentPlanLayer() : PolicyLogicEnergy.Instance.m_energyCableLayerGrey.CurrentPlanLayer();
		}

		protected override void OnPolygonRemoved(SubEntity a_removedSubEntity)
		{
			base.OnPolygonRemoved(a_removedSubEntity);
			//Connected cables removed second so connections stay intact in cables

			EnergyPointSubEntity energyEnt = ((EnergyPolygonSubEntity)a_removedSubEntity).m_sourcePoint;
			List<Connection> removedConnections = new List<Connection>(energyEnt.Connections);
			foreach (Connection con in removedConnections)
			{
				m_fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(con.cable, m_cablePlanLayer, UndoOperation.EditMode.Modify, false, true));
				m_cablePlanLayer.BaseLayer.RemoveSubEntity(con.cable);
				con.cable.RemoveGameObject();
			}
		}

		protected override void OnPolygonModifiedViaRemoval(SubEntity a_modifiedSubEntity)
		{
			base.OnPolygonModifiedViaRemoval(a_modifiedSubEntity);
			EnergyPointSubEntity energyEnt = ((EnergyPolygonSubEntity)a_modifiedSubEntity).m_sourcePoint;
			List<Connection> removedConnections = new List<Connection>(energyEnt.Connections);
			foreach (Connection con in removedConnections)
			{
				//Undooperation depends on wether the cable was on the current planlayer
				if (con.cable.m_entity.PlanLayer == m_cablePlanLayer)
				{
					m_fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(con.cable, m_cablePlanLayer, UndoOperation.EditMode.Modify, false, true));
					con.cable.RemoveGameObject();
				}
				else
					m_fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(con.cable, m_cablePlanLayer, false));
				m_cablePlanLayer.BaseLayer.RemoveSubEntity(con.cable);
			}
			a_modifiedSubEntity.WarningIfDeletingExisting(
				"Energy Grid",
				"In plan '{0}' you have removed an energy polygon first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action.",
				m_planLayer.Plan
			);
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			base.Dragging(a_dragStartPosition, a_currentPosition);
			if (!m_draggingSelection)
				return;
			foreach (EnergyPointSubEntity energySubEnt in m_selectedSourcePoints)
			{
				Vector3 finalPos = energySubEnt.GetPosition();

				//Move attached cables
				foreach (Connection con in energySubEnt.Connections)
				{
					//Check if set contains the other endpoint of moved cables
					//Dont add undo operations for these moves
					if (m_selectedSourcePoints.Contains(con.cable.GetConnection(!con.connectedToFirst).point))
						con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)
					else
						con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
				}
			}
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			if (m_draggingSelection)
			{
				UpdateSelectionDragPositions(a_dragFinalPosition - a_dragStartPosition, true);

				foreach (EnergyPointSubEntity energySubEnt in m_selectedSourcePoints)
				{
					Vector3 finalPos = energySubEnt.GetPosition();

					//Move attached cables
					foreach (Connection con in energySubEnt.Connections)
					{
						//Check if set contains the other endpoint of moved cables
						//Dont add undo operations for these moves
						if (m_selectedSourcePoints.Contains(con.cable.GetConnection(!con.connectedToFirst).point))
							con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)
						else
							con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
					}
				}
			}

			base.StoppedDragging(a_dragStartPosition, a_dragFinalPosition);
		}

		protected override PolygonSubEntity StartModifyingSubEntity(PolygonSubEntity a_subEntity, bool a_insideUndoBatch)
		{			
			if (a_subEntity.m_entity.PlanLayer == m_planLayer)
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				m_fsm.AddToUndoStack(new ModifyPolygonOperation(a_subEntity, m_planLayer, a_subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
				a_subEntity.m_edited = true;

				//Create undo operations for cables attached to sourcepoint
				foreach (Connection con in (a_subEntity as EnergyPolygonSubEntity).m_sourcePoint.Connections)
				{
					con.cable.AddModifyLineUndoOperation(m_fsm);
					con.cable.m_edited = true;
				}

				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
			}
			else
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				//Duplicate polygon
				EnergyPolygonSubEntity oldEnergySubEnt = (a_subEntity as EnergyPolygonSubEntity);
				PolygonSubEntity duplicate = CreateNewPlanPolygon(a_subEntity.GetDataCopy(), a_subEntity.GetPersistentID());
				SwitchSelectionFromBasePolygonToDuplicate(a_subEntity, duplicate);

				//Duplicate all attached cables that were not part of the plan
				EnergyPointSubEntity newEnergySubEnt = (duplicate as EnergyPolygonSubEntity).m_sourcePoint;
				List<EnergyLineStringSubEntity> newCables = new List<EnergyLineStringSubEntity>();
				foreach (Connection con in oldEnergySubEnt.m_sourcePoint.Connections)
					if (con.cable.m_entity.PlanLayer != m_cablePlanLayer) //Make sure not to add 2 new cables if both endpoints are being edited
						newCables.Add(con.cable);
					else //Change the cables connection from the old point to the new
					{
						Connection newCon = new Connection(con.cable, newEnergySubEnt, con.connectedToFirst);
						con.cable.RemoveConnection(con);
						con.cable.AddConnection(newCon);
						newEnergySubEnt.AddConnection(newCon);
						con.cable.AddModifyLineUndoOperation(m_fsm);
						m_fsm.AddToUndoStack(new ReconnectCableToPoint(con, newCon));
					}
				foreach (EnergyLineStringSubEntity cable in newCables)
					cable.DuplicateCableToPlanLayer(m_cablePlanLayer, newEnergySubEnt, m_fsm);
				a_subEntity = duplicate;

				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
			}
			a_subEntity.WarningIfEditingExisting(
				"Energy Grid",
				"In plan '{0}' you have altered an energy polygon first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action."
			);
			return a_subEntity;
		}
	}
}
