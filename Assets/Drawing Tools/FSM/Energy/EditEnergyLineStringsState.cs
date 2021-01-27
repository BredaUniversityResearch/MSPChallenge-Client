using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


class EditEnergyLineStringsState : EditLineStringsState
{
    public EditEnergyLineStringsState(FSM fsm, PlanLayer planLayer, HashSet<LineStringSubEntity> selectedSubEntities) : base(fsm, planLayer, selectedSubEntities)
    {
    }

    protected override void deleteSelection()
    {
        if (selectedPoints.Count == 0)
        {
            fsm.AddToUndoStack(new BatchUndoOperationMarker());

            //Delete all selected
            foreach (LineStringSubEntity subEntity in selectedSubEntities)
            {
                if (subEntity.Entity.PlanLayer == planLayer)
                {
                    EnergyLineStringSubEntity energySubEntity = subEntity as EnergyLineStringSubEntity;

                    fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(energySubEntity, planLayer, UndoOperation.EditMode.Modify));
                    baseLayer.RemoveSubEntity(energySubEntity);
                    subEntity.RemoveGameObject();
                }
                else
                {
                    fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                    baseLayer.RemoveSubEntity(subEntity);
                    subEntity.RedrawGameObject();
                }
            }

            fsm.AddToUndoStack(new BatchUndoOperationMarker());
        }
        else
        {
            //Delete selected points
            List<LineStringSubEntity> selectedPointsKeys = new List<LineStringSubEntity>(selectedPoints.Keys);
            foreach (LineStringSubEntity subEntity in selectedPointsKeys)
            {
                bool firstOrLast = (subEntity as EnergyLineStringSubEntity).AreFirstOrLastPoints(selectedPoints[subEntity]);
                if (subEntity.GetPointCount() - selectedPoints[subEntity].Count < 2 || firstOrLast)
                {
                    if (firstOrLast && draggingSelection)
                        fsm.AddToUndoStack(new ConcatOperationMarker()); //Make sure the point is also moved back if we were dragging it
                    fsm.AddToUndoStack(new BatchUndoOperationMarker());

                    if (subEntity.Entity.PlanLayer == planLayer)
                    {
                        EnergyLineStringSubEntity energySubEntity = subEntity as EnergyLineStringSubEntity;

                        fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(energySubEntity, planLayer, UndoOperation.EditMode.Modify));
                        baseLayer.RemoveSubEntity(energySubEntity);
                        subEntity.RemoveGameObject();
                    }
                    else
                    {
                        fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                        baseLayer.RemoveSubEntity(subEntity);
                        subEntity.RedrawGameObject();
                    }

                    selectedSubEntities.Remove(subEntity);
                    fsm.AddToUndoStack(new BatchUndoOperationMarker());
                }
                else
                {
                    fsm.AddToUndoStack(new BatchUndoOperationMarker());
                    LineStringSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);

                    subEntityToModify.RemovePoints(selectedPoints[subEntityToModify]);

                    subEntityToModify.RedrawGameObject();
                    fsm.AddToUndoStack(new BatchUndoOperationMarker());
                }
            }
        }

        fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
    }

    public override void DoubleClick(Vector3 position)
    {
        LineStringSubEntity clickedSubEntity = getSubEntityFromSelection(position, selectedSubEntities);
        if (clickedSubEntity != null)
        {
            HashSet<int> allPoints = new HashSet<int>();
            //Ignores first and last point
            for (int i = 1; i < clickedSubEntity.GetPointCount() -1; ++i)
            {
                allPoints.Add(i);
            }
            Dictionary<LineStringSubEntity, HashSet<int>> newSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
            newSelection.Add(clickedSubEntity, allPoints);
            selectPoints(newSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
    }

    protected override void UpdateSelectionDragPositions(Vector3 offset)
    {
        foreach (var kvp in selectedPoints)
        {
            if (kvp.Key.AreOnlyFirstOrLastPoint(kvp.Value))
            {
                Vector3 newPosition = selectionDragStart[kvp.Key][kvp.Value.First()] + offset;
                EnergyLineStringSubEntity energySubEntity = kvp.Key as EnergyLineStringSubEntity;
                bool first = kvp.Value.First() == 0;
                EnergyPointSubEntity point = LayerManager.GetEnergyPointAtPosition(newPosition);
                if (point != null)
                {
                    //Snap to point
                    energySubEntity.SetPointPosition(kvp.Value.First(), point.GetPosition());
                    energySubEntity.restrictionNeedsUpdate = true;
                    if(!point.CanConnectToEnergySubEntity(energySubEntity.GetConnection(!first).point))
                    { 
                        //Redraw with red color
                        energySubEntity.RedrawGameObject(SubEntityDrawMode.Invalid, kvp.Value, null);
                    }
                    else
                        energySubEntity.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value, null);
                }
                else
                    base.UpdateSelectionDragPositions(offset);
                return;
            }
            else
                base.UpdateSelectionDragPositions(offset);
            return;
        }
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        if (draggingSelection)
        {
            AudioMain.PlaySound(AudioMain.ITEM_MOVED);
            draggingSelection = false;

            //Handle start and endpoint movement if those were selected
            foreach (var kvp in selectedPoints)
                if (kvp.Key.AreFirstOrLastPoints(kvp.Value))
                {
                    EnergyLineStringSubEntity energySubEntity = kvp.Key as EnergyLineStringSubEntity;
                    bool first = kvp.Value.First() == 0;
                    EnergyPointSubEntity point = LayerManager.GetEnergyPointAtPosition(dragFinalPosition);
                    if (point == null || !point.CanConnectToEnergySubEntity(energySubEntity.GetConnection(!first).point))
                    {
                        //Snap back to original position
                        //If this causes problems, the following line might be more appropriate
                        //energySubEntity.SetPointPosition(kvp.Value.First(), energySubEntity.GetConnection(first).point.GetPosition(), true);
                        fsm.undo(); //Undo the duplication or modified undo state
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
                    fsm.AddToUndoStack(new ConcatOperationMarker());
                    fsm.AddToUndoStack(new ChangeConnectionOperation(energySubEntity, oldConn, newConn));
                    
                    return;
                }

            //Handle other point drag
            UpdateSelectionDragPositions(dragFinalPosition - dragStartPosition);
        }
        else if (selectingBox)
        {
            UpdateBoxSelection(dragStartPosition, dragFinalPosition);

            selectPoints(currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            BoxSelect.HideBoxSelection();
            selectingBox = false;
            currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
        }
    }

    protected override LineStringSubEntity startModifyingSubEntity(LineStringSubEntity subEntity, bool insideUndoBatch)
    {
        if (planLayer == subEntity.Entity.PlanLayer)
        {
            fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
        }
        else
        {
            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

            //Duplicate linestring
            EnergyLineStringSubEntity duplicate = createNewPlanLineString(subEntity.GetPersistentID(), subEntity.GetDataCopy()) as EnergyLineStringSubEntity;
            switchSelectionFromBaseLineStringToDuplicate(subEntity, duplicate);
            
            //Add connections to new cable and reconnect attached points
            foreach (Connection con in (subEntity as EnergyLineStringSubEntity).connections)
            {
                Connection newCon = new Connection(duplicate, con.point, con.connectedToFirst);
                con.point.RemoveConnection(con);
                newCon.point.AddConnection(newCon);
                duplicate.AddConnection(newCon);
                fsm.AddToUndoStack(new ReconnectCableToPoint(con, newCon));
            }
            subEntity = duplicate;

            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
        }
        return subEntity;
    }
}

