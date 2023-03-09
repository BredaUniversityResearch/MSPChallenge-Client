using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class CreatingLineStringState : FSMState
	{
		protected LineStringSubEntity subEntity;
		protected PlanLayer planLayer;
		public override EEditingStateType StateType => EEditingStateType.Create;
		public CreatingLineStringState(FSM fsm, PlanLayer planLayer, LineStringSubEntity subEntity) : base(fsm)
		{
			this.subEntity = subEntity;
			this.planLayer = planLayer;
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

			int pointCount = subEntity.GetPointCount();
			subEntity.SetPointPosition(pointCount - 1, subEntity.GetPointPosition(pointCount - 2));

			LineStringLayer layer = (LineStringLayer)subEntity.m_entity.Layer;
			if (layer.Entities.Contains(subEntity.m_entity as LineStringEntity))
			{
				subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			}
			else
			{
				layer.RestoreSubEntity(subEntity);
				subEntity.DrawGameObject(layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			}

			m_fsm.SetCursor(FSM.CursorType.Add);
			m_fsm.SetSnappingEnabled(true);

			IssueManager.Instance.SetIssueInteractability(false);
		}

		public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
		{
			Vector3 snappingPoint;
			bool drawAsInvalid;
			if (ClickingWouldFinishDrawing(currentPosition, out snappingPoint, out drawAsInvalid))
			{
				subEntity.SetPointPosition(subEntity.GetPointCount() - 1, snappingPoint);
				if (drawAsInvalid)
				{
					subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
					m_fsm.SetCursor(FSM.CursorType.Invalid);
				}
				else
				{
					subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
					m_fsm.SetCursor(FSM.CursorType.Complete);
				}
			}
			else
			{
				subEntity.SetPointPosition(subEntity.GetPointCount() - 1, snappingPoint);
				if (drawAsInvalid)
				{
					subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
					m_fsm.SetCursor(FSM.CursorType.Invalid);
				}
				else
				{
					subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
					m_fsm.SetCursor(FSM.CursorType.Add);
				}
			}

			if (cursorIsOverUI)
			{
				m_fsm.SetCursor(FSM.CursorType.Default);
			}
		}

		protected virtual bool ClickingWouldFinishDrawing(Vector3 position, out Vector3 snappingPoint, out bool drawAsInvalid)
		{
			drawAsInvalid = false;
			if (subEntity.GetPointCount() < 3)
			{
				snappingPoint = position;
				return false;
			}

			float closestDistSq;
			int pointClicked = subEntity.GetPointAt(position, out closestDistSq, subEntity.GetPointCount() - 1);

			if (pointClicked != -1)
				snappingPoint = subEntity.GetPointPosition(pointClicked);
			else
				snappingPoint = position;
			return pointClicked == subEntity.GetPointCount() - 2;
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			Vector3 snappingPoint;
			bool drawAsInvalid;
			if (ClickingWouldFinishDrawing(finalPosition, out snappingPoint, out drawAsInvalid))
			{
				m_fsm.AddToUndoStack(new FinalizeLineStringOperation(subEntity, planLayer));
				FinalizeLineString();
				return;
			}

			SubEntityDataCopy dataCopy = subEntity.GetDataCopy();

			subEntity.m_edited = true;
			subEntity.AddPoint(finalPosition);
			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			if (subEntity.GetPointCount() > 1)
				m_fsm.SetCursor(FSM.CursorType.Complete);

			m_fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public override void HandleEntityTypeChange(List<EntityType> newTypes)
		{
			SubEntityDataCopy dataCopy = subEntity.GetDataCopy();

			subEntity.m_edited = true;
			subEntity.m_entity.EntityTypes = newTypes;
			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

			m_fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public override void Abort()
		{
			m_fsm.AddToUndoStack(new RemoveLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
			m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, planLayer));
		}

		public virtual void FinalizeLineString()
		{
			subEntity.RemovePoints(new HashSet<int>() { subEntity.GetPointCount() - 1 });

			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			if (selectedType != null) { subEntity.m_entity.EntityTypes = selectedType; }

			subEntity.m_restrictionNeedsUpdate = true;
			subEntity.UnHideRestrictionArea();
			subEntity.RedrawGameObject(SubEntityDrawMode.Default);

			subEntity = null; // set line string to null so the exit state function doesn't remove it

			m_fsm.TriggerGeometryComplete();
			m_fsm.SetCurrentState(new StartCreatingLineStringState(m_fsm, planLayer));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				m_fsm.AddToUndoStack(new FinalizeLineStringOperation(subEntity, planLayer));
				FinalizeLineString();
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				Abort();
			}
		}

		public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
		{
			switch (toolbarInput)
			{
				case FSM.ToolbarInput.Edit:
				case FSM.ToolbarInput.Abort:
					Abort();
					break;
			}
		}

		public override void ExitState(Vector3 currentMousePosition)
		{
			if (subEntity != null)
			{
				subEntity.m_entity.Layer.RemoveSubEntity(subEntity, false);
				subEntity.RemoveGameObject();
			}

			IssueManager.Instance.SetIssueInteractability(true);
		}
	}
}
