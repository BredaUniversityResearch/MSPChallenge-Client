using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class CreatingPolygonState : FSMState
	{
		private PolygonSubEntity m_subEntity;
		private PlanLayer m_planLayer;
		public override EEditingStateType StateType => EEditingStateType.Create;
		public CreatingPolygonState(FSM a_fsm, PolygonSubEntity a_subEntity, PlanLayer a_planLayer) : base(a_fsm)
		{
			m_subEntity = a_subEntity;
			m_planLayer = a_planLayer;
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

			int pointCount = m_subEntity.GetPolygonPointCount();
			m_subEntity.SetPointPosition(pointCount - 1, m_subEntity.GetPointPosition(pointCount - 2), true);

			if ((m_subEntity.m_entity.Layer as PolygonLayer).m_activeEntities.Contains(m_subEntity.m_entity as PolygonEntity))
			{
				m_subEntity.DrawGameObject(m_subEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			}
			else
			{
				(m_subEntity.m_entity.Layer as PolygonLayer).RestoreSubEntity(m_subEntity);
				m_subEntity.DrawGameObject(m_subEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			}

			m_fsm.SetCursor(FSM.CursorType.Add);
			m_fsm.SetSnappingEnabled(true);

			IssueManager.Instance.SetIssueInteractability(false);
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			bool finishing = ClickingWouldFinishDrawing(a_currentPosition);
			m_subEntity.SetPointPosition(m_subEntity.GetPolygonPointCount() - 1, a_currentPosition, true);
			m_subEntity.PerformNewSegmentValidityCheck(finishing);

			if (finishing)
				m_subEntity.SetPointPosition(m_subEntity.GetPolygonPointCount() - 1, m_subEntity.GetPointPosition(0), true);

			m_subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

			if (a_cursorIsOverUI)
				m_fsm.SetCursor(FSM.CursorType.Default);       
			else if(m_subEntity.InvalidPoints != null)
				m_fsm.SetCursor(FSM.CursorType.Invalid);
			else if(finishing)
				m_fsm.SetCursor(FSM.CursorType.Complete);
			else
				m_fsm.SetCursor(FSM.CursorType.Add);
		}

		private bool ClickingWouldFinishDrawing(Vector3 a_position)
		{
			int points = m_subEntity.GetPolygonPointCount();
			if (points < 4)
				return false; 

			//Get closest, ignoring the last point
			float closestDistSq;
			int pointClicked = m_subEntity.GetPointAt(a_position, out closestDistSq, true);

			return pointClicked == 0 || pointClicked == points - 2;
		}

		public override void LeftMouseButtonUp(Vector3 a_startPosition, Vector3 a_finalPosition)
		{
			bool finishing = ClickingWouldFinishDrawing(a_finalPosition);
			m_subEntity.PerformNewSegmentValidityCheck(finishing);

			if (m_subEntity.InvalidPoints != null)
				return;
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);
			if (finishing)
			{
				m_fsm.AddToUndoStack(new FinalizePolygonOperation(m_subEntity, m_planLayer));
				FinalizePolygon();
				return;
			}

			SubEntityDataCopy dataCopy = m_subEntity.GetDataCopy();

			m_subEntity.AddPoint(a_finalPosition);
			m_subEntity.m_edited = true;
			m_subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			if (m_subEntity.GetPolygonPointCount() > 2)
				m_fsm.SetCursor(FSM.CursorType.Complete);

			m_fsm.AddToUndoStack(new ModifyPolygonOperation(m_subEntity, m_planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public override void HandleEntityTypeChange(List<EntityType> a_newTypes)
		{
			SubEntityDataCopy dataCopy = m_subEntity.GetDataCopy();

			m_subEntity.m_entity.EntityTypes = a_newTypes;
			m_subEntity.m_edited = true;
			m_subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

			m_fsm.AddToUndoStack(new ModifyPolygonOperation(m_subEntity, m_planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public override void Abort()
		{
			m_fsm.AddToUndoStack(new RemovePolygonOperation(m_subEntity, m_planLayer, UndoOperation.EditMode.Create));
			m_fsm.SetCurrentState(new SelectPolygonsState(m_fsm, m_planLayer));
		}

		public void FinalizePolygon()
		{
			m_subEntity.RemovePoints(new HashSet<int>() { m_subEntity.GetPolygonPointCount() - 1 });

			m_subEntity.PerformValidityCheck(false);
			
			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();

			if (selectedType != null) { m_subEntity.m_entity.EntityTypes = selectedType; }

			m_subEntity.m_restrictionNeedsUpdate = true;
			m_subEntity.UnHideRestrictionArea();
			m_subEntity.RedrawGameObject(SubEntityDrawMode.Default);

			m_subEntity = null; // set polygon to null so the exit state function doesn't remove it

			m_fsm.TriggerGeometryComplete();
			m_fsm.SetCurrentState(new StartCreatingPolygonState(m_fsm, m_planLayer));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				if (m_subEntity.GetPolygonPointCount() < 4)
					DialogBoxManager.instance.NotificationWindow("Couldn't complete polygon", "A polygon needs at least 3 points to be completed.", null);
				else
				{
					m_subEntity.PerformNewSegmentValidityCheck(true);
					if (m_subEntity.InvalidPoints == null)
					{
						m_fsm.AddToUndoStack(new FinalizePolygonOperation(m_subEntity, m_planLayer));
						FinalizePolygon();
					}
					else
						DialogBoxManager.instance.NotificationWindow("Couldn't complete polygon", "Completing the polygon in its current state would cause self-intersection. The action was canceled.", null);
				}
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				Abort();
			}
		}

		public override void HandleToolbarInput(FSM.ToolbarInput a_toolbarInput)
		{
			switch (a_toolbarInput)
			{
				case FSM.ToolbarInput.Edit:
				case FSM.ToolbarInput.Abort:
					Abort();
					break;
			}
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			if (m_subEntity != null)
			{
				(m_subEntity.m_entity.Layer as PolygonLayer).RemoveSubEntity(m_subEntity, false);
				m_subEntity.RemoveGameObject();
			}

			IssueManager.Instance.SetIssueInteractability(true);
		}
	}
}
