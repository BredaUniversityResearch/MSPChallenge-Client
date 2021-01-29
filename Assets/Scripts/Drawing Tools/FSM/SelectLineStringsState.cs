using UnityEngine;
using System.Collections.Generic;

public class SelectLineStringsState : FSMState
{
    protected PlanLayer planLayer;
    protected LineStringLayer baseLayer = null;

    protected bool selectingBox = false;
    protected HashSet<LineStringSubEntity> currentBoxSelection = new HashSet<LineStringSubEntity>();

    protected LineStringSubEntity previousHover = null;

    public SelectLineStringsState(FSM fsm, PlanLayer planLayer) : base(fsm)
    {
        this.planLayer = planLayer;
        this.baseLayer = planLayer.BaseLayer as LineStringLayer;
        //if (layer.planLayer != null) { baseLayer = layer.planLayer.BaseLayer as LineStringLayer; }
    }

    public override void EnterState(Vector3 currentMousePosition)
    {
		base.EnterState(currentMousePosition);

		UIManager.SetToolbarMode(ToolBar.DrawingMode.Edit);
        UIManager.ToolbarEnable(false, FSM.ToolbarInput.Delete);
        UIManager.ToolbarEnable(false, FSM.ToolbarInput.Recall);
        UIManager.ToolbarEnable(false, FSM.ToolbarInput.Abort);
        UIManager.SetActivePlanWindowInteractability(false);

        LineStringSubEntity hover = baseLayer.GetSubEntityAt(currentMousePosition) as LineStringSubEntity;
        //if (hover == null && baseLayer != null) { hover = baseLayer.GetSubEntityAt(currentMousePosition) as LineStringSubEntity; }

        if (hover != null)
        {
            HoveredSubEntity(hover, true);
        }

        previousHover = hover;
    }

    public override void LeftClick(Vector3 worldPosition)
    {
        LineStringSubEntity hover = baseLayer.GetSubEntityAt(worldPosition) as LineStringSubEntity;
        //if (hover == null && baseLayer != null) { hover = baseLayer.GetSubEntityAt(position) as LineStringSubEntity; }

        if (hover != null)
        {
            if (baseLayer.IsEnergyLineLayer())
                fsm.SetCurrentState(new EditEnergyLineStringsState(fsm, planLayer, new HashSet<LineStringSubEntity>() { hover }));
            else
                fsm.SetCurrentState(new EditLineStringsState(fsm, planLayer, new HashSet<LineStringSubEntity>() { hover }));

        }
    }

    public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
    {
        if (!selectingBox)
        {
            LineStringSubEntity hover = null;
            if (!cursorIsOverUI)
            {
                hover = baseLayer.GetSubEntityAt(currentPosition) as LineStringSubEntity;
                if (hover == null && baseLayer != null) { hover = baseLayer.GetSubEntityAt(currentPosition) as LineStringSubEntity; }
            }

            if (previousHover != null || hover != null)
            {
                if (previousHover != null)
                {
                    HoveredSubEntity(previousHover, false);
                }

                if (hover != null)
                {
                    HoveredSubEntity(hover, true);
                }
            }

            previousHover = hover;
        }
    }

    public override void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        selectingBox = true;
        currentBoxSelection = new HashSet<LineStringSubEntity>();

        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);
    }

    public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        updateBoxSelection(dragStartPosition, currentPosition);
    }

    protected void updateBoxSelection(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);

        HashSet<LineStringSubEntity> selectionsInBox = baseLayer.GetSubEntitiesInBox(dragStartPosition, currentPosition);
        //if (baseLayer != null) { selectionsInBox.UnionWith(baseLayer.GetSubEntitiesInBox(dragStartPosition, currentPosition)); }

        foreach (LineStringSubEntity selectionInBox in selectionsInBox)
        {
            if (!currentBoxSelection.Contains(selectionInBox)) { HoveredSubEntity(selectionInBox, true); }
        }

        foreach (LineStringSubEntity currentlySelected in currentBoxSelection)
        {
            if (!selectionsInBox.Contains(currentlySelected)) { HoveredSubEntity(currentlySelected, false); }
        }

        currentBoxSelection = selectionsInBox;
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        updateBoxSelection(dragStartPosition, dragFinalPosition);

        if (currentBoxSelection.Count > 0)
        {
            if (baseLayer.IsEnergyLineLayer())
                fsm.SetCurrentState(new EditEnergyLineStringsState(fsm, planLayer, currentBoxSelection));
            else
                fsm.SetCurrentState(new EditLineStringsState(fsm, planLayer, currentBoxSelection));
        }

        BoxSelect.HideBoxSelection();
        selectingBox = false;
    }

    public override void HandleKeyboardEvents()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (baseLayer.IsEnergyLineLayer())
                fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, planLayer));
            else
                fsm.SetCurrentState(new StartCreatingLineStringState(fsm, planLayer));
        }
    }

    public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
    {
        switch (toolbarInput)
        {
            case FSM.ToolbarInput.Create:
                if (baseLayer.IsEnergyLineLayer())
                    fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, planLayer));
                else
                    fsm.SetCurrentState(new StartCreatingLineStringState(fsm, planLayer));
                break;
            case FSM.ToolbarInput.SelectAll:
                fsm.SetCurrentState(new EditLineStringsState(fsm, planLayer, new HashSet<LineStringSubEntity>((baseLayer as LineStringLayer).GetAllSubEntities())));
                break;
        }
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        if (previousHover != null)
        {
            HoveredSubEntity(previousHover, false);
        }

        foreach (LineStringSubEntity lse in currentBoxSelection)
        {
            lse.RedrawGameObject();
        }

        BoxSelect.HideBoxSelection();
    }
}