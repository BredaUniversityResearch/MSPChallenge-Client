using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class StartCreatingLineStringState : FSMState
	{
		protected LineStringLayer baseLayer;
		protected PlanLayer planLayer;
		protected bool showingToolTip = false;
		public override EEditingStateType StateType => EEditingStateType.Create;

		public StartCreatingLineStringState(FSM fsm, PlanLayer planLayer) : base(fsm)
		{
			this.planLayer = planLayer;
			this.baseLayer = planLayer.BaseLayer as LineStringLayer;
		}

		public override void EnterState(Vector3 currentMousePosition)
		{
			base.EnterState(currentMousePosition);

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetCreateMode(true);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, false);
			//ic.ToolbarEnable(true, FSM.ToolbarInput.Abort);
			gt.SetTeamAndTypeToBasicIfEmpty();
			gt.SetActivePlanWindowInteractability(true);

			fsm.SetCursor(FSM.CursorType.Add);
			fsm.SetSnappingEnabled(true);

			IssueManager.Instance.SetIssueInteractability(false);
		}

		public override void ExitState(Vector3 currentMousePosition)
		{
			base.ExitState(currentMousePosition);
			if (showingToolTip)
				TooltipManager.HideTooltip();
			IssueManager.Instance.SetIssueInteractability(true);
		}

		public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
		{
			if (!cursorIsOverUI)
			{
				fsm.SetCursor(FSM.CursorType.Add);
				if (!showingToolTip)
				{
					List<EntityType> entityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
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
				if(showingToolTip)
					TooltipManager.HideTooltip();
				showingToolTip = false;

			}
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			LineStringEntity entity = baseLayer.CreateNewLineStringEntity(finalPosition, new List<EntityType>() { baseLayer.EntityTypes.GetFirstValue() }, planLayer);
			baseLayer.activeEntities.Add(entity);
			entity.EntityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			LineStringSubEntity subEntity = entity.GetSubEntity(0) as LineStringSubEntity;

			entity.DrawGameObjects(baseLayer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			subEntity.edited = true;
			fsm.SetCurrentState(new CreatingLineStringState(fsm, planLayer, subEntity));

			fsm.AddToUndoStack(new CreateLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
			}
		}

		public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
		{
			switch (toolbarInput)
			{
				case FSM.ToolbarInput.Edit:
				case FSM.ToolbarInput.Abort:
					fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
					break;
			}
		}
	}
}