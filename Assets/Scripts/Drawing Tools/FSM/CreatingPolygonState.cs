using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class CreatingPolygonState : FSMState
	{
		PolygonSubEntity subEntity;
		PlanLayer planLayer;
		public override EEditingStateType StateType => EEditingStateType.Create;
		public CreatingPolygonState(FSM fsm, PolygonSubEntity subEntity, PlanLayer planLayer) : base(fsm)
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

			int pointCount = subEntity.GetPolygonPointCount();
			subEntity.SetPointPosition(pointCount - 1, subEntity.GetPointPosition(pointCount - 2), true);

			if ((subEntity.Entity.Layer as PolygonLayer).m_activeEntities.Contains(subEntity.Entity as PolygonEntity))
			{
				subEntity.DrawGameObject(subEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			}
			else
			{
				(subEntity.Entity.Layer as PolygonLayer).RestoreSubEntity(subEntity);
				subEntity.DrawGameObject(subEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			}

			fsm.SetCursor(FSM.CursorType.Add);
			fsm.SetSnappingEnabled(true);

			IssueManager.Instance.SetIssueInteractability(false);
		}

		public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
		{
			bool finishing = clickingWouldFinishDrawing(currentPosition);
			subEntity.SetPointPosition(subEntity.GetPolygonPointCount() - 1, currentPosition, true);
			subEntity.PerformNewSegmentValidityCheck(finishing);

			if (finishing)
				subEntity.SetPointPosition(subEntity.GetPolygonPointCount() - 1, subEntity.GetPointPosition(0), true);

			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

			if (cursorIsOverUI)
				fsm.SetCursor(FSM.CursorType.Default);       
			else if(subEntity.InvalidPoints != null)
				fsm.SetCursor(FSM.CursorType.Invalid);
			else if(finishing)
				fsm.SetCursor(FSM.CursorType.Complete);
			else
				fsm.SetCursor(FSM.CursorType.Add);
		}

		private bool clickingWouldFinishDrawing(Vector3 position)
		{
			int points = subEntity.GetPolygonPointCount();
			if (points < 4)
				return false; 

			//Get closest, ignoring the last point
			float closestDistSq;
			int pointClicked = subEntity.GetPointAt(position, out closestDistSq, true);

			return pointClicked == 0 || pointClicked == points - 2;
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{
			bool finishing = clickingWouldFinishDrawing(finalPosition);
			subEntity.PerformNewSegmentValidityCheck(finishing);

			if (subEntity.InvalidPoints == null)
			{
				AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);
				if (finishing)
				{
					fsm.AddToUndoStack(new FinalizePolygonOperation(subEntity, planLayer));
					FinalizePolygon();
					return;
				}

				SubEntityDataCopy dataCopy = subEntity.GetDataCopy();

				subEntity.AddPoint(finalPosition);
				subEntity.edited = true;
				subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
				if (subEntity.GetPolygonPointCount() > 2)
					fsm.SetCursor(FSM.CursorType.Complete);

				fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
			}
		}

		public override void HandleEntityTypeChange(List<EntityType> newTypes)
		{
			SubEntityDataCopy dataCopy = subEntity.GetDataCopy();

			subEntity.Entity.EntityTypes = newTypes;
			subEntity.edited = true;
			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

			fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public override void Abort()
		{
			fsm.AddToUndoStack(new RemovePolygonOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
			fsm.SetCurrentState(new SelectPolygonsState(fsm, planLayer));
		}

		public void FinalizePolygon()
		{
			subEntity.RemovePoints(new HashSet<int>() { subEntity.GetPolygonPointCount() - 1 });

			subEntity.PerformValidityCheck(false);
			//if (subEntity is EnergyPolygonSubEntity)
			//    (subEntity as EnergyPolygonSubEntity).FinalizePoly();

			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();

			if (selectedType != null) { subEntity.Entity.EntityTypes = selectedType; }

			subEntity.restrictionNeedsUpdate = true;
			subEntity.UnHideRestrictionArea();
			subEntity.RedrawGameObject(SubEntityDrawMode.Default);

			subEntity = null; // set polygon to null so the exit state function doesn't remove it

			fsm.TriggerGeometryComplete();
			fsm.SetCurrentState(new StartCreatingPolygonState(fsm, planLayer));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				if (subEntity.GetPolygonPointCount() < 4)
					DialogBoxManager.instance.NotificationWindow("Couldn't complete polygon", "A polygon needs at least 3 points to be completed.", null);
				else
				{
					subEntity.PerformNewSegmentValidityCheck(true);
					if (subEntity.InvalidPoints == null)
					{
						fsm.AddToUndoStack(new FinalizePolygonOperation(subEntity, planLayer));
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
				(subEntity.Entity.Layer as PolygonLayer).RemoveSubEntity(subEntity, false);
				subEntity.RemoveGameObject();
			}

			IssueManager.Instance.SetIssueInteractability(true);
		}
	}
}
