using UnityEngine;
using System.Collections.Generic;

public class LayerProbeState : FSMState
{
    FSM.CursorType previousCursorType;
    MapScaleToolButton stateToggle;

    public LayerProbeState(FSM fsm, MapScaleToolButton stateToggle) : base(fsm)
    {
        this.stateToggle = stateToggle;
    }

    public override void EnterState(Vector3 currentMousePosition)
    {
        base.EnterState(currentMousePosition);

        //Cache previous cursor & Set cursor
        previousCursorType = fsm.CurrentCursorType;
        fsm.SetCursor(FSM.CursorType.LayerProbe);
        stateToggle.SetSelected(true);
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        base.ExitState(currentMousePosition);
        fsm.SetCursor(previousCursorType);
        stateToggle.SetSelected(false);
    }

    public override void LeftMouseButtonDown(Vector3 position)
    {
        List<SubEntity> subEntities = new List<SubEntity>();
        List<AbstractLayer> loadedLayers = LayerManager.GetVisibleLayersSortedByDepth(); // change this back to loaded layers by depth, for the layerprobe

        foreach (AbstractLayer layer in loadedLayers)
        {
            if (!layer.Selectable) { continue; }

            foreach (SubEntity entity in layer.GetSubEntitiesAt(position))
            {
                if (entity.planState != SubEntityPlanState.NotShown)
                    subEntities.Add(entity);
            }
        }

        Vector3 windowPosition = Input.mousePosition;
        windowPosition.y -= Screen.height;

        if(subEntities.Count > 0)
            UIManager.CreateLayerProbeWindow(subEntities, position, windowPosition);
        fsm.SetInterruptState(null);
    }

}

