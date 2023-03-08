using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class StartCreatingLineStringState : FSMState
	{
		protected LineStringLayer m_baseLayer;
		protected PlanLayer m_planLayer;
		protected bool m_showingToolTip = false;
		public override EEditingStateType StateType => EEditingStateType.Create;

		public StartCreatingLineStringState(FSM a_fsm, PlanLayer a_planLayer) : base(a_fsm)
		{
			m_planLayer = a_planLayer;
			m_baseLayer = a_planLayer.BaseLayer as LineStringLayer;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetCreateMode(true);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, false);
			//ic.ToolbarEnable(true, FSM.ToolbarInput.Abort);
			gt.SetTeamAndTypeToBasicIfEmpty();
			gt.SetActivePlanWindowInteractability(true);

			m_fsm.SetCursor(FSM.CursorType.Add);
			m_fsm.SetSnappingEnabled(true);

			IssueManager.Instance.SetIssueInteractability(false);
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			base.ExitState(a_currentMousePosition);
			if (m_showingToolTip)
				TooltipManager.HideTooltip();
			IssueManager.Instance.SetIssueInteractability(true);
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			if (!a_cursorIsOverUI)
			{
				m_fsm.SetCursor(FSM.CursorType.Add);
				if (!m_showingToolTip)
				{
					List<EntityType> entityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
					StringBuilder sb = new StringBuilder("Creating: " + entityTypes[0].Name);
					for (int i = 1; i < entityTypes.Count; i++)
						sb.Append("\n& " + entityTypes[i].Name);
					TooltipManager.ForceSetToolTip(sb.ToString());
				}
				m_showingToolTip = true;
			}
			else
			{
				m_fsm.SetCursor(FSM.CursorType.Default);
				if(m_showingToolTip)
					TooltipManager.HideTooltip();
				m_showingToolTip = false;

			}
		}

		public override void LeftMouseButtonUp(Vector3 a_startPosition, Vector3 a_finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			LineStringEntity entity = m_baseLayer.CreateNewLineStringEntity(a_finalPosition, new List<EntityType>() { m_baseLayer.m_entityTypes.GetFirstValue() }, m_planLayer);
			m_baseLayer.m_activeEntities.Add(entity);
			entity.EntityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			LineStringSubEntity subEntity = entity.GetSubEntity(0) as LineStringSubEntity;

			entity.DrawGameObjects(m_baseLayer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			subEntity.m_edited = true;
			m_fsm.SetCurrentState(new CreatingLineStringState(m_fsm, m_planLayer, subEntity));

			m_fsm.AddToUndoStack(new CreateLineStringOperation(subEntity, m_planLayer, UndoOperation.EditMode.Create));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
			}
		}

		public override void HandleToolbarInput(FSM.ToolbarInput a_toolbarInput)
		{
			switch (a_toolbarInput)
			{
				case FSM.ToolbarInput.Edit:
				case FSM.ToolbarInput.Abort:
					m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
					break;
			}
		}
	}
}
