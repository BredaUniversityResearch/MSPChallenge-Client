using UnityEngine;
using System.Collections.Generic;

class EditEnergyPolygonState : EditPolygonsState
{
	private PlanLayer cablePlanLayer;

	public EditEnergyPolygonState(FSM fsm, PlanLayer planLayer, HashSet<PolygonSubEntity> selectedSubEntities) : base(fsm, planLayer, selectedSubEntities)
	{
		cablePlanLayer = planLayer.BaseLayer.greenEnergy ? LayerManager.energyCableLayerGreen.CurrentPlanLayer() : LayerManager.energyCableLayerGrey.CurrentPlanLayer();
	}

	protected override void OnPolygonRemoved(SubEntity removedSubEntity)
	{
		base.OnPolygonRemoved(removedSubEntity);
		//Connected cables removed second so connections stay intact in cables

		EnergyPointSubEntity energyEnt = ((EnergyPolygonSubEntity)removedSubEntity).sourcePoint;
		List<Connection> removedConnections = new List<Connection>(energyEnt.connections);
		foreach (Connection con in removedConnections)
		{
			fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(con.cable, cablePlanLayer, UndoOperation.EditMode.Modify, false, true));
			cablePlanLayer.BaseLayer.RemoveSubEntity(con.cable);
			con.cable.RemoveGameObject();
		}
	}

	protected override void OnPolygonModifiedViaRemoval(SubEntity modifiedSubEntity)
	{
		base.OnPolygonModifiedViaRemoval(modifiedSubEntity);
		EnergyPointSubEntity energyEnt = ((EnergyPolygonSubEntity)modifiedSubEntity).sourcePoint;
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
	}

	public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
	{
		base.Dragging(dragStartPosition, currentPosition);
		if (draggingSelection)
		{
			foreach (EnergyPointSubEntity energySubEnt in selectedSourcePoints)
			{
				Vector3 finalPos = energySubEnt.GetPosition();

				//Move attached cables
				foreach (Connection con in energySubEnt.connections)
				{
					//Check if set contains the other endpoint of moved cables
					//Dont add undo operations for these moves
					if (selectedSourcePoints.Contains(con.cable.GetConnection(!con.connectedToFirst).point))
						con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)
					else
						con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
				}
			}
		}
	}

	public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
	{
		if (draggingSelection)
		{
			updateSelectionDragPositions(dragFinalPosition - dragStartPosition, true);

			foreach (EnergyPointSubEntity energySubEnt in selectedSourcePoints)
			{
				Vector3 finalPos = energySubEnt.GetPosition();

				//Move attached cables
				foreach (Connection con in energySubEnt.connections)
				{
					//Check if set contains the other endpoint of moved cables
					//Dont add undo operations for these moves
					if (selectedSourcePoints.Contains(con.cable.GetConnection(!con.connectedToFirst).point))
						con.cable.MoveWithEndPoint(con.connectedToFirst); //Move entire cable (is called for both endpoints)
					else
						con.cable.SetPoint(finalPos, con.connectedToFirst); //Only move connected point
				}
			}
		}

		base.StoppedDragging(dragStartPosition, dragFinalPosition);
	}

	protected override PolygonSubEntity startModifyingSubEntity(PolygonSubEntity subEntity, bool insideUndoBatch)
	{
		if (subEntity.Entity.PlanLayer == planLayer)
		{
			if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

			fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
            subEntity.edited = true;

			//Create undo operations for cables attached to sourcepoint
			foreach (Connection con in (subEntity as EnergyPolygonSubEntity).sourcePoint.connections)
				con.cable.AddModifyLineUndoOperation(fsm);

			if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
		}
		else
		{
			if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

			//Duplicate polygon
			EnergyPolygonSubEntity oldEnergySubEnt = (subEntity as EnergyPolygonSubEntity);
			PolygonSubEntity duplicate = createNewPlanPolygon(subEntity.GetDataCopy(), subEntity.GetPersistentID());
			switchSelectionFromBasePolygonToDuplicate(subEntity, duplicate);
			//fsm.AddToUndoStack(new ChangeSourcePointActivity(oldEnergySubEnt, false)); //Adds an undo state for the old sourcepoint being deactivated

			//Duplicate all attached cables that were not part of the plan
			EnergyPointSubEntity newEnergySubEnt = (duplicate as EnergyPolygonSubEntity).sourcePoint;
			List<EnergyLineStringSubEntity> newCables = new List<EnergyLineStringSubEntity>();
			foreach (Connection con in oldEnergySubEnt.sourcePoint.connections)
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
}

