using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ZoomToAreaState : FSMState
{
    FSM.CursorType previousCursorType;
    MapScaleToolButton stateToggle;

    public ZoomToAreaState(FSM fsm, MapScaleToolButton mapScaleToolButton) : base(fsm)
    {
        this.stateToggle = mapScaleToolButton;
    }

    public override void EnterState(Vector3 currentMousePosition)
    {
        base.EnterState(currentMousePosition);

        //Cache previous cursor & Set cursor
        previousCursorType = fsm.CurrentCursorType;
        fsm.SetCursor(FSM.CursorType.ZoomToArea);
        stateToggle.SetSelected(true);
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        base.ExitState(currentMousePosition);
        BoxSelect.HideBoxSelection();
        fsm.SetCursor(previousCursorType);
        stateToggle.SetSelected(false);
    }

    public override void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);
    }

    public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        float xmin, width, ymin, height;
        if (dragStartPosition.x < dragFinalPosition.x)
        {
            xmin = dragStartPosition.x;
            width = Mathf.Max(1f, dragFinalPosition.x - dragStartPosition.x);
        }
        else
        {
            xmin = dragFinalPosition.x;
            width = Mathf.Max(1f, dragStartPosition.x - dragFinalPosition.x);
        }
        if (dragStartPosition.y < dragFinalPosition.y)
        {
            ymin = dragStartPosition.y;
            height = Mathf.Max(1f, dragFinalPosition.y - dragStartPosition.y);
        }
        else
        {
            ymin = dragFinalPosition.y;
            height = Mathf.Max(1f, dragStartPosition.y - dragFinalPosition.y);
        }

        CameraManager.Instance.ZoomToBounds(new Rect(xmin, ymin, width, height), 1f);
        fsm.SetInterruptState(null);
    }

}

