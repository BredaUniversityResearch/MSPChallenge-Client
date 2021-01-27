using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class CreatePointsState : FSMState
{
    protected PointLayer baseLayer;
    protected PlanLayer planLayer;
    private bool showingToolTip = false;

    public CreatePointsState(FSM fsm, PlanLayer planLayer) : base(fsm)
    {
        this.planLayer = planLayer;
        baseLayer = planLayer.BaseLayer as PointLayer;
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

        fsm.SetCursor(FSM.CursorType.Add);
        fsm.SetSnappingEnabled(true);

		IssueManager.instance.SetIssueInteractability(false);
    }

	public override void ExitState(Vector3 currentMousePosition)
	{
		base.ExitState(currentMousePosition);
        if (showingToolTip)
            TooltipManager.HideTooltip();
        IssueManager.instance.SetIssueInteractability(true);
	}

	public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
    {
        if (!cursorIsOverUI)
        {
            fsm.SetCursor(FSM.CursorType.Add);
            if (!showingToolTip)
            {
                List<EntityType> entityTypes = UIManager.GetCurrentEntityTypeSelection();
                StringBuilder sb = new StringBuilder("Creating: " + entityTypes[0].Name);
                for (int i = 1; i < entityTypes.Count; i++)
                    sb.Append("\n& " + entityTypes[i].Name);
                TooltipManager.ForceSetToolTip(sb.ToString());
            }
            showingToolTip = true;
        }
        else
        {
            fsm.SetCursor(FSM.CursorType.Default);
            if (showingToolTip)
                TooltipManager.HideTooltip();
            showingToolTip = false;

        }
    }

    public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
    {
        AudioMain.PlaySound(AudioMain.ITEM_PLACED);

        List<EntityType> selectedType = UIManager.GetCurrentEntityTypeSelection();

        PointEntity entity = baseLayer.CreateNewPointEntity(finalPosition, selectedType != null ? selectedType : new List<EntityType>() { baseLayer.EntityTypes.GetFirstValue() }, planLayer);
        baseLayer.activeEntities.Add(entity);
        PointSubEntity subEntity = entity.GetSubEntity(0) as PointSubEntity;
        subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);

        fsm.AddToUndoStack(new CreatePointOperation(subEntity, planLayer));
    }

    public override void HandleKeyboardEvents()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (baseLayer.IsEnergyPointLayer())
                fsm.SetCurrentState(new EditEnergyPointsState(fsm, planLayer));
            else
                fsm.SetCurrentState(new EditPointsState(fsm, planLayer));
        }
    }

    public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
    {
        switch (toolbarInput)
        {
            case FSM.ToolbarInput.Edit:
            case FSM.ToolbarInput.Abort:
                if (baseLayer.IsEnergyPointLayer())
                    fsm.SetCurrentState(new EditEnergyPointsState(fsm, planLayer));
                else
                    fsm.SetCurrentState(new EditPointsState(fsm, planLayer));
                break;
        }
    }
}
