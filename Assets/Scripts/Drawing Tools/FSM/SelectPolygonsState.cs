using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SelectPolygonsState : FSMState
	{
		private PolygonLayer m_baseLayer;
		private PlanLayer m_planLayer;

		private bool m_selectingBox = false;
		HashSet<PolygonSubEntity> m_currentBoxSelection = new HashSet<PolygonSubEntity>();

		private PolygonSubEntity m_previousHover = null;
		public override EEditingStateType StateType => EEditingStateType.Edit;

		public SelectPolygonsState(FSM a_fsm, PlanLayer a_planLayer) : base(a_fsm)
		{
			m_planLayer = a_planLayer;
			m_baseLayer = a_planLayer.BaseLayer as PolygonLayer;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetCreateMode(false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, false);
			gt.SetActivePlanWindowInteractability(false);

			PolygonSubEntity hover = m_baseLayer.GetSubEntityAt(a_currentMousePosition) as PolygonSubEntity;

			if (hover != null)
			{
				HoveredSubEntity(hover, true);
			}

			m_previousHover = hover;
		}

		public override void LeftClick(Vector3 a_worldPosition)
		{
			PolygonSubEntity hover = m_baseLayer.GetSubEntityAt(a_worldPosition) as PolygonSubEntity;

			if (hover == null)
				return;
			if (m_baseLayer.IsEnergyPolyLayer())
				m_fsm.SetCurrentState(new EditEnergyPolygonState(m_fsm, m_planLayer, new HashSet<PolygonSubEntity>() { hover }));
			else
				m_fsm.SetCurrentState(new EditPolygonsState(m_fsm, m_planLayer, new HashSet<PolygonSubEntity>() { hover }));
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			if (m_selectingBox)
				return;
			PolygonSubEntity hover = null;
			if (!a_cursorIsOverUI)
			{
				hover = m_baseLayer.GetSubEntityAt(a_currentPosition) as PolygonSubEntity;
				if (hover == null && m_baseLayer != null) { hover = m_baseLayer.GetSubEntityAt(a_currentPosition) as PolygonSubEntity; }
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
			m_currentBoxSelection = new HashSet<PolygonSubEntity>();

			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			UpdateBoxSelection(a_dragStartPosition, a_currentPosition);
		}

		private void UpdateBoxSelection(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);

			HashSet<PolygonSubEntity> selectionsInBox = m_baseLayer.GetSubEntitiesInBox(a_dragStartPosition, a_currentPosition);

			foreach (PolygonSubEntity selectionInBox in selectionsInBox)
			{
				if (!m_currentBoxSelection.Contains(selectionInBox)) { HoveredSubEntity(selectionInBox, true); }
			}

			foreach (PolygonSubEntity currentlySelected in m_currentBoxSelection)
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
				if (m_baseLayer.IsEnergyPolyLayer())
					m_fsm.SetCurrentState(new EditEnergyPolygonState(m_fsm, m_planLayer, m_currentBoxSelection));
				else
					m_fsm.SetCurrentState(new EditPolygonsState(m_fsm, m_planLayer, m_currentBoxSelection));
			}

			BoxSelect.HideBoxSelection();
			m_selectingBox = false;
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				m_fsm.SetCurrentState(new StartCreatingPolygonState(m_fsm, m_planLayer));
			}
		}

		public override void HandleToolbarInput(FSM.ToolbarInput a_toolbarInput)
		{
			switch (a_toolbarInput)
			{
				case FSM.ToolbarInput.Create:
					m_fsm.SetCurrentState(new StartCreatingPolygonState(m_fsm, m_planLayer));
					break;
			}
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			if (m_previousHover != null)
			{
				HoveredSubEntity(m_previousHover, false);
			}

			foreach (PolygonSubEntity pse in m_currentBoxSelection)
			{
				pse.RedrawGameObject();
			}

			BoxSelect.HideBoxSelection();
		}
	}
}
