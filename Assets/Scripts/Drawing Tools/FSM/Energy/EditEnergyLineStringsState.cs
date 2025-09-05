using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	class EditEnergyLineStringsState : EditLineStringsState
	{
		public EditEnergyLineStringsState(FSM a_fsm, PlanLayer a_planLayer, HashSet<LineStringSubEntity> a_selectedSubEntities) : base(a_fsm, a_planLayer, a_selectedSubEntities)
		{
		}

		protected override void DeleteSelection()
		{
			if (m_selectedPoints.Count == 0)
			{
				m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

				//Delete all selected
				foreach (LineStringSubEntity subEntity in m_selectedSubEntities)
				{
					if (subEntity.m_entity.PlanLayer == m_planLayer)
					{
						EnergyLineStringSubEntity energySubEntity = subEntity as EnergyLineStringSubEntity;

						m_fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(energySubEntity, m_planLayer, UndoOperation.EditMode.Modify));
						m_baseLayer.RemoveSubEntity(energySubEntity);
						subEntity.RemoveGameObject();
					}
					else
					{
						m_fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
						m_baseLayer.RemoveSubEntity(subEntity);
						subEntity.RedrawGameObject();
					}
					subEntity.WarningIfDeletingExisting(
						"Energy Cable",
						"In plan '{0}' you have removed an energy cable first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action.",
						m_planLayer.Plan
					);
				}

				m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			}
			else
			{
				//Delete selected points
				List<LineStringSubEntity> selectedPointsKeys = new List<LineStringSubEntity>(m_selectedPoints.Keys);
				foreach (LineStringSubEntity subEntity in selectedPointsKeys)
				{
					bool firstOrLast = (subEntity as EnergyLineStringSubEntity).AreFirstOrLastPoints(m_selectedPoints[subEntity]);
					if (subEntity.GetPointCount() - m_selectedPoints[subEntity].Count < 2 || firstOrLast)
					{
						if (firstOrLast && m_draggingSelection)
							m_fsm.AddToUndoStack(new ConcatOperationMarker()); //Make sure the point is also moved back if we were dragging it
						m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

						if (subEntity.m_entity.PlanLayer == m_planLayer)
						{
							EnergyLineStringSubEntity energySubEntity = subEntity as EnergyLineStringSubEntity;

							m_fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(energySubEntity, m_planLayer, UndoOperation.EditMode.Modify));
							m_baseLayer.RemoveSubEntity(energySubEntity);
							subEntity.RemoveGameObject();
						}
						else
						{
							m_fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
							m_baseLayer.RemoveSubEntity(subEntity);
							subEntity.RedrawGameObject();
						}

						m_selectedSubEntities.Remove(subEntity);
						subEntity.SetInFrontOfLayer(false);
						m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
					}
					else
					{
						m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
						LineStringSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);

						subEntityToModify.RemovePoints(m_selectedPoints[subEntityToModify]);

						subEntityToModify.RedrawGameObject();
						m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
					}
					subEntity.WarningIfDeletingExisting(
						"Energy Cable",
						"In plan '{0}' you have removed a part of an energy cable first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action.",
						m_planLayer.Plan
					);
				}
			}

			m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
		}

		public override void DoubleClick(Vector3 a_position)
		{
			LineStringSubEntity clickedSubEntity = GetSubEntityFromSelection(a_position, m_selectedSubEntities);
			if (clickedSubEntity == null)
				return;
			HashSet<int> allPoints = new HashSet<int>();
			//Ignores first and last point
			for (int i = 1; i < clickedSubEntity.GetPointCount() -1; ++i)
			{
				allPoints.Add(i);
			}
			Dictionary<LineStringSubEntity, HashSet<int>> newSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
			newSelection.Add(clickedSubEntity, allPoints);
			SelectPoints(newSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
		}

		protected override void UpdateSelectionDragPositions(Vector3 a_offset)
		{
			foreach (var kvp in m_selectedPoints)
			{
				if (kvp.Key.AreOnlyFirstOrLastPoint(kvp.Value))
				{
					Vector3 newPosition = m_selectionDragStart[kvp.Key][kvp.Value.First()] + a_offset;
					EnergyLineStringSubEntity energySubEntity = kvp.Key as EnergyLineStringSubEntity;
					bool first = kvp.Value.First() == 0;
					EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(newPosition);
					if (point != null)
					{
						//Snap to point
						energySubEntity.SetPointPosition(kvp.Value.First(), point.GetPosition());
						energySubEntity.m_restrictionNeedsUpdate = true;
						if (!point.CanConnectToEnergySubEntity(energySubEntity.GetConnection(!first).point))
						{
							//Redraw with red color
							energySubEntity.RedrawGameObject(SubEntityDrawMode.Invalid, kvp.Value, null);
							m_fsm.SetCursor(FSM.CursorType.Invalid);
						}
						else
						{
							energySubEntity.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value, null);
							m_fsm.SetCursor(FSM.CursorType.Default);
						}
					}
					else
						base.UpdateSelectionDragPositions(a_offset);
					return;
				}
				base.UpdateSelectionDragPositions(a_offset);
				return;
			}
		}

		public override void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			base.StartedDragging(a_dragStartPosition, a_currentPosition);
			if (m_draggingSelection)
			{
				foreach (var kvp in m_selectedPoints)
				{
					//If we are dragging endpoints, update nonreference layers
					if (kvp.Key.AreFirstOrLastPoints(kvp.Value))
					{
						foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
						{
							if (layer.m_greenEnergy == kvp.Key.m_entity.Layer.m_greenEnergy)
							{
								if (layer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
								{
									//Get and add the centerpoint layer
									EnergyPolygonLayer polyLayer = (EnergyPolygonLayer)layer;
									LayerManager.Instance.AddNonReferenceLayer(polyLayer.m_centerPointLayer, false);
									foreach (Entity entity in polyLayer.m_centerPointLayer.m_activeEntities)
										entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
								}
								else if (layer.m_editingType == AbstractLayer.EditingType.SourcePoint ||
										 layer.m_editingType == AbstractLayer.EditingType.Socket ||
										 layer.m_editingType == AbstractLayer.EditingType.Transformer)
								{
									LayerManager.Instance.AddNonReferenceLayer(layer, true);
								}
							}
						}
						return;
					}

				}
			}
		}		
		
		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			if (m_draggingSelection)
			{
				AudioMain.Instance.PlaySound(AudioMain.ITEM_MOVED);
				m_draggingSelection = false;

				//Handle start and endpoint movement if those were selected
				foreach (var kvp in m_selectedPoints)
					if (kvp.Key.AreFirstOrLastPoints(kvp.Value))
					{
						//Unset non-reference layers
						AbstractLayer baseLayer = InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer;
						if (baseLayer == null)
							LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { }, false, true);
						else
							LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { baseLayer }, false, true);

						foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
						{
							if (layer.m_greenEnergy == m_planLayer.BaseLayer.m_greenEnergy && layer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
							{
								foreach (Entity entity in ((EnergyPolygonLayer)layer).m_centerPointLayer.m_activeEntities)
								{
									entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
								}
							}
						}

						EnergyLineStringSubEntity energySubEntity = kvp.Key as EnergyLineStringSubEntity;
						bool first = kvp.Value.First() == 0;
						EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(a_dragFinalPosition);
						if (point == null || !point.CanConnectToEnergySubEntity(energySubEntity.GetConnection(!first).point))
						{
							//Snap back to original position
							//If this causes problems, the following line might be more appropriate
							m_fsm.Undo(false); //Undo the duplication or modified undo state, don't add it to redo
							m_fsm.UpdateUndoRedoButtonEnabled(); 
							return;
						}
						//Connect to new point
						energySubEntity.SetPointPosition(kvp.Value.First(), point.GetPosition());
						//Remove old conn
						Connection oldConn = energySubEntity.GetConnection(first);
						energySubEntity.RemoveConnection(oldConn);
						oldConn.point.RemoveConnection(oldConn);
						//Add new conn
						Connection newConn = new Connection(energySubEntity, point, first);
						energySubEntity.AddConnection(newConn);
						point.AddConnection(newConn);

						//Remove the last batch marker and add the changed connection to it
						m_fsm.AddToUndoStack(new ConcatOperationMarker());
						m_fsm.AddToUndoStack(new ChangeConnectionOperation(energySubEntity, oldConn, newConn));
                    
						return;
					}

				//Handle other point drag
				UpdateSelectionDragPositions(a_dragFinalPosition - a_dragStartPosition);
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_dragFinalPosition);

				SelectPoints(m_currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				BoxSelect.HideBoxSelection();
				m_selectingBox = false;
				m_currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
			}
		}

		protected override LineStringSubEntity StartModifyingSubEntity(LineStringSubEntity a_subEntity, bool a_insideUndoBatch)
		{
			if (m_planLayer == a_subEntity.m_entity.PlanLayer)
			{
				m_fsm.AddToUndoStack(new ModifyLineStringOperation(a_subEntity, m_planLayer, a_subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
				a_subEntity.m_edited = true;
			}
			else
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				//Duplicate linestring
				EnergyLineStringSubEntity duplicate = CreateNewPlanLineString(a_subEntity.GetPersistentID(), a_subEntity.GetDataCopy()) as EnergyLineStringSubEntity;
				SwitchSelectionFromBaseLineStringToDuplicate(a_subEntity, duplicate);

				//Add connections to new cable and reconnect attached points
				foreach (Connection con in (a_subEntity as EnergyLineStringSubEntity).Connections)
				{
					Connection newCon = new Connection(duplicate, con.point, con.connectedToFirst);
					con.point.RemoveConnection(con);
					newCon.point.AddConnection(newCon);
					duplicate.AddConnection(newCon);
					m_fsm.AddToUndoStack(new ReconnectCableToPoint(con, newCon));
				}
				a_subEntity = duplicate;

				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
			}
			a_subEntity.WarningIfEditingExisting(
				"Energy Cable",
				"In plan '{0}' you have affected an energy cable first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action."
			);
			return a_subEntity;
		}
	}
}
