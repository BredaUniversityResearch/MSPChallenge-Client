﻿using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class CreatePointsState : FSMState
	{
		protected PointLayer baseLayer;
		protected PlanLayer planLayer;
		private bool showingToolTip = false;

		public override EEditingStateType StateType => EEditingStateType.Create;

		public CreatePointsState(FSM fsm, PlanLayer planLayer) : base(fsm)
		{
			this.planLayer = planLayer;
			baseLayer = planLayer.BaseLayer as PointLayer;
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

			m_fsm.SetCursor(FSM.CursorType.Add);
			m_fsm.SetSnappingEnabled(true);

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
				m_fsm.SetCursor(FSM.CursorType.Add);
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
				m_fsm.SetCursor(FSM.CursorType.Default);
				if (showingToolTip)
					TooltipManager.HideTooltip();
				showingToolTip = false;

			}
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();

			PointEntity entity = baseLayer.CreateNewPointEntity(finalPosition, selectedType != null ? selectedType : new List<EntityType>() { baseLayer.m_entityTypes.GetFirstValue() }, planLayer);
			baseLayer.m_activeEntities.Add(entity);
			PointSubEntity subEntity = entity.GetSubEntity(0) as PointSubEntity;
			subEntity.m_edited = true;
			subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);

			m_fsm.TriggerGeometryComplete();
			m_fsm.AddToUndoStack(new CreatePointOperation(subEntity, planLayer));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				if (baseLayer.IsEnergyPointLayer())
					m_fsm.SetCurrentState(new EditEnergyPointsState(m_fsm, planLayer));
				else
					m_fsm.SetCurrentState(new EditPointsState(m_fsm, planLayer));
			}
		}

		public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
		{
			switch (toolbarInput)
			{
				case FSM.ToolbarInput.Edit:
				case FSM.ToolbarInput.Abort:
					if (baseLayer.IsEnergyPointLayer())
						m_fsm.SetCurrentState(new EditEnergyPointsState(m_fsm, planLayer));
					else
						m_fsm.SetCurrentState(new EditPointsState(m_fsm, planLayer));
					break;
			}
		}
	}
}
