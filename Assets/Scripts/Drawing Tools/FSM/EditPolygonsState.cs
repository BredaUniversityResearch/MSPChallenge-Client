using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EditPolygonsState : FSMState
	{
		private const float InsertPointDelay = 0.5f;

		//protected PolygonLayer layer;
		private PolygonLayer m_baseLayer;
		protected PlanLayer m_planLayer;

		protected bool m_draggingSelection = false;
		private Dictionary<PolygonSubEntity, Dictionary<int, Vector3>> m_selectionDragStart = null;

		private bool m_selectingBox = false;
		private Dictionary<PolygonSubEntity, HashSet<int>> m_currentBoxSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();

		private HashSet<PolygonSubEntity> m_selectedSubEntities = new HashSet<PolygonSubEntity>();
		protected HashSet<EnergyPointSubEntity> m_selectedSourcePoints = new HashSet<EnergyPointSubEntity>();
		private Dictionary<PolygonSubEntity, HashSet<int>> m_selectedPoints = new Dictionary<PolygonSubEntity, HashSet<int>>();
		private Dictionary<PolygonSubEntity, HashSet<int>> m_highlightedPoints = new Dictionary<PolygonSubEntity, HashSet<int>>(); 

		private bool m_insertingPointsDisabled = true;
		private bool m_selectedRemovedEntity = false;
		private float m_stateEnteredTime = float.MinValue;
		private Vector3 m_reEnableInsertingPointsPosition;

		private PolygonSubEntity m_insertPointPreviewSubEntity = null;
		private int m_insertPointPreviewIndex = -1;
		public override EEditingStateType StateType => EEditingStateType.Edit;

		public EditPolygonsState(FSM a_fsm, PlanLayer a_planLayer, HashSet<PolygonSubEntity> a_selectedSubEntities) : base(a_fsm)
		{
			m_planLayer = a_planLayer;
			m_baseLayer = a_planLayer.BaseLayer as PolygonLayer;
			SetSelectedSubEntities(a_selectedSubEntities);
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			gt.m_toolBar.SetCreateMode(false);
			gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Delete, true);
			gt.SetActivePlanWindowInteractability(true);

			IssueManager.Instance.SetIssueInteractability(false);
		
			foreach (PolygonSubEntity pse in m_selectedSubEntities)
			{
				pse.PerformValidityCheck(false);
				pse.RedrawGameObject(SubEntityDrawMode.Selected);
			}

			UpdateActivePlanWindowToSelection();

			m_insertingPointsDisabled = true;
			m_stateEnteredTime = Time.time;
			m_reEnableInsertingPointsPosition = a_currentMousePosition;

			m_fsm.SetSnappingEnabled(true);
		}

		private void RedrawObject(PolygonSubEntity a_entity)
		{
			SubEntityDrawMode drawMode = m_selectedSubEntities.Contains(a_entity) ? SubEntityDrawMode.Selected : SubEntityDrawMode.Default;
			m_selectedPoints.TryGetValue(a_entity, out HashSet<int> selectedPointsForEntity);
			m_highlightedPoints.TryGetValue(a_entity, out HashSet<int> highlightedPointsForEntity);
			a_entity.RedrawGameObject(drawMode, selectedPointsForEntity, highlightedPointsForEntity);
		}

		private void PreviewInsertPoint(Vector3 a_position)
		{
			GetLineAt(a_position, m_selectedSubEntities, out m_insertPointPreviewSubEntity, out var lineA, out var lineB);
			if (lineA == -1)
				return;
			m_insertPointPreviewIndex = m_insertPointPreviewSubEntity.AddPointBetween(a_position, lineA, lineB);

			m_insertPointPreviewSubEntity.RedrawGameObject(SubEntityDrawMode.Selected, new HashSet<int>() { m_insertPointPreviewIndex }, null);
		}

		private void GetLineAt(Vector3 a_position, HashSet<PolygonSubEntity> a_selectedSubEntities, out PolygonSubEntity a_subEntity, out int a_lineA, out int a_lineB)
		{
			a_lineA = -1;
			a_lineB = -1;
			a_subEntity = null;
			float closestDistanceSquared = float.MaxValue;

			foreach (PolygonSubEntity pse in a_selectedSubEntities)
			{
				pse.GetLineAt(a_position, out var pseLineA, out var pseLineB, out var closestDistSq);
				if (pseLineA == -1 || !(closestDistSq < closestDistanceSquared))
					continue;
				a_lineA = pseLineA;
				a_lineB = pseLineB;
				a_subEntity = pse;
				closestDistanceSquared = closestDistSq;
			}
		}

		private void RemoveInsertPointPreview()
		{
			m_insertPointPreviewSubEntity.RemovePoints(new HashSet<int>() { m_insertPointPreviewIndex });

			HashSet<int> originalSelection = null;
			if (m_selectedPoints.ContainsKey(m_insertPointPreviewSubEntity))
			{
				originalSelection = m_selectedPoints[m_insertPointPreviewSubEntity];
			}
			m_insertPointPreviewSubEntity.RedrawGameObject(SubEntityDrawMode.Selected, originalSelection, null);
			m_insertPointPreviewSubEntity = null;
			m_insertPointPreviewIndex = -1;
		}

		private bool ClickingWouldInsertAPoint(Vector3 a_position)
		{
			if (m_insertingPointsDisabled) { return false; }

			Dictionary<PolygonSubEntity, HashSet<int>> point = GetPointAt(a_position, m_selectedSubEntities);
			if (point != null)
			{
				return false;
			}

			GetLineAt(a_position, m_selectedSubEntities, out PolygonSubEntity _, out var lineA, out _);

			return lineA != -1;
		}

		private Dictionary<PolygonSubEntity, HashSet<int>> GetPointAt(Vector3 a_position, HashSet<PolygonSubEntity> a_selectedSubEntities)
		{
			int closestPoint = -1;
			PolygonSubEntity closestPolygonSubEntity = null;
			float closestDistanceSquared = float.MaxValue;

			foreach (PolygonSubEntity subEntity in a_selectedSubEntities)
			{
				float closestDistSq;
				int point = subEntity.GetPointAt(a_position, out closestDistSq);
				if (point == -1 || !(closestDistSq < closestDistanceSquared))
					continue;
				closestPoint = point;
				closestPolygonSubEntity = subEntity;
				closestDistanceSquared = closestDistSq;
			}

			if (closestPoint == -1) { return null; }

			Dictionary<PolygonSubEntity, HashSet<int>> result = new Dictionary<PolygonSubEntity, HashSet<int>>();
			result.Add(closestPolygonSubEntity, new HashSet<int>() { closestPoint });
			return result;
		}

		protected PolygonSubEntity CreateNewPlanPolygon(SubEntityDataCopy a_dataCopy, int a_persistentID)
		{
			PolygonEntity newEntity = m_baseLayer.CreateNewPolygonEntity(a_dataCopy.m_entityTypeCopy, m_planLayer);
			PolygonSubEntity newSubEntity = m_baseLayer.m_editingType == AbstractLayer.EditingType.SourcePolygon ? new EnergyPolygonSubEntity(newEntity) : new PolygonSubEntity(newEntity);
			newSubEntity.SetPersistentID(a_persistentID);
			((PolygonEntity)newSubEntity.m_entity).AddSubEntity(newSubEntity);
			newSubEntity.SetDataToCopy(a_dataCopy);
			newSubEntity.m_edited = true;

			m_fsm.AddToUndoStack(new CreatePolygonOperation(newSubEntity, m_planLayer, UndoOperation.EditMode.Modify, true));
			newSubEntity.DrawGameObject(m_baseLayer.LayerGameObject.transform);
			return newSubEntity;
		}

		protected void SwitchSelectionFromBasePolygonToDuplicate(PolygonSubEntity a_basePolygon, PolygonSubEntity a_duplicate)
		{
			AddSelectedSubEntity(a_duplicate, false);
			if (m_selectedPoints.ContainsKey(a_basePolygon)) { m_selectedPoints.Add(a_duplicate, m_selectedPoints[a_basePolygon]); }
			HashSet<int> duplicateSelection = m_selectedPoints.ContainsKey(a_duplicate) ? m_selectedPoints[a_duplicate] : null;
			RemoveSelectedSubEntity(a_basePolygon);
			m_selectedPoints.Remove(a_basePolygon);

			//Change active geom 
			m_baseLayer.AddPreModifiedEntity(a_basePolygon.m_entity);
			m_baseLayer.m_activeEntities.Remove(a_basePolygon.m_entity as PolygonEntity);
			m_baseLayer.m_activeEntities.Add(a_duplicate.m_entity as PolygonEntity);
			if (m_baseLayer.IsEnergyPolyLayer())
			{
				EnergyPolygonSubEntity baseEnergyPoly = (a_basePolygon as EnergyPolygonSubEntity);
				EnergyPolygonSubEntity energyDuplicate = (a_duplicate as EnergyPolygonSubEntity);
				baseEnergyPoly.DeactivateSourcePoint();
				energyDuplicate.m_sourcePoint.RedrawGameObject();
			}

			//Redraw based on activity changes
			a_duplicate.RedrawGameObject(SubEntityDrawMode.Selected, duplicateSelection);
			a_basePolygon.RedrawGameObject();
		}

		protected virtual PolygonSubEntity StartModifyingSubEntity(PolygonSubEntity a_subEntity, bool a_insideUndoBatch)
		{
			if (a_subEntity.m_entity.PlanLayer == m_planLayer)
			{
				m_fsm.AddToUndoStack(new ModifyPolygonOperation(a_subEntity, m_planLayer, a_subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
				a_subEntity.m_edited = true;
			}
			else
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				PolygonSubEntity duplicate = CreateNewPlanPolygon(a_subEntity.GetDataCopy(), a_subEntity.GetPersistentID());
				SwitchSelectionFromBasePolygonToDuplicate(a_subEntity, duplicate);
				a_subEntity = duplicate;

				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
			}
			return a_subEntity;
		}

		public override void LeftClick(Vector3 a_worldPosition)
		{
			if (m_insertPointPreviewSubEntity != null)
			{
				RemoveInsertPointPreview();
			}
			if (!m_selectedRemovedEntity)
			{
				// case 0: control is pressed: try to select another sub entity at this position
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				{
					SelectNextSubEntity(a_worldPosition);
					return;
				}

				// case 1: clicked on a point: select the point
				Dictionary<PolygonSubEntity, HashSet<int>> point = GetPointAt(a_worldPosition, m_selectedSubEntities);
				if (point != null)
				{
					SelectPoints(point, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
					return;
				}

				// case 2: clicked on a line + shift isn't pressed: add a point on the line and select the new point
				if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
				{
					AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

					GetLineAt(a_worldPosition, m_selectedSubEntities, out PolygonSubEntity subEntity, out var lineA, out var lineB);
					if (lineA != -1)
					{
						subEntity = StartModifyingSubEntity(subEntity, false);
						subEntity.m_restrictionNeedsUpdate = true;

						int newPoint = subEntity.AddPointBetween(a_worldPosition, lineA, lineB);

						Dictionary<PolygonSubEntity, HashSet<int>> newSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();
						newSelection.Add(subEntity, new HashSet<int>() { newPoint });

						SelectPoints(newSelection, false);
						return;
					}
				}

				// case 3: clicked on a selected polygon: do nothing
				PolygonSubEntity clickedSubEntity = GetSubEntityFromSelection(a_worldPosition, m_selectedSubEntities);
				if (clickedSubEntity != null)
				{
					return;
				}

				// case 4: clicked on a polygon that is not selected + shift is pressed: add polygon to selected polygons
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					clickedSubEntity = m_baseLayer.GetSubEntityAt(a_worldPosition) as PolygonSubEntity;

					if (clickedSubEntity != null)
					{
						if (clickedSubEntity.IsPlannedForRemoval())
							return;
						AddSelectedSubEntity(clickedSubEntity);
						clickedSubEntity.RedrawGameObject(SubEntityDrawMode.Selected);
						return;
					}
				}
			}
			// case 5: clicked somewhere else: deselect all and go back to selecting state
			m_fsm.SetCurrentState(new SelectPolygonsState(m_fsm, m_planLayer));
		}

		private PolygonSubEntity GetSubEntityFromSelection(Vector2 a_position, HashSet<PolygonSubEntity> a_selection)
		{
			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistancePolygon();

			Rect positionBounds = new Rect(a_position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

			List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

			foreach (PolygonSubEntity subEntity in a_selection)
			{
				if (positionBounds.Overlaps(subEntity.m_boundingBox))
				{
					collisions.Add(subEntity);
				}
			}

			if (collisions.Count == 0) { return null; }

			foreach (PolygonSubEntity collision in collisions)
			{
				if (collision.CollidesWithPoint(a_position, maxDistance))
				{
					return collision;
				}
			}

			return null;
		}

		private void SelectNextSubEntity(Vector3 a_position)
		{
			List<SubEntity> subEntities = m_baseLayer.GetSubEntitiesAt(a_position);
			if (m_baseLayer != null) { subEntities.AddRange(m_baseLayer.GetSubEntitiesAt(a_position)); }
			if (subEntities.Count <= 0)
				return;
			int selectIndex = 0;
			for (int i = 0; i < subEntities.Count; ++i)
			{
				if (m_selectedSubEntities.Contains(subEntities[i] as PolygonSubEntity))
				{
					selectIndex = (i + 1) % subEntities.Count;
				}
			}
			if (m_baseLayer.IsEnergyPolyLayer())
				m_fsm.SetCurrentState(new EditEnergyPolygonState(m_fsm, m_planLayer, new HashSet<PolygonSubEntity> { subEntities[selectIndex] as PolygonSubEntity }));
			else
				m_fsm.SetCurrentState(new EditPolygonsState(m_fsm, m_planLayer, new HashSet<PolygonSubEntity> { subEntities[selectIndex] as PolygonSubEntity }));
		}

		public override void DoubleClick(Vector3 a_position)
		{
			if (m_selectedRemovedEntity)
				return;

			PolygonSubEntity clickedSubEntity = GetSubEntityFromSelection(a_position, m_selectedSubEntities);
			if (clickedSubEntity == null)
				return;
			HashSet<int> allPoints = new HashSet<int>();
			int totalPointCount = clickedSubEntity.GetTotalPointCount();
			for (int i = 0; i < totalPointCount; ++i)
			{
				allPoints.Add(i);
			}
			Dictionary<PolygonSubEntity, HashSet<int>> newSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();
			newSelection.Add(clickedSubEntity, allPoints);
			SelectPoints(newSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
		}

		private void SelectPoints(Dictionary<PolygonSubEntity, HashSet<int>> a_newSelection, bool a_keepPreviousSelection)
		{
			if (!a_keepPreviousSelection)
			{
				foreach (var kvp in m_selectedPoints)
				{
					kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected);
				}

				foreach (var kvp in a_newSelection)
				{
					kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value);
				}

				//SetSelectedSubEntities(newSelection);
				m_selectedPoints = a_newSelection;
			}
			else
			{
				MergeSelectionBIntoSelectionA(m_selectedPoints, a_newSelection);

				foreach (var kvp in m_selectedPoints)
				{
					kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value);
				}
			}

			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, m_selectedRemovedEntity);
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SetActivePlanWindowInteractability(!m_selectedRemovedEntity);
		}

		private void MergeSelectionBIntoSelectionA(Dictionary<PolygonSubEntity, HashSet<int>> a_a, Dictionary<PolygonSubEntity, HashSet<int>> a_b)
		{
			foreach (var kvp in a_b)
			{
				if (!a_a.ContainsKey(kvp.Key))
				{
					a_a.Add(kvp.Key, kvp.Value);
				}
				else
				{
					foreach (int index in kvp.Value)
					{
						a_a[kvp.Key].Add(index);
					}
				}
			}
		}

		private bool DraggingWouldMoveCurrentSelection(Vector3 a_dragStart)
		{
			Dictionary<PolygonSubEntity, HashSet<int>> point = GetPointAt(a_dragStart, m_selectedSubEntities);
			if (point != null)
			{
				PolygonSubEntity pse = null;
				int pointIndex = -1;
				foreach (var kvp in point)
				{
					pse = kvp.Key;
					foreach (int i in kvp.Value) { pointIndex = i; }
				}

				return m_selectedPoints.ContainsKey(pse) && m_selectedPoints[pse].Contains(pointIndex);
			}

			PolygonSubEntity subEntity = GetSubEntityFromSelection(a_dragStart, m_selectedSubEntities);

			return subEntity != null && m_selectedPoints.ContainsKey(subEntity) && m_selectedPoints[subEntity].Count == subEntity.GetTotalPointCount();
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			if (m_insertingPointsDisabled)
			{
				m_reEnableInsertingPointsPosition = a_currentPosition;
			}

			if (m_insertPointPreviewSubEntity != null)
			{
				RemoveInsertPointPreview();
			}

			if (m_draggingSelection || m_selectingBox)
				return;
			if (a_cursorIsOverUI)
			{
				m_fsm.SetCursor(FSM.CursorType.Default);
			}
			else if (!m_selectedRemovedEntity)
			{
				if (ClickingWouldInsertAPoint(a_currentPosition))
				{
					PreviewInsertPoint(a_currentPosition);
					m_fsm.SetCursor(FSM.CursorType.Insert);
				}
				else
				{
					if (DraggingWouldMoveCurrentSelection(a_currentPosition))
					{
						m_fsm.SetCursor(FSM.CursorType.Move);
					}
					else
					{
						Dictionary<PolygonSubEntity, HashSet<int>> point = GetPointAt(a_currentPosition, m_selectedSubEntities);
						UpdateHighlightingPoints(point);
						m_fsm.SetCursor(point != null ? FSM.CursorType.Move : FSM.CursorType.Default);
					}
				}
			}
		}

		private void UpdateHighlightingPoints(Dictionary<PolygonSubEntity, HashSet<int>> a_newHighlightedPoints)
		{
			Dictionary<PolygonSubEntity, HashSet<int>> oldHighlightedPoints = m_highlightedPoints;
			m_highlightedPoints = a_newHighlightedPoints ?? new Dictionary<PolygonSubEntity, HashSet<int>>();

			foreach (KeyValuePair<PolygonSubEntity, HashSet<int>> kvp in oldHighlightedPoints)
			{
				if (!m_highlightedPoints.ContainsKey(kvp.Key))
				{
					RedrawObject(kvp.Key);
				}
			}

			foreach (KeyValuePair<PolygonSubEntity, HashSet<int>> kvp in m_highlightedPoints)
			{
				RedrawObject(kvp.Key);
			}
		}

		private void CreateUndoForDraggedSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			HashSet<PolygonSubEntity> selectedPointsKeys = new HashSet<PolygonSubEntity>(m_selectedPoints.Keys);
			foreach (PolygonSubEntity subEntity in selectedPointsKeys)
			{
				StartModifyingSubEntity(subEntity, true);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_insertPointPreviewSubEntity != null)
			{
				RemoveInsertPointPreview();
			}
			if (m_selectedRemovedEntity)
				return;

			if (DraggingWouldMoveCurrentSelection(a_dragStartPosition))
			{
				m_draggingSelection = true;
			}
			else
			{
				Dictionary<PolygonSubEntity, HashSet<int>> point = GetPointAt(a_dragStartPosition, m_selectedSubEntities);
				if (point != null)
				{
					m_draggingSelection = true;
					SelectPoints(point, false);
				}
			}

			if (m_draggingSelection)
			{
				CreateUndoForDraggedSelection();

				// if the user is dragging a point, this offset is used to make sure the user is dragging the center of the point (to make snapping work correctly)
				Vector3 offset = GetSelectionDragOffset(a_dragStartPosition);

				m_selectionDragStart = new Dictionary<PolygonSubEntity, Dictionary<int, Vector3>>();
				foreach (var kvp in m_selectedPoints)
				{
					Dictionary<int, Vector3> dragStartEntry = new Dictionary<int, Vector3>();
					foreach (int index in kvp.Value)
					{
						dragStartEntry.Add(index, kvp.Key.GetPointPosition(index) + offset);
					}
					m_selectionDragStart.Add(kvp.Key, dragStartEntry);
				}
			}
			else
			{
				SelectPoints(new Dictionary<PolygonSubEntity, HashSet<int>>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				m_selectingBox = true;
				m_currentBoxSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();

				BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);
			}
		}

		private Vector3 GetSelectionDragOffset(Vector3 a_dragStartPosition)
		{
			Vector3 closestPoint = Vector3.zero;
			float closestPointSqrDistance = float.MaxValue;

			foreach (var kvp in m_selectedPoints)
			{
				foreach (int index in kvp.Value)
				{
					Vector3 selectedPoint = kvp.Key.GetPointPosition(index);
					float sqrDistance = (selectedPoint - a_dragStartPosition).sqrMagnitude;
					if (!(sqrDistance < closestPointSqrDistance))
						continue;
					closestPoint = selectedPoint;
					closestPointSqrDistance = sqrDistance;
				}
			}

			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();
			if (closestPointSqrDistance < maxDistance * maxDistance)
			{
				return a_dragStartPosition - closestPoint;
			}
			return Vector3.zero;
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			if (m_selectedRemovedEntity)
				return;

			if (m_draggingSelection)
			{
				UpdateSelectionDragPositions(a_currentPosition - a_dragStartPosition, true);//Was false, set to true to update energypoly centerpoints
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_currentPosition);
			}
		}

		protected void UpdateSelectionDragPositions(Vector3 a_offset, bool a_updateBoundingBoxes)
		{
			foreach (var kvp in m_selectedPoints)
			{
				foreach (int selectedPoint in kvp.Value)
				{
					kvp.Key.SetPointPosition(selectedPoint, m_selectionDragStart[kvp.Key][selectedPoint] + a_offset, a_updateBoundingBoxes);
				}
				if (a_updateBoundingBoxes && kvp.Value.Count != kvp.Key.GetPolygonPointCount())
					kvp.Key.m_restrictionNeedsUpdate = true;
				kvp.Key.PerformValidityCheck(false);
				kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value, null);
			}
		}

		private void UpdateBoxSelection(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);

			Dictionary<PolygonSubEntity, HashSet<int>> selectionsInBox = GetPointsInBox(a_dragStartPosition, a_currentPosition, m_selectedSubEntities);
			foreach (var kvp in selectionsInBox)
			{
				PolygonSubEntity subEntity = kvp.Key;
				bool redraw = false;
				if (!m_currentBoxSelection.ContainsKey(subEntity)) { redraw = true; }
				else
				{
					HashSet<int> currentPoints = m_currentBoxSelection[subEntity];
					foreach (int pointIndex in kvp.Value)
					{
						if (!currentPoints.Contains(pointIndex))
						{
							redraw = true;
							break;
						}
					}
				}
				if (!redraw)
					continue;
				HashSet<int> alreadySelected = m_selectedPoints.ContainsKey(subEntity) ? m_selectedPoints[subEntity] : null;
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, alreadySelected, new HashSet<int>(kvp.Value));
			}

			foreach (var kvp in m_currentBoxSelection)
			{
				PolygonSubEntity subEntity = kvp.Key;
				bool redraw = false;
				HashSet<int> hoverPoints = null;
				if (!selectionsInBox.ContainsKey(subEntity)) { redraw = true; }
				else
				{
					HashSet<int> boxPoints = selectionsInBox[subEntity];
					foreach (int pointIndex in kvp.Value)
					{
						if (boxPoints.Contains(pointIndex))
							continue;
						redraw = true;
						hoverPoints = new HashSet<int>(boxPoints);
						break;
					}
				}
				if (!redraw)
					continue;
				HashSet<int> alreadySelected = m_selectedPoints.ContainsKey(subEntity) ? m_selectedPoints[subEntity] : null;
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, alreadySelected, hoverPoints);
			}

			m_currentBoxSelection = selectionsInBox;
		}

		private Dictionary<PolygonSubEntity, HashSet<int>> GetPointsInBox(Vector3 a_boxCornerA, Vector3 a_boxCornerB, HashSet<PolygonSubEntity> a_selectedSubEntities)
		{
			Vector3 min = Vector3.Min(a_boxCornerA, a_boxCornerB);
			Vector3 max = Vector3.Max(a_boxCornerA, a_boxCornerB);

			Dictionary<PolygonSubEntity, HashSet<int>> result = new Dictionary<PolygonSubEntity, HashSet<int>>();

			foreach (PolygonSubEntity subEntity in a_selectedSubEntities)
			{
				HashSet<int> points = subEntity.GetPointsInBox(min, max);
				if (points != null)
				{
					result.Add(subEntity, points);
				}
			}

			return result;
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			if (m_draggingSelection)
			{
				AudioMain.Instance.PlaySound(AudioMain.ITEM_MOVED);

				//TODO: Update restriction polygon
				UpdateSelectionDragPositions(a_dragFinalPosition - a_dragStartPosition, true);
				m_draggingSelection = false;
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_dragFinalPosition);

				SelectPoints(m_currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				BoxSelect.HideBoxSelection();
				m_selectingBox = false;
				m_currentBoxSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();
			}
		}

		private int getNumberOfSelectedPointsOnContour(PolygonSubEntity a_subEntity, HashSet<int> a_selectedPoints)
		{
			int result = 0;
			int pointsOnContour = a_subEntity.GetPolygonPointCount();
			foreach (int selectedPoint in a_selectedPoints)
			{
				if (selectedPoint < pointsOnContour) { result++; }
			}
			return result;
		}

		protected virtual void OnPolygonRemoved(SubEntity a_removedSubEntity)
		{
		}

		protected virtual void OnPolygonModifiedViaRemoval(SubEntity a_modifiedSubEntity)
		{
		}

		protected virtual void DeleteSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			if (m_selectedPoints.Count == 0)
			{
				//Delete all selected subentities
				foreach (PolygonSubEntity subEntity in m_selectedSubEntities)
				{
					if (subEntity.m_entity.PlanLayer == m_planLayer)
					{
						m_fsm.AddToUndoStack(new RemovePolygonOperation(subEntity, m_planLayer, UndoOperation.EditMode.Modify));
						OnPolygonRemoved(subEntity);
						m_baseLayer.RemoveSubEntity(subEntity);
						subEntity.RemoveGameObject();
					}
					else
					{
						m_fsm.AddToUndoStack(new ModifyPolygonRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
						OnPolygonModifiedViaRemoval(subEntity);
						m_baseLayer.RemoveSubEntity(subEntity);
						subEntity.RedrawGameObject();
					}
				}
			}
			else
			{
				//Delete all selected points
				List<PolygonSubEntity> selectedPointsKeys = new List<PolygonSubEntity>(m_selectedPoints.Keys);
				foreach (PolygonSubEntity subEntity in selectedPointsKeys)
				{
					if (subEntity.GetPolygonPointCount() - getNumberOfSelectedPointsOnContour(subEntity, m_selectedPoints[subEntity]) < 3)
					{
						// remove polygon if it has fewer than 3 points left on the contour after deletion
						if (subEntity.m_entity.PlanLayer == m_planLayer)
						{
							m_fsm.AddToUndoStack(new RemovePolygonOperation(subEntity, m_planLayer, UndoOperation.EditMode.Modify));
							OnPolygonRemoved(subEntity);
							m_baseLayer.RemoveSubEntity(subEntity);
							subEntity.RemoveGameObject();
						}
						else
						{
							m_fsm.AddToUndoStack(new ModifyPolygonRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
							OnPolygonModifiedViaRemoval(subEntity);
							m_baseLayer.RemoveSubEntity(subEntity);
							subEntity.RedrawGameObject();
						}
						RemoveSelectedSubEntity(subEntity);
					}
					else
					{
						PolygonSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);

						subEntityToModify.RemovePoints(m_selectedPoints[subEntityToModify]);

						int holeCount = subEntityToModify.GetHoleCount();
						for (int i = holeCount - 1; i >= 0; --i)
						{
							if (subEntityToModify.GetHolePointCount(i) < 3)
							{
								// remove hole if it has fewer than 3 points left
								subEntityToModify.RemoveHole(i);
							}
						}

						subEntityToModify.m_restrictionNeedsUpdate = true;
						subEntityToModify.PerformValidityCheck(false);
						subEntityToModify.RedrawGameObject();
					}
				}
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			m_fsm.SetCurrentState(new SelectPolygonsState(m_fsm, m_planLayer));
		}

		private void UndoDeleteForSelection()
		{
			if (!m_selectedRemovedEntity)
				return;
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			foreach (PolygonSubEntity subEntity in m_selectedSubEntities)
			{
				m_fsm.AddToUndoStack(new ModifyPolygonRemovalPlanOperation(subEntity, m_planLayer, true));
				m_planLayer.RemovedGeometry.Remove(subEntity.GetPersistentID());
				subEntity.RestoreDependencies();
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_selectedPoints.ContainsKey(subEntity) ? m_selectedPoints[subEntity] : null);

			}
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			m_fsm.SetCurrentState(new SelectPolygonsState(m_fsm, m_planLayer));
		}

		private void RemoveHolesFromSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PolygonSubEntity subEntity in m_selectedSubEntities)
			{
				m_fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, m_planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));

				subEntity.m_edited = true;
				subEntity.RemoveAllHoles();
				subEntity.PerformValidityCheck(false);
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleKeyboardEvents()
		{
			// hasn't got anything to do with keyboard events; added here because this function is called every frame
			if (m_insertingPointsDisabled && Time.time - m_stateEnteredTime > InsertPointDelay)
			{
				m_insertingPointsDisabled = false;
				MouseMoved(m_reEnableInsertingPointsPosition, m_reEnableInsertingPointsPosition, false);
			}

			if (Input.GetKeyDown(KeyCode.Delete))
			{
				DeleteSelection();
			}

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
				case FSM.ToolbarInput.Delete:
					DeleteSelection();
					break;
				case FSM.ToolbarInput.Abort:
					m_fsm.SetCurrentState(new SelectPolygonsState(m_fsm, m_planLayer));
					break;
				case FSM.ToolbarInput.Recall:
					UndoDeleteForSelection();
					break;
			}
		}

		public override void HandleEntityTypeChange(List<EntityType> a_newTypes)
		{
			if (a_newTypes.Count <= 0) { return; }

			List<PolygonSubEntity> subEntitiesWithDifferentTypes = new List<PolygonSubEntity>();

			//Find subentities with changed entity types
			foreach (PolygonSubEntity subEntity in m_selectedSubEntities)
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

			foreach (PolygonSubEntity subEntity in subEntitiesWithDifferentTypes)
			{
				PolygonSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.EntityTypes = a_newTypes;
				subEntityToModify.RedrawGameObject(SubEntityDrawMode.Selected, m_selectedPoints.ContainsKey(subEntityToModify) ? m_selectedPoints[subEntityToModify] : null);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleTeamChange(int a_newTeam)
		{
			List<PolygonSubEntity> subEntitiesWithDifferentTeam = new List<PolygonSubEntity>();

			//Find subentities with changed entity types
			foreach (PolygonSubEntity subEntity in m_selectedSubEntities)
			{
				if (subEntity.m_entity.Country != a_newTeam)
				{
					subEntitiesWithDifferentTeam.Add(subEntity);
				}
			}

			if (subEntitiesWithDifferentTeam.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PolygonSubEntity subEntity in subEntitiesWithDifferentTeam)
			{
				PolygonSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.Country = a_newTeam;
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleParameterChange(EntityPropertyMetaData a_parameter, string a_newValue)
		{
			List<PolygonSubEntity> subEntitiesWithDifferentParams = new List<PolygonSubEntity>();

			//Find subentities with changed entity types
			foreach (PolygonSubEntity subEntity in m_selectedSubEntities)
			{
				if (subEntity.m_entity.GetPropertyMetaData(a_parameter) != a_newValue)
				{
					subEntitiesWithDifferentParams.Add(subEntity);
				}
			}

			if (subEntitiesWithDifferentParams.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (PolygonSubEntity subEntity in subEntitiesWithDifferentParams)
			{
				PolygonSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.SetPropertyMetaData(a_parameter, a_newValue);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			foreach (PolygonSubEntity pse in m_selectedSubEntities)
			{
				pse.RedrawGameObject();
			}
			m_selectedSubEntities = new HashSet<PolygonSubEntity>();
			m_selectedSourcePoints = new HashSet<EnergyPointSubEntity>();

			BoxSelect.HideBoxSelection();
			IssueManager.Instance.SetIssueInteractability(true);
		}

		private void AddSelectedSubEntity(PolygonSubEntity a_subEntity, bool a_updateActivePlanWindow = true)
		{
			m_selectedSubEntities.Add(a_subEntity);
			if (this is EditEnergyPolygonState)
				m_selectedSourcePoints.Add((a_subEntity as EnergyPolygonSubEntity).m_sourcePoint);
			if(a_updateActivePlanWindow)
				UpdateActivePlanWindowToSelection();
		}

		private void SetSelectedSubEntities(HashSet<PolygonSubEntity> a_subEntities)
		{
			m_selectedSubEntities = a_subEntities;
			foreach (PolygonSubEntity poly in a_subEntities) //Check if this is a polygon marked for removal, this limits editing
			{
				m_selectedRemovedEntity = poly.IsPlannedForRemoval();
				InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, m_selectedRemovedEntity);
				InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SetActivePlanWindowInteractability(!m_selectedRemovedEntity);
				break;
			}
			if (!(this is EditEnergyPolygonState))
				return;
			{
				m_selectedSourcePoints = new HashSet<EnergyPointSubEntity>();
				foreach (PolygonSubEntity poly in m_selectedSubEntities)
					m_selectedSourcePoints.Add((poly as EnergyPolygonSubEntity).m_sourcePoint);
			}
		}

		private void RemoveSelectedSubEntity(PolygonSubEntity a_subEntity)
		{
			m_selectedSubEntities.Remove(a_subEntity);
			if (this is EditEnergyPolygonState)
				m_selectedSourcePoints.Remove((a_subEntity as EnergyPolygonSubEntity).m_sourcePoint);
		}

		private void UpdateActivePlanWindowToSelection()
		{
			List<List<EntityType>> selectedEntityTypes = new List<List<EntityType>>();
			int? selectedTeam = null;
			List<Dictionary<EntityPropertyMetaData, string>> selectedParams = new List<Dictionary<EntityPropertyMetaData, string>>();

			foreach (PolygonSubEntity pse in m_selectedSubEntities)
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
			}

			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.SetToSelection(
				selectedEntityTypes.Count > 0 ? selectedEntityTypes : null,
				selectedTeam ?? -2,
				selectedParams);
		}
	}
}
