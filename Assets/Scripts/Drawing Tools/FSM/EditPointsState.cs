using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EditPointsState : FSMState
{
    protected PointLayer baseLayer;
    protected PlanLayer planLayer;

    protected bool selectedRemovedEntity = false;
    protected bool draggingSelection = false;
    protected Dictionary<PointSubEntity, Vector3> selectionDragStart = null;

    protected bool selectingBox = false;
    protected HashSet<PointSubEntity> currentBoxSelection = null;

    protected HashSet<PointSubEntity> selection = new HashSet<PointSubEntity>();

    PointSubEntity previousHover = null;

    protected static HashSet<int> firstPoint = new HashSet<int>() { 0 };

    public EditPointsState(FSM fsm, PlanLayer planLayer) : base(fsm)
    {
        this.planLayer = planLayer;
        this.baseLayer = planLayer.BaseLayer as PointLayer;
        //if (layer.planLayer != null)
        //{
        //    planLayer = layer.planLayer;
        //    baseLayer = planLayer.BaseLayer as PointLayer;
        //}
    }

    public override void EnterState(Vector3 currentMousePosition)
    {
		base.EnterState(currentMousePosition);
		
        UIManager.SetToolbarMode(ToolBar.DrawingMode.Edit);
        UIManager.ToolbarEnable(false, FSM.ToolbarInput.Delete, FSM.ToolbarInput.Recall, FSM.ToolbarInput.Abort);
        UIManager.SetActivePlanWindowInteractability(false);

        PointSubEntity hover = baseLayer.GetPointAt(currentMousePosition);

        if (hover != null)
        {
            hover.RedrawGameObject(SubEntityDrawMode.Default, null, firstPoint);
        }

        previousHover = hover;

        fsm.SetSnappingEnabled(true);
		IssueManager.instance.SetIssueInteractability(false);
    }

    public override void LeftClick(Vector3 worldPosition)
    {
        PointSubEntity point = baseLayer.GetPointAt(worldPosition);
        if (point == null && baseLayer != null) { point = baseLayer.GetPointAt(worldPosition); }

        if (point != null)
        {
            select(new HashSet<PointSubEntity>() { point }, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        else
        {
            select(new HashSet<PointSubEntity>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
    }

    protected void select(HashSet<PointSubEntity> newSelection, bool keepPreviousSelection)
    {
        if (!keepPreviousSelection)
        {
            foreach (PointSubEntity pse in selection)
            {
                pse.RedrawGameObject(SubEntityDrawMode.Default, null, null);
            }
            selection = newSelection;
        }
        else if(!selectedRemovedEntity)
        {
            selection.UnionWith(newSelection);
        }

        selectedRemovedEntity = false;
        foreach (PointSubEntity pse in newSelection)
        {
            if (pse.IsPlannedForRemoval())
                selectedRemovedEntity = true;
            pse.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null);
        }

        UIManager.ToolbarEnable(selection.Count > 0, FSM.ToolbarInput.Delete, FSM.ToolbarInput.Abort);
        UIManager.ToolbarEnable(selectedRemovedEntity, FSM.ToolbarInput.Recall);
		UIManager.SetActivePlanWindowChangeable(!selectedRemovedEntity);
		//Points have no selecting state, so dropdown interactivity can change while in this state
		if (selection.Count == 0)
        {
            UIManager.SetActivePlanWindowInteractability(false);
            return;
        }
        else
		{
			UIManager.SetActivePlanWindowChangeable(!selectedRemovedEntity);
		}

		UpdateActivePlanWindowToSelection();
	}

    public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
    {
        if (!draggingSelection && !selectingBox)
        {
            PointSubEntity hover = null;
            if (!cursorIsOverUI)
            {
                hover = baseLayer.GetPointAt(currentPosition);
                if (hover == null && baseLayer != null) { hover = baseLayer.GetPointAt(currentPosition); }
            }

            if (previousHover != hover)
            {
                if (previousHover != null)
                {
                    if (selection.Contains(previousHover)) { previousHover.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null); }
                    else { previousHover.RedrawGameObject(); }
                }

                if (hover != null)
                {
                    if (selection.Contains(hover)) { hover.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, firstPoint); }
                    else { hover.RedrawGameObject(SubEntityDrawMode.Default, null, firstPoint); }
                }
            }

            previousHover = hover;

            if (hover == null)
            {
                fsm.SetCursor(FSM.CursorType.Default);
            }
            else
            {
                fsm.SetCursor(FSM.CursorType.Move);
            }
        }
    }

    protected virtual PointSubEntity createNewPlanPoint(Vector3 point, List<EntityType> entityType, int persistentID, Dictionary<string, string> metaData, int country)
    {
        PointEntity newEntity = baseLayer.CreateNewPointEntity(point, entityType, planLayer);
		newEntity.metaData = new Dictionary<string, string>(metaData);
		newEntity.Country = country;
        PointSubEntity newSubEntity = newEntity.GetSubEntity(0) as PointSubEntity;
        newSubEntity.SetPersistentID(persistentID);
        fsm.AddToUndoStack(new CreatePointOperation(newSubEntity, planLayer, true));
        newSubEntity.DrawGameObject(baseLayer.LayerGameObject.transform);
        return newSubEntity;
    }

    protected void switchSelectionFromBasePointToDuplicate(PointSubEntity basePoint, PointSubEntity duplicate)
    {
        selection.Add(duplicate);
        selection.Remove(basePoint);

        //Change active geom 
        baseLayer.AddPreModifiedEntity(basePoint.Entity);
        baseLayer.activeEntities.Remove(basePoint.Entity as PointEntity);
        baseLayer.activeEntities.Add(duplicate.Entity as PointEntity);


        //Redraw based on activity changes
        basePoint.RedrawGameObject();
        duplicate.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint);
    }

    protected virtual PointSubEntity startModifyingSubEntity(PointSubEntity subEntity, bool insideUndoBatch)
    {
        if (subEntity.Entity.PlanLayer == planLayer)
        {
            fsm.AddToUndoStack(new ModifyPointOperation(subEntity, planLayer, subEntity.GetDataCopy()));
        }
        else
        {
            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

            PointSubEntity duplicate = createNewPlanPoint(subEntity.GetPosition(), subEntity.Entity.EntityTypes, subEntity.GetPersistentID(), subEntity.Entity.metaData, subEntity.Entity.Country);
            switchSelectionFromBasePointToDuplicate(subEntity, duplicate);
            subEntity = duplicate;

            if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
        }
        return subEntity;
    }

    protected void createUndoForDraggedSelection()
    {
        fsm.AddToUndoStack(new BatchUndoOperationMarker());

        HashSet<PointSubEntity> selectionCopy = new HashSet<PointSubEntity>(selection);
        foreach (PointSubEntity subEntity in selectionCopy)
        {
            startModifyingSubEntity(subEntity, true);
        }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
    }

    public override void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        if (selectedRemovedEntity)
            return;
        PointSubEntity draggingPoint = baseLayer.GetPointAt(dragStartPosition);
        if (draggingPoint != null && !selection.Contains(draggingPoint))
        {
            select(new HashSet<PointSubEntity> { draggingPoint }, false);
            if (selectedRemovedEntity)
                return;
        }

        if (draggingPoint != null)
        {
            draggingSelection = true;
            createUndoForDraggedSelection();

            // this offset is used to make sure the user is dragging the center of the point that is being dragged (to make snapping work correctly)
            Vector3 offset = dragStartPosition - draggingPoint.GetPosition();

            selectionDragStart = new Dictionary<PointSubEntity, Vector3>();
            foreach (PointSubEntity pse in selection)
            {
                selectionDragStart.Add(pse, pse.GetPosition() + offset);
            }
        }
        else
        {
            select(new HashSet<PointSubEntity>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

            selectingBox = true;
            currentBoxSelection = new HashSet<PointSubEntity>();

            BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);
        }
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
                subEntity.SetPosition(selectionDragStart[subEntity] + offset);
                subEntity.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null);
            }
        }
        else if (selectingBox)
        {
            updateBoxSelection(dragStartPosition, currentPosition);
        }
    }

    protected void updateBoxSelection(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);

        HashSet<PointSubEntity> pointsInBox = baseLayer.GetPointsInBox(dragStartPosition, currentPosition);
        //if (baseLayer != null) { pointsInBox.UnionWith(baseLayer.GetPointsInBox(dragStartPosition, currentPosition)); }

        foreach (PointSubEntity pointInBox in pointsInBox)
        {
            if (!currentBoxSelection.Contains(pointInBox))
            {
                if (selection.Contains(pointInBox)) { pointInBox.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, firstPoint); }
                else { pointInBox.RedrawGameObject(SubEntityDrawMode.Default, null, firstPoint); }
            }
        }

        foreach (PointSubEntity selectedPoint in currentBoxSelection)
        {
            if (!pointsInBox.Contains(selectedPoint))
            {
                if (selection.Contains(selectedPoint)) { selectedPoint.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null); }
                else { selectedPoint.RedrawGameObject(SubEntityDrawMode.Default, null, null); }
            }
        }

        currentBoxSelection = pointsInBox;
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        if (draggingSelection)
        {
            AudioMain.PlaySound(AudioMain.ITEM_MOVED);

            Vector3 offset = dragFinalPosition - dragStartPosition;
            foreach (PointSubEntity subEntity in selection)
            {
                subEntity.SetPosition(selectionDragStart[subEntity] + offset);
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

    protected virtual void deleteSelection()
    {
        fsm.AddToUndoStack(new BatchUndoOperationMarker());

        foreach (PointSubEntity subEntity in selection)
        {
            if (subEntity.Entity.PlanLayer == planLayer)
            {
                fsm.AddToUndoStack(new RemovePointOperation(subEntity, planLayer));
                //planLayer.RemovedGeometry.Add(subEntity.GetPersistentID());
                baseLayer.RemoveSubEntity(subEntity);
                subEntity.RemoveGameObject();
            }
            else
            {
                fsm.AddToUndoStack(new ModifyPointRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                baseLayer.RemoveSubEntity(subEntity);
                subEntity.RedrawGameObject();
            }
        }
        selection = new HashSet<PointSubEntity>();

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
    }

    private void undoDeleteForSelection()
    {
        if (selectedRemovedEntity)
        {
            fsm.AddToUndoStack(new BatchUndoOperationMarker());
            foreach (PointSubEntity subEntity in selection)
            {
                fsm.AddToUndoStack(new ModifyPointRemovalPlanOperation(subEntity, planLayer, true));
                planLayer.RemovedGeometry.Remove(subEntity.GetPersistentID());
                subEntity.RestoreDependencies();
                subEntity.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null);

            }
            fsm.AddToUndoStack(new BatchUndoOperationMarker());
            selectedRemovedEntity = false;
        }
    }

    public override void HandleKeyboardEvents()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            deleteSelection();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (baseLayer.IsEnergyPointLayer())
                fsm.SetCurrentState(new CreateEnergyPointState(fsm, planLayer));
            else
                fsm.SetCurrentState(new CreatePointsState(fsm, planLayer));
        }
    }

    public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
    {
        switch (toolbarInput)
        {
            case FSM.ToolbarInput.Create:
                if (baseLayer.IsEnergyPointLayer())
                    fsm.SetCurrentState(new CreateEnergyPointState(fsm, planLayer));
                else
                    fsm.SetCurrentState(new CreatePointsState(fsm, planLayer));
                break;
            case FSM.ToolbarInput.Delete:
                deleteSelection();
                break;
            case FSM.ToolbarInput.Abort:
                select(new HashSet<PointSubEntity>(), false);
                break;
            case FSM.ToolbarInput.SelectAll:
                select(new HashSet<PointSubEntity>((baseLayer as PointLayer).GetAllSubEntities()), true);
                break;
            case FSM.ToolbarInput.Recall:
                undoDeleteForSelection();
                break;
        }
    }

    public override void HandleEntityTypeChange(List<EntityType> newTypes)
    {
        List<PointSubEntity> subEntitiesWithDifferentTypes = new List<PointSubEntity>();

        //Find subentities with changed entity types
        foreach (PointSubEntity subEntity in selection)
        {
            if (subEntity.Entity.EntityTypes.Count != newTypes.Count)
            {
                subEntitiesWithDifferentTypes.Add(subEntity);
                continue;
            }
            foreach (EntityType type in subEntity.Entity.EntityTypes)
            {
                if (!newTypes.Contains(type))
                {
                    subEntitiesWithDifferentTypes.Add(subEntity);
                    break;
                }
            }
        }

        if (subEntitiesWithDifferentTypes.Count == 0) { return; }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());

        foreach (PointSubEntity subEntity in subEntitiesWithDifferentTypes)
        {
            PointSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
            subEntityToModify.Entity.EntityTypes = newTypes;
            subEntityToModify.RedrawGameObject(SubEntityDrawMode.Selected, firstPoint, null);
        }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
    }

    public override void HandleTeamChange(int newTeam)
    {
        List<PointSubEntity> subEntitiesWithDifferentTeam = new List<PointSubEntity>();

        //Find subentities with changed entity types
        foreach (PointSubEntity subEntity in selection)
        {
            if (subEntity.Entity.Country != newTeam)
            {
                subEntitiesWithDifferentTeam.Add(subEntity);
            }
        }

        if (subEntitiesWithDifferentTeam.Count == 0) { return; }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());

        foreach (PointSubEntity subEntity in subEntitiesWithDifferentTeam)
        {
            PointSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
            subEntityToModify.Entity.Country = newTeam;
        }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
    }

	public override void HandleParameterChange(EntityPropertyMetaData parameter, string newValue)
	{
		List<PointSubEntity> subEntitiesWithDifferentParams = new List<PointSubEntity>();

		//Find subentities with changed entity types
		foreach (PointSubEntity subEntity in selection)
		{
			if (subEntity.Entity.GetPropertyMetaData(parameter) != newValue)
			{
				subEntitiesWithDifferentParams.Add(subEntity);
			}
		}

		if (subEntitiesWithDifferentParams.Count == 0) { return; }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (PointSubEntity subEntity in subEntitiesWithDifferentParams)
		{
			PointSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.SetPropertyMetaData(parameter, newValue);
		}

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void ExitState(Vector3 currentMousePosition)
    {
        if (previousHover != null)
        {
            previousHover.RedrawGameObject(SubEntityDrawMode.Default, null, null);
        }

        foreach (PointSubEntity pse in selection)
        {
            pse.RedrawGameObject(SubEntityDrawMode.Default);
        }
        selection = new HashSet<PointSubEntity>();

        BoxSelect.HideBoxSelection();
		IssueManager.instance.SetIssueInteractability(true);

        // make sure the entity type dropdown shows a valid value
        //UIManager.SetCurrentEntityTypeSelection(UIManager.GetCurrentEntityTypeSelection());
    }

	private void UpdateActivePlanWindowToSelection()
	{
		List<List<EntityType>> selectedEntityTypes = new List<List<EntityType>>();
		int? selectedTeam = null;
		List<Dictionary<EntityPropertyMetaData, string>> selectedParams = new List<Dictionary<EntityPropertyMetaData, string>>();

		foreach (PointSubEntity pse in selection)
		{
			selectedEntityTypes.Add(pse.Entity.EntityTypes);
			if (selectedTeam.HasValue && pse.Entity.Country != selectedTeam.Value)
				selectedTeam = -1;
			else
				selectedTeam = pse.Entity.Country;
			Dictionary<EntityPropertyMetaData, string> parameters = new Dictionary<EntityPropertyMetaData, string>();
			foreach (EntityPropertyMetaData p in baseLayer.propertyMetaData)
			{
				if (p.ShowInEditMode)
					parameters.Add(p, pse.Entity.GetPropertyMetaData(p));
			}
			selectedParams.Add(parameters);

		}
		UIManager.SetActiveplanWindowToSelection(
			selectedEntityTypes.Count > 0 ? selectedEntityTypes : null,
			selectedTeam ?? -2,
			selectedParams);
	}
}
