using UnityEngine;
using System.Collections;
using System.Collections.Generic;


class EditEnergyPointsState : EditPointsState
{
    PlanLayer cablePlanLayer;

    public EditEnergyPointsState(FSM fsm, PlanLayer planLayer) : base(fsm, planLayer)
    {
        cablePlanLayer = planLayer.BaseLayer.greenEnergy ? LayerManager.energyCableLayerGreen.CurrentPlanLayer() : LayerManager.energyCableLayerGrey.CurrentPlanLayer();
    }

    protected override void deleteSelection()
    {
        fsm.AddToUndoStack(new BatchUndoOperationMarker());

        foreach (PointSubEntity subEntity in selection)
        {
            EnergyPointSubEntity energyEnt = subEntity as EnergyPointSubEntity;
            if (subEntity.Entity.PlanLayer == planLayer)
            {
                //Point undo added first so it is undone last
                fsm.AddToUndoStack(new RemoveEnergyPointOperation(subEntity, planLayer));
                
                //Connected cables removed second so connections stay intact in cables
                List<Connection> removedConnections = new List<Connection>(energyEnt.connections);
                foreach (Connection con in removedConnections)
                {
                    fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(con.cable, cablePlanLayer, UndoOperation.EditMode.Modify, false, true));
                    cablePlanLayer.BaseLayer.RemoveSubEntity(con.cable);
                    con.cable.RemoveGameObject();
                }

                //Point itself removed last
                baseLayer.RemoveSubEntity(subEntity);
                subEntity.RemoveGameObject();
            }
            else
            {
                //Point undo added first
                fsm.AddToUndoStack(new ModifyPointRemovalPlanOperation(subEntity, planLayer, false));

                //Cables removed second
                List<Connection> removedConnections = new List<Connection>(energyEnt.connections);
                foreach (Connection con in removedConnections)
                {  
                    //Undooperation depends on wether the cable was on the current planlayer
                    if (con.cable.Entity.PlanLayer == cablePlanLayer)
                    {
                        fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(con.cable, cablePlanLayer, UndoOperation.EditMode.Modify, false, true));
                        con.cable.RemoveGameObject();
                    }
                    else
                        fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(con.cable, cablePlanLayer, false));
                    cablePlanLayer.BaseLayer.RemoveSubEntity(con.cable);  
                }

                //Point itself removed last
                baseLayer.RemoveSubEntity(subEntity);
            }
        }
        selection = new HashSet<PointSubEntity>();

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
    }

    public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        if (selectedRemovedEntity)
            return;

        if (draggingSelection)
        {
            Vector3 offset = currentPosition - dragStartPosition;

            foreach (PointSubEntity subEntity in selection)
            {
                Vector3 finalPos = selectionDragStart[subEntity] + offset;
                subEntity.SetPosition(finalPos);

                //Move attached cables
                EnergyPointSubEntity energyEnt = subEntity as EnergyPointSubEntity;
                foreach (Connection con in energyEnt.connections)
                {
                    //Check if set contains the other endpoint of moved cables
                    if (selection.Contains(con.cable.GetConnection(!con.connectedToFirst).point))
                        con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)
                    else
                        con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
                }
                subEntity.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null);
            }
        }
        else if (selectingBox)
        {
            updateBoxSelection(dragStartPosition, currentPosition);
        }
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        if (draggingSelection)
        {
            AudioMain.PlaySound(AudioMain.ITEM_MOVED);

            Vector3 offset = dragFinalPosition - dragStartPosition;
            foreach (PointSubEntity subEntity in selection)
            {
                Vector3 finalPos = selectionDragStart[subEntity] + offset;
                //Vector3 frameOffset = finalPos - subEntity.GetPosition();
                subEntity.SetPosition(finalPos);

                //Move attached cables
                EnergyPointSubEntity energyEnt = subEntity as EnergyPointSubEntity;
                foreach (Connection con in energyEnt.connections)
                {
                    //Check if set contains the other endpoint of moved cables
                    if (selection.Contains(con.cable.GetConnection(!con.connectedToFirst).point))      
                        con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)                           
                    else
                        con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
                }
                subEntity.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null);
            }
            draggingSelection = false;
        }
        else if (selectingBox)
        {
            updateBoxSelection(dragStartPosition, dragFinalPosition);
            select(currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            BoxSelect.HideBoxSelection();
            selectingBox = false;
            currentBoxSelection = null;
        }
    }

    protected override PointSubEntity startModifyingSubEntity(PointSubEntity subEntity, bool insideUndoBatch)
    {
        if (planLayer == subEntity.Entity.PlanLayer)
        {
            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

            fsm.AddToUndoStack(new ModifyEnergyPointOperation(subEntity, planLayer, subEntity.GetDataCopy()));

            //Create undo operations for attached cables
            foreach (Connection con in (subEntity as EnergyPointSubEntity).connections)
                con.cable.AddModifyLineUndoOperation(fsm);

            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
        }
        else
        {
            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

            //Duplicate point
            PointSubEntity duplicate = createNewPlanPoint(subEntity.GetPosition(), subEntity.Entity.EntityTypes, subEntity.GetPersistentID(), subEntity.Entity.metaData, subEntity.Entity.Country);
            switchSelectionFromBasePointToDuplicate(subEntity, duplicate);
            //subEntity = duplicate;

            //Duplicate all attached cables that were not part of the plan
            EnergyPointSubEntity oldEnergySubEnt = subEntity as EnergyPointSubEntity;
            EnergyPointSubEntity newEnergySubEnt = duplicate as EnergyPointSubEntity;
            List<EnergyLineStringSubEntity> newCables = new List<EnergyLineStringSubEntity>();
            foreach (Connection con in oldEnergySubEnt.connections)
                if (con.cable.Entity.PlanLayer != cablePlanLayer) //Make sure not to add 2 new cables if both endpoints are being edited
                    newCables.Add(con.cable);
                else //Change the cables connection from the old point to the new
                {
                    Connection newCon = new Connection(con.cable, newEnergySubEnt, con.connectedToFirst);
                    con.cable.RemoveConnection(con);
                    con.cable.AddConnection(newCon);
                    newEnergySubEnt.AddConnection(newCon);
                    con.cable.AddModifyLineUndoOperation(fsm);
                    fsm.AddToUndoStack(new ReconnectCableToPoint(con, newCon));
                }
            foreach (EnergyLineStringSubEntity cable in newCables)
                cable.DuplicateCableToPlanLayer(cablePlanLayer, newEnergySubEnt, fsm);
            subEntity = duplicate;

            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
        }
        return subEntity;
    }

    protected override PointSubEntity createNewPlanPoint(Vector3 point, List<EntityType> entityType, int persistentID, Dictionary<string, string> metaData, int country)
    {
        PointEntity newEntity = baseLayer.CreateNewPointEntity(point, entityType, planLayer);
		newEntity.metaData = new Dictionary<string, string>(metaData);
		newEntity.Country = country;
        PointSubEntity newSubEntity = newEntity.GetSubEntity(0) as PointSubEntity;
        newSubEntity.SetPersistentID(persistentID);
        fsm.AddToUndoStack(new CreateEnergyPointOperation(newSubEntity, planLayer, true, true));
        newSubEntity.DrawGameObject(baseLayer.LayerGameObject.transform);
        return newSubEntity;
    }
}

