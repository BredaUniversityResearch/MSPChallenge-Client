using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EditPointsState : FSMState
	{
		protected PointLayer m_baseLayer;
		protected PlanLayer m_planLayer;

		protected bool m_selectedRemovedEntity = false;
		protected bool m_draggingSelection = false;
		protected Dictionary<PointSubEntity, Vector3> m_selectionDragStart = null;

		protected bool m_selectingBox = false;
		protected HashSet<PointSubEntity> m_currentBoxSelection = null;

		protected HashSet<PointSubEntity> m_selection = new HashSet<PointSubEntity>();

		private PointSubEntity m_previousHover = null;

		protected static HashSet<int> m_firstPoint = new HashSet<int>() { 0 };
		public override EEditingStateType StateType => EEditingStateType.Edit;
		public override bool HasGeometrySelected => m_selection != null && m_selection.Count > 0;


		public EditPointsState(FSM a_fsm, PlanLayer a_planLayer) : base(a_fsm)
		{
			m_planLayer = a_planLayer;
			m_baseLayer = a_planLayer.BaseLayer as PointLayer;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetCreateMode(false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, false);
			//ic.ToolbarEnable(true, FSM.ToolbarInput.Abort);
			gt.SetActivePlanWindowInteractability(false);

			PointSubEntity hover = m_baseLayer.GetPointAt(a_currentMousePosition);

			if (hover != null)
			{
				hover.RedrawGameObject(SubEntityDrawMode.Default, null, m_firstPoint);
			}

			m_previousHover = hover;

			m_fsm.SetSnappingEnabled(true);
			IssueManager.Instance.SetIssueInteractability(false);
		}

		public override void LeftClick(Vector3 a_worldPosition)
		{
			PointSubEntity point = m_baseLayer.GetPointAt(a_worldPosition);
			if (point == null && m_baseLayer != null) { point = m_baseLayer.GetPointAt(a_worldPosition); }

			if (point != null)
			{
				Select(new HashSet<PointSubEntity>() { point }, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
			}
			else
			{
				Select(new HashSet<PointSubEntity>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
			}
		}

		protected void Select(HashSet<PointSubEntity> a_newSelection, bool a_keepPreviousSelection)
		{
			if (!a_keepPreviousSelection)
			{
				foreach (PointSubEntity pse in m_selection)
				{
					pse.RedrawGameObject(SubEntityDrawMode.Default, null, null);
				}
				m_selection = a_newSelection;
			}
			else if(!m_selectedRemovedEntity)
			{
				m_selection.UnionWith(a_newSelection);
			}

			m_selectedRemovedEntity = false;
			foreach (PointSubEntity pse in a_newSelection)
			{
				if (pse.IsPlannedForRemoval())
					m_selectedRemovedEntity = true;
				pse.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);
			}

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, m_selection.Count > 0);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, m_selectedRemovedEntity);
			gt.SetObjectChangeInteractable(!m_selectedRemovedEntity);

			//Points have no selecting state, so dropdown interactivity can change while in this state
			if (m_selection.Count == 0)
			{
				gt.SetActivePlanWindowInteractability(false);
				return;
			}

			UpdateActivePlanWindowToSelection();
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			if (m_draggingSelection || m_selectingBox)
				return;
			PointSubEntity hover = null;
			if (!a_cursorIsOverUI)
			{
				hover = m_baseLayer.GetPointAt(a_currentPosition);
				if (hover == null && m_baseLayer != null) { hover = m_baseLayer.GetPointAt(a_currentPosition); }
			}

			if (m_previousHover != hover)
			{
				if (m_previousHover != null)
				{
					if (m_selection.Contains(m_previousHover)) { m_previousHover.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null); }
					else { m_previousHover.RedrawGameObject(); }
				}

				if (hover != null)
				{
					if (m_selection.Contains(hover)) { hover.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, m_firstPoint); }
					else { hover.RedrawGameObject(SubEntityDrawMode.Default, null, m_firstPoint); }
				}
			}

			m_previousHover = hover;

			if (hover == null)
			{
				m_fsm.SetCursor(FSM.CursorType.Default);
			}
			else
			{
				m_fsm.SetCursor(FSM.CursorType.Move);
			}
		}

		protected virtual PointSubEntity CreateNewPlanPoint(Vector3 a_point, List<EntityType> a_entityType, int a_persistentID, Dictionary<string, string> a_metaData, int a_country)
		{
			PointEntity newEntity = m_baseLayer.CreateNewPointEntity(a_point, a_entityType, m_planLayer);
			newEntity.metaData = new Dictionary<string, string>(a_metaData);
			newEntity.Country = a_country;
			PointSubEntity newSubEntity = newEntity.GetSubEntity(0) as PointSubEntity;
			newSubEntity.SetPersistentID(a_persistentID);
			newSubEntity.m_edited = true;
			m_fsm.AddToUndoStack(new CreatePointOperation(newSubEntity, m_planLayer, true));
			newSubEntity.DrawGameObject(m_baseLayer.LayerGameObject.transform);
			return newSubEntity;
		}

		protected void SwitchSelectionFromBasePointToDuplicate(PointSubEntity a_basePoint, PointSubEntity a_duplicate)
		{
			m_selection.Add(a_duplicate);
			m_selection.Remove(a_basePoint);

			//Change active geom 
			m_baseLayer.AddPreModifiedEntity(a_basePoint.m_entity);
			m_baseLayer.m_activeEntities.Remove(a_basePoint.m_entity as PointEntity);
			m_baseLayer.m_activeEntities.Add(a_duplicate.m_entity as PointEntity);


			//Redraw based on activity changes
			a_basePoint.RedrawGameObject();
			a_duplicate.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint);
		}

		protected virtual PointSubEntity StartModifyingSubEntity(PointSubEntity a_subEntity, bool a_insideUndoBatch)
		{
			if (a_subEntity.m_entity.PlanLayer == m_planLayer)
			{
				m_fsm.AddToUndoStack(new ModifyPointOperation(a_subEntity, m_planLayer, a_subEntity.GetDataCopy()));
				a_subEntity.m_edited = true;
			}
			else
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				PointSubEntity duplicate = CreateNewPlanPoint(a_subEntity.GetPosition(), a_subEntity.m_entity.EntityTypes, a_subEntity.GetPersistentID(), a_subEntity.m_entity.metaData, a_subEntity.m_entity.Country);
				SwitchSelectionFromBasePointToDuplicate(a_subEntity, duplicate);
				a_subEntity = duplicate;

				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
			}
			return a_subEntity;
		}

		private void CreateUndoForDraggedSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			HashSet<PointSubEntity> selectionCopy = new HashSet<PointSubEntity>(m_selection);
			foreach (PointSubEntity subEntity in selectionCopy)
			{
				StartModifyingSubEntity(subEntity, true);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_selectedRemovedEntity)
				return;
			PointSubEntity draggingPoint = m_baseLayer.GetPointAt(a_dragStartPosition);
			if (draggingPoint != null && !m_selection.Contains(draggingPoint))
			{
				Select(new HashSet<PointSubEntity> { draggingPoint }, false);
				if (m_selectedRemovedEntity)
					return;
			}

			if (draggingPoint != null)
			{
				m_draggingSelection = true;
				CreateUndoForDraggedSelection();

				// this offset is used to make sure the user is dragging the center of the point that is being dragged (to make snapping work correctly)
				Vector3 offset = a_dragStartPosition - draggingPoint.GetPosition();

				m_selectionDragStart = new Dictionary<PointSubEntity, Vector3>();
				foreach (PointSubEntity pse in m_selection)
				{
					m_selectionDragStart.Add(pse, pse.GetPosition() + offset);
				}
			}
			else
			{
				Select(new HashSet<PointSubEntity>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				m_selectingBox = true;
				m_currentBoxSelection = new HashSet<PointSubEntity>();

				BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);
			}
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_selectedRemovedEntity)
				return;
			if (m_draggingSelection)
			{
				Vector3 offset = a_currentPosition - a_dragStartPosition;
				foreach (PointSubEntity subEntity in m_selection)
				{
					subEntity.SetPosition(m_selectionDragStart[subEntity] + offset);
					subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);
				}
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_currentPosition);
			}
		}

		protected void UpdateBoxSelection(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);

			HashSet<PointSubEntity> pointsInBox = m_baseLayer.GetPointsInBox(a_dragStartPosition, a_currentPosition);
			//if (baseLayer != null) { pointsInBox.UnionWith(baseLayer.GetPointsInBox(dragStartPosition, currentPosition)); }

			foreach (PointSubEntity pointInBox in pointsInBox)
			{
				if (m_currentBoxSelection.Contains(pointInBox))
					continue;
				if (m_selection.Contains(pointInBox)) { pointInBox.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, m_firstPoint); }
				else { pointInBox.RedrawGameObject(SubEntityDrawMode.Default, null, m_firstPoint); }
			}

			foreach (PointSubEntity selectedPoint in m_currentBoxSelection)
			{
				if (pointsInBox.Contains(selectedPoint))
					continue;
				if (m_selection.Contains(selectedPoint)) { selectedPoint.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null); }
				else { selectedPoint.RedrawGameObject(SubEntityDrawMode.Default, null, null); }
			}

			m_currentBoxSelection = pointsInBox;
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			if (m_draggingSelection)
			{
				AudioMain.Instance.PlaySound(AudioMain.ITEM_MOVED);

				Vector3 offset = a_dragFinalPosition - a_dragStartPosition;
				foreach (PointSubEntity subEntity in m_selection)
				{
					subEntity.SetPosition(m_selectionDragStart[subEntity] + offset);
					subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);
				}
				m_draggingSelection = false;
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_dragFinalPosition);
				Select(m_currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				BoxSelect.HideBoxSelection();
				m_selectingBox = false;
				m_currentBoxSelection = null;
			}
		}

		protected virtual void DeleteSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PointSubEntity subEntity in m_selection)
			{
				if (subEntity.m_entity.PlanLayer == m_planLayer)
				{
					m_fsm.AddToUndoStack(new RemovePointOperation(subEntity, m_planLayer));
					//planLayer.RemovedGeometry.Add(subEntity.GetPersistentID());
					m_baseLayer.RemoveSubEntity(subEntity);
					subEntity.RemoveGameObject();
				}
				else
				{
					m_fsm.AddToUndoStack(new ModifyPointRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
					m_baseLayer.RemoveSubEntity(subEntity);
					subEntity.RedrawGameObject();
				}
			}
			m_selection = new HashSet<PointSubEntity>();

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		private void UndoDeleteForSelection()
		{
			if (!m_selectedRemovedEntity)
				return;
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			foreach (PointSubEntity subEntity in m_selection)
			{
				m_fsm.AddToUndoStack(new ModifyPointRemovalPlanOperation(subEntity, m_planLayer, true));
				m_planLayer.RemovedGeometry.Remove(subEntity.GetPersistentID());
				subEntity.RestoreDependencies();
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);

			}
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			m_selectedRemovedEntity = false;
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Delete))
			{
				DeleteSelection();
			}

			if (!Input.GetKeyDown(KeyCode.Return))
				return;
			if (m_baseLayer.IsEnergyPointLayer())
				m_fsm.SetCurrentState(new CreateEnergyPointState(m_fsm, m_planLayer));
			else
				m_fsm.SetCurrentState(new CreatePointsState(m_fsm, m_planLayer));
		}

		public override void HandleToolbarInput(FSM.ToolbarInput a_toolbarInput)
		{
			switch (a_toolbarInput)
			{
				case FSM.ToolbarInput.Create:
					if (m_baseLayer.IsEnergyPointLayer())
						m_fsm.SetCurrentState(new CreateEnergyPointState(m_fsm, m_planLayer));
					else
						m_fsm.SetCurrentState(new CreatePointsState(m_fsm, m_planLayer));
					break;
				case FSM.ToolbarInput.Delete:
					DeleteSelection();
					break;
				case FSM.ToolbarInput.Abort:
					Select(new HashSet<PointSubEntity>(), false);
					break;
				case FSM.ToolbarInput.Recall:
					UndoDeleteForSelection();
					break;
			}
		}

		public override void HandleEntityTypeChange(List<EntityType> a_newTypes)
		{
			List<PointSubEntity> subEntitiesWithDifferentTypes = new List<PointSubEntity>();

			//Find subentities with changed entity types
			foreach (PointSubEntity subEntity in m_selection)
			{
				if (subEntity.m_entity.EntityTypes.Count != a_newTypes.Count)
				{
					subEntitiesWithDifferentTypes.Add(subEntity);
					continue;
				}
				foreach (EntityType type in subEntity.m_entity.EntityTypes)
				{
					if (a_newTypes.Contains(type))
						continue;
					subEntitiesWithDifferentTypes.Add(subEntity);
					break;
				}
			}

			if (subEntitiesWithDifferentTypes.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PointSubEntity subEntity in subEntitiesWithDifferentTypes)
			{
				PointSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.EntityTypes = a_newTypes;
				subEntityToModify.RedrawGameObject(SubEntityDrawMode.Selected, m_firstPoint, null);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleTeamChange(int a_newTeam)
		{
			List<PointSubEntity> subEntitiesWithDifferentTeam = new List<PointSubEntity>();

			//Find subentities with changed entity types
			foreach (PointSubEntity subEntity in m_selection)
			{
				if (subEntity.m_entity.Country != a_newTeam)
				{
					subEntitiesWithDifferentTeam.Add(subEntity);
				}
			}

			if (subEntitiesWithDifferentTeam.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PointSubEntity subEntity in subEntitiesWithDifferentTeam)
			{
				PointSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.Country = a_newTeam;
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleParameterChange(EntityPropertyMetaData a_parameter, string a_newValue)
		{
			List<PointSubEntity> subEntitiesWithDifferentParams = new List<PointSubEntity>();

			//Find subentities with changed entity types
			foreach (PointSubEntity subEntity in m_selection)
			{
				if (subEntity.m_entity.GetPropertyMetaData(a_parameter) != a_newValue)
				{
					subEntitiesWithDifferentParams.Add(subEntity);
				}
			}

			if (subEntitiesWithDifferentParams.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PointSubEntity subEntity in subEntitiesWithDifferentParams)
			{
				PointSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.SetPropertyMetaData(a_parameter, a_newValue);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleGeometryPolicyChange(EntityPropertyMetaData a_policy, Dictionary<Entity, string> a_newValues)
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			foreach (PointSubEntity subEntity in m_selection)
			{
				if (a_newValues.TryGetValue(subEntity.m_entity, out string value))
				{
					PointSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
					subEntityToModify.m_entity.SetPropertyMetaData(a_policy, value);
				}
			}
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			if (m_previousHover != null)
			{
				m_previousHover.RedrawGameObject(SubEntityDrawMode.Default, null, null);
			}

			foreach (PointSubEntity pse in m_selection)
			{
				pse.RedrawGameObject(SubEntityDrawMode.Default);
			}
			m_selection = new HashSet<PointSubEntity>();

			BoxSelect.HideBoxSelection();
			IssueManager.Instance.SetIssueInteractability(true);
		}

		private void UpdateActivePlanWindowToSelection()
		{
			List<List<EntityType>> selectedEntityTypes = new List<List<EntityType>>();
			int? selectedTeam = null;
			List<Dictionary<EntityPropertyMetaData, string>> selectedParams = new List<Dictionary<EntityPropertyMetaData, string>>();
			List<Entity> entities = new List<Entity>(m_selection.Count);

			foreach (PointSubEntity pse in m_selection)
			{
				selectedEntityTypes.Add(pse.m_entity.EntityTypes);
				if (selectedTeam.HasValue && pse.m_entity.Country != selectedTeam.Value)
					selectedTeam = -1;
				else
					selectedTeam = pse.m_entity.Country;
				Dictionary<EntityPropertyMetaData, string> parameters = new Dictionary<EntityPropertyMetaData, string>();
				foreach (EntityPropertyMetaData p in m_baseLayer.m_propertyMetaData)
				{
					if (p.ShowInEditMode)
						parameters.Add(p, pse.m_entity.GetPropertyMetaData(p));
				}
				selectedParams.Add(parameters);
				entities.Add(pse.m_entity);
			}
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SetToSelection(
				selectedEntityTypes.Count > 0 ? selectedEntityTypes : null,
				selectedTeam ?? -2,
				selectedParams,
				entities);
		}
	}
}
