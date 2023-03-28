using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class FSMState
	{
		public enum EEditingStateType {Create, Edit, Other}
		protected FSM m_fsm;

		protected FSMState(FSM a_fsm)
		{
			m_fsm = a_fsm;
		}

		//State meta
		public virtual void EnterState(Vector3 a_currentMousePosition)
		{
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonActive(FSM.ToolbarInput.ChangeDirection, false);
		}
		public virtual void ExitState(Vector3 a_currentMousePosition) { }
		public virtual EEditingStateType StateType => EEditingStateType.Other;

		//Mouse input
		public virtual void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI) { }
		public virtual void LeftMouseButtonDown(Vector3 a_position) { }
		public virtual void LeftMouseButtonUp(Vector3 a_startPosition, Vector3 a_finalPosition) { }
		public virtual void LeftClick(Vector3 a_worldPosition) { }
		public virtual void DoubleClick(Vector3 a_position) { }

		//Dragging
		public virtual void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition) { }
		public virtual void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition) { }
		public virtual void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition) { }

		//Event handling
		public virtual void HandleKeyboardEvents() { }
		public virtual void HandleToolbarInput(FSM.ToolbarInput a_toolbarInput) { }
		public virtual void HandleEntityTypeChange(List<EntityType> a_newTypes) { }
		public virtual void HandleTeamChange(int a_newteam) { }
		public virtual void HandleParameterChange(EntityPropertyMetaData a_parameter, string a_newValue) { }
		public virtual void Abort() { }
		public virtual void HandleCameraZoomChanged() { }

		protected void HoveredSubEntity(SubEntity a_subEntity, bool a_hover)
		{
			if (a_hover)
				a_subEntity.RedrawGameObject(SubEntityDrawMode.Hover);
			else
				a_subEntity.RedrawGameObject(SubEntityDrawMode.Default);
			a_subEntity.SetInFrontOfLayer(a_hover);
		}
	}
}
