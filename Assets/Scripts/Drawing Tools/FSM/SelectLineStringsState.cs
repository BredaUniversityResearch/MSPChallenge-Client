using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SelectLineStringsState : FSMState
	{
		private PlanLayer m_planLayer;
		private LineStringLayer m_baseLayer = null;

		private bool m_selectingBox = false;
		private HashSet<LineStringSubEntity> m_currentBoxSelection = new HashSet<LineStringSubEntity>();

		private LineStringSubEntity m_previousHover = null;
		public override EEditingStateType StateType => EEditingStateType.Edit;

		public SelectLineStringsState(FSM a_fsm, PlanLayer a_planLayer) : base(a_fsm)
		{
			m_planLayer = a_planLayer;
			m_baseLayer = a_planLayer.BaseLayer as LineStringLayer;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetCreateMode(false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, false);
			gt.SetActivePlanWindowInteractability(false);

			LineStringSubEntity hover = m_baseLayer.GetSubEntityAt(a_currentMousePosition) as LineStringSubEntity;

			if (hover != null)
			{
				HoveredSubEntity(hover, true);
			}

			m_previousHover = hover;
		}

		public override void LeftClick(Vector3 a_worldPosition)
		{
			LineStringSubEntity hover = m_baseLayer.GetSubEntityAt(a_worldPosition) as LineStringSubEntity;

			if (hover == null)
				return;
			if (m_baseLayer.IsEnergyLineLayer())
				m_fsm.SetCurrentState(new EditEnergyLineStringsState(m_fsm, m_planLayer, new HashSet<LineStringSubEntity>() { hover }));
			else
				m_fsm.SetCurrentState(new EditLineStringsState(m_fsm, m_planLayer, new HashSet<LineStringSubEntity>() { hover }));
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			if (m_selectingBox)
				return;
			LineStringSubEntity hover = null;
			if (!a_cursorIsOverUI)
			{
				hover = m_baseLayer.GetSubEntityAt(a_currentPosition) as LineStringSubEntity;
				if (hover == null && m_baseLayer != null) { hover = m_baseLayer.GetSubEntityAt(a_currentPosition) as LineStringSubEntity; }
			}

			if (m_previousHover != null || hover != null)
			{
				if (m_previousHover != null)
				{
					HoveredSubEntity(m_previousHover, false);
				}

				if (hover != null)
				{
					HoveredSubEntity(hover, true);
				}
			}

			m_previousHover = hover;
		}

		public override void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			m_selectingBox = true;
			m_currentBoxSelection = new HashSet<LineStringSubEntity>();

			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			UpdateBoxSelection(a_dragStartPosition, a_currentPosition);
		}

		private void UpdateBoxSelection(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);

			HashSet<LineStringSubEntity> selectionsInBox = m_baseLayer.GetSubEntitiesInBox(a_dragStartPosition, a_currentPosition);

			foreach (LineStringSubEntity selectionInBox in selectionsInBox)
			{
				if (!m_currentBoxSelection.Contains(selectionInBox)) { HoveredSubEntity(selectionInBox, true); }
			}

			foreach (LineStringSubEntity currentlySelected in m_currentBoxSelection)
			{
				if (!selectionsInBox.Contains(currentlySelected)) { HoveredSubEntity(currentlySelected, false); }
			}

			m_currentBoxSelection = selectionsInBox;
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			UpdateBoxSelection(a_dragStartPosition, a_dragFinalPosition);

			if (m_currentBoxSelection.Count > 0)
			{
				if (m_baseLayer.IsEnergyLineLayer())
					m_fsm.SetCurrentState(new EditEnergyLineStringsState(m_fsm, m_planLayer, m_currentBoxSelection));
				else
					m_fsm.SetCurrentState(new EditLineStringsState(m_fsm, m_planLayer, m_currentBoxSelection));
			}

			BoxSelect.HideBoxSelection();
			m_selectingBox = false;
		}

		public override void HandleKeyboardEvents()
		{
			if (!Input.GetKeyDown(KeyCode.Return))
				return;
			if (m_baseLayer.IsEnergyLineLayer())
				m_fsm.SetCurrentState(new StartCreatingEnergyLineStringState(m_fsm, m_planLayer));
			else
				m_fsm.SetCurrentState(new StartCreatingLineStringState(m_fsm, m_planLayer));
		}

		public override void HandleToolbarInput(FSM.ToolbarInput a_toolbarInput)
		{
			switch (a_toolbarInput)
			{
				case FSM.ToolbarInput.Create:
					if (m_baseLayer.IsEnergyLineLayer())
						m_fsm.SetCurrentState(new StartCreatingEnergyLineStringState(m_fsm, m_planLayer));
					else
						m_fsm.SetCurrentState(new StartCreatingLineStringState(m_fsm, m_planLayer));
					break;
			}
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			if (m_previousHover != null)
			{
				HoveredSubEntity(m_previousHover, false);
			}

			foreach (LineStringSubEntity lse in m_currentBoxSelection)
			{
				lse.RedrawGameObject();
			}

			BoxSelect.HideBoxSelection();
		}
	}
}
