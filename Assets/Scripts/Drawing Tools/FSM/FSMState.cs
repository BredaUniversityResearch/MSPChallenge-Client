using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class FSMState
{
    protected FSM fsm;

    public FSMState(FSM fsm)
    {
        this.fsm = fsm;
    }

    //State meta
    public virtual void EnterState(Vector3 currentMousePosition)
	{
		UIManager.ToolbarVisibility(false, FSM.ToolbarInput.ChangeDirection);
	}
    public virtual void ExitState(Vector3 currentMousePosition) { }

    //Mouse input
    public virtual void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI) { }
    public virtual void LeftMouseButtonDown(Vector3 position) { }
    public virtual void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition) { }
    public virtual void LeftClick(Vector3 worldPosition) { }
    public virtual void DoubleClick(Vector3 position) { }

    //Dragging
    public virtual void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition) { }
    public virtual void Dragging(Vector3 dragStartPosition, Vector3 currentPosition) { }
    public virtual void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition) { }

    //Event handling
    public virtual void HandleKeyboardEvents() { }
    public virtual void HandleToolbarInput(FSM.ToolbarInput toolbarInput) { }
    public virtual void HandleEntityTypeChange(List<EntityType> newTypes) { }
    public virtual void HandleTeamChange(int newteam) { }
    public virtual void HandleParameterChange(EntityPropertyMetaData parameter, string newValue) { }
    public virtual void Abort() { }
    public virtual void HandleCameraZoomChanged() { }

    protected void HoveredSubEntity(SubEntity subEntity, bool hover)
    {
        if (hover)
            subEntity.RedrawGameObject(SubEntityDrawMode.Hover);
        else
            subEntity.RedrawGameObject(SubEntityDrawMode.Default);
        subEntity.SetInFrontOfLayer(hover);
    }
}
