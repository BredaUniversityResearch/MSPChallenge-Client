using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CreatingLineStringState : FSMState
{
	protected LineStringSubEntity subEntity;
	protected PlanLayer planLayer;

	public CreatingLineStringState(FSM fsm, PlanLayer planLayer, LineStringSubEntity subEntity) : base(fsm)
	{
		this.subEntity = subEntity;
		this.planLayer = planLayer;
	}

	public override void EnterState(Vector3 currentMousePosition)
	{
		base.EnterState(currentMousePosition);

		UIManager.SetToolbarMode(ToolBar.DrawingMode.Create);
		UIManager.ToolbarEnable(false, FSM.ToolbarInput.Delete);
		UIManager.ToolbarEnable(false, FSM.ToolbarInput.Recall);
		UIManager.ToolbarEnable(true, FSM.ToolbarInput.Abort);
		UIManager.SetTeamAndTypeToBasicIfEmpty();
		UIManager.SetActivePlanWindowInteractability(true);

		int pointCount = subEntity.GetPointCount();
		subEntity.SetPointPosition(pointCount - 1, subEntity.GetPointPosition(pointCount - 2));

		LineStringLayer layer = (LineStringLayer)subEntity.Entity.Layer;
		if (layer.Entities.Contains(subEntity.Entity as LineStringEntity))
		{
			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
		}
		else
		{
			layer.RestoreSubEntity(subEntity);
			subEntity.DrawGameObject(layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
		}

		fsm.SetCursor(FSM.CursorType.Add);
		fsm.SetSnappingEnabled(true);

		IssueManager.instance.SetIssueInteractability(false);
	}

	public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
	{
		Vector3 snappingPoint;
        bool drawAsInvalid;
        if (ClickingWouldFinishDrawing(currentPosition, out snappingPoint, out drawAsInvalid))
		{
			subEntity.SetPointPosition(subEntity.GetPointCount() - 1, snappingPoint);
            if (drawAsInvalid)
                subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
            else
                subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
            fsm.SetCursor(FSM.CursorType.Complete);
		}
		else
		{
			subEntity.SetPointPosition(subEntity.GetPointCount() - 1, snappingPoint);
            if(drawAsInvalid)
			    subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
            else
			    subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
            fsm.SetCursor(FSM.CursorType.Add);
		}

		if (cursorIsOverUI)
		{
			fsm.SetCursor(FSM.CursorType.Default);
		}
	}

	protected virtual bool ClickingWouldFinishDrawing(Vector3 position, out Vector3 snappingPoint, out bool drawAsInvalid)
	{
        drawAsInvalid = false;
		if (subEntity.GetPointCount() < 3)
		{
            snappingPoint = position;
			return false;
		}

		float closestDistSq;
		int pointClicked = subEntity.GetPointAt(position, out closestDistSq, subEntity.GetPointCount() - 1);

		if (pointClicked != -1)
			snappingPoint = subEntity.GetPointPosition(pointClicked);
		else
			snappingPoint = position;
		return pointClicked == subEntity.GetPointCount() - 2;
	}

	public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
	{
		AudioMain.PlaySound(AudioMain.ITEM_PLACED);

		Vector3 snappingPoint;
        bool drawAsInvalid;
        if (ClickingWouldFinishDrawing(finalPosition, out snappingPoint, out drawAsInvalid))
		{
			fsm.AddToUndoStack(new FinalizeLineStringOperation(subEntity, planLayer));
			FinalizeLineString();
			return;
		}

		SubEntityDataCopy dataCopy = subEntity.GetDataCopy();

		subEntity.AddPoint(finalPosition);
		subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
		if (subEntity.GetPointCount() > 1)
			fsm.SetCursor(FSM.CursorType.Complete);

		fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
	}

    public override void HandleEntityTypeChange(List<EntityType> newTypes)
    {
        SubEntityDataCopy dataCopy = subEntity.GetDataCopy();

        subEntity.Entity.EntityTypes = newTypes;
        subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

        fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
    }

    public override void Abort()
	{
		fsm.AddToUndoStack(new RemoveLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
		fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
	}

	public virtual void FinalizeLineString()
	{
		subEntity.RemovePoints(new HashSet<int>() { subEntity.GetPointCount() - 1 });

		List<EntityType> selectedType = UIManager.GetCurrentEntityTypeSelection();
		if (selectedType != null) { subEntity.Entity.EntityTypes = selectedType; }

		subEntity.restrictionNeedsUpdate = true;
		subEntity.UnHideRestrictionArea();
		subEntity.RedrawGameObject(SubEntityDrawMode.Default);

		subEntity = null; // set line string to null so the exit state function doesn't remove it

		fsm.SetCurrentState(new StartCreatingLineStringState(fsm, planLayer));
	}

	public override void HandleKeyboardEvents()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			fsm.AddToUndoStack(new FinalizeLineStringOperation(subEntity, planLayer));
			FinalizeLineString();
		}
		else if (Input.GetKeyDown(KeyCode.Escape))
		{
			Abort();
		}
	}

	public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
	{
		switch (toolbarInput)
		{
		case FSM.ToolbarInput.Edit:
		case FSM.ToolbarInput.Abort:
			Abort();
			break;
		}
	}

	public override void ExitState(Vector3 currentMousePosition)
	{
		if (subEntity != null)
		{
			subEntity.Entity.Layer.RemoveSubEntity(subEntity, false);
			subEntity.RemoveGameObject();
		}

		IssueManager.instance.SetIssueInteractability(true);
	}
}
