using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	class EditEnergyPointsState : EditPointsState
	{
		private PlanLayer m_cablePlanLayer;

		public EditEnergyPointsState(FSM a_fsm, PlanLayer a_planLayer, HashSet<PointSubEntity> a_selectedSubEntities = null) : base(a_fsm, a_planLayer, a_selectedSubEntities)
		{
			m_cablePlanLayer = a_planLayer.BaseLayer.m_greenEnergy ? PolicyLogicEnergy.Instance.m_energyCableLayerGreen.CurrentPlanLayer() : PolicyLogicEnergy.Instance.m_energyCableLayerGrey.CurrentPlanLayer();
		}

		protected override void DeleteSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PointSubEntity subEntity in m_selection)
			{
				EnergyPointSubEntity energyEnt = subEntity as EnergyPointSubEntity;
				if (subEntity.m_entity.PlanLayer == m_planLayer)
				{
					//Point undo added first so it is undone last
					m_fsm.AddToUndoStack(new RemoveEnergyPointOperation(subEntity, m_planLayer));
                
					//Connected cables removed second so connections stay intact in cables
					List<Connection> removedConnections = new List<Connection>(energyEnt.Connections);
					foreach (Connection con in removedConnections)
					{
						m_fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(con.cable, m_cablePlanLayer, UndoOperation.EditMode.Modify, false, true));
						m_cablePlanLayer.BaseLayer.RemoveSubEntity(con.cable);
						con.cable.RemoveGameObject();
					}

					//Point itself removed last
					m_baseLayer.RemoveSubEntity(subEntity);
					subEntity.RemoveGameObject();
				}
				else
				{
					//Point undo added first
					m_fsm.AddToUndoStack(new ModifyPointRemovalPlanOperation(subEntity, m_planLayer, false));

					//Cables removed second
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

					//Point itself removed last
					m_baseLayer.RemoveSubEntity(subEntity);
				}
			}
			m_selection = new HashSet<PointSubEntity>();

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_selectedRemovedEntity)
				return;

			if (m_draggingSelection)
			{
				Vector3 offset = a_currentPosition - a_dragStartPosition;

				foreach (PointSubEntity subEntity in m_selection)
				{
					Vector3 finalPos = m_selectionDragStart[subEntity] + offset;
					subEntity.SetPosition(finalPos);

					//Move attached cables
					EnergyPointSubEntity energyEnt = subEntity as EnergyPointSubEntity;
					foreach (Connection con in energyEnt.Connections)
					{
						//Check if set contains the other endpoint of moved cables
						if (m_selection.Contains(con.cable.GetConnection(!con.connectedToFirst).point))
							con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)
						else
							con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
					}
					subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);
				}
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_currentPosition);
			}
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			if (m_draggingSelection)
			{
				AudioMain.Instance.PlaySound(AudioMain.ITEM_MOVED);

				Vector3 offset = a_dragFinalPosition - a_dragStartPosition;
				foreach (PointSubEntity subEntity in m_selection)
				{
					Vector3 finalPos = m_selectionDragStart[subEntity] + offset;
					//Vector3 frameOffset = finalPos - subEntity.GetPosition();
					subEntity.SetPosition(finalPos);

					//Move attached cables
					EnergyPointSubEntity energyEnt = subEntity as EnergyPointSubEntity;
					foreach (Connection con in energyEnt.Connections)
					{
						//Check if set contains the other endpoint of moved cables
						if (m_selection.Contains(con.cable.GetConnection(!con.connectedToFirst).point))      
							con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)                           
						else
							con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
					}
					subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);
				}
				m_draggingSelection = false;
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_dragFinalPosition);
				Select(m_currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				BoxSelect.HideBoxSelection();
				m_selectingBox = false;
				m_currentBoxSelection = null;
			}
		}

		protected override PointSubEntity StartModifyingSubEntity(PointSubEntity a_subEntity, bool a_insideUndoBatch)
		{
			if (m_planLayer == a_subEntity.m_entity.PlanLayer)
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				m_fsm.AddToUndoStack(new ModifyEnergyPointOperation(a_subEntity, m_planLayer, a_subEntity.GetDataCopy()));
				a_subEntity.m_edited = true;

				//Create undo operations for attached cables
				foreach (Connection con in (a_subEntity as EnergyPointSubEntity).Connections)
				{
					con.cable.AddModifyLineUndoOperation(m_fsm);
					con.cable.m_edited = true;
				}

				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
			}
			else
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				//Duplicate point
				PointSubEntity duplicate = CreateNewPlanPoint(a_subEntity.GetPosition(), a_subEntity.m_entity.EntityTypes, a_subEntity.GetPersistentID(), a_subEntity.m_entity.metaData, a_subEntity.m_entity.Country);
				SwitchSelectionFromBasePointToDuplicate(a_subEntity, duplicate);
				//subEntity = duplicate;

				//Duplicate all attached cables that were not part of the plan
				EnergyPointSubEntity oldEnergySubEnt = a_subEntity as EnergyPointSubEntity;
				EnergyPointSubEntity newEnergySubEnt = duplicate as EnergyPointSubEntity;
				List<EnergyLineStringSubEntity> newCables = new List<EnergyLineStringSubEntity>();
				foreach (Connection con in oldEnergySubEnt.Connections)
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
			return a_subEntity;
		}

		protected override PointSubEntity CreateNewPlanPoint(Vector3 a_point, List<EntityType> a_entityType, int a_persistentID, Dictionary<string, string> a_metaData, int a_country)
		{
			PointEntity newEntity = m_baseLayer.CreateNewPointEntity(a_point, a_entityType, m_planLayer);
			newEntity.metaData = new Dictionary<string, string>(a_metaData);
			newEntity.Country = a_country;
			PointSubEntity newSubEntity = newEntity.GetSubEntity(0) as PointSubEntity;
			newSubEntity.SetPersistentID(a_persistentID);
			m_fsm.AddToUndoStack(new CreateEnergyPointOperation(newSubEntity, m_planLayer, true, true));
			newSubEntity.DrawGameObject(m_baseLayer.LayerGameObject.transform);
			return newSubEntity;
		}
	}
}
