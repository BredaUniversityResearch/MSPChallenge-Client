using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EditLineStringsState : FSMState
	{
		private const float INSERT_POINT_DELAY = 0.5f;

		//protected LineStringLayer layer;
		protected LineStringLayer m_baseLayer;
		protected PlanLayer m_planLayer;

		protected bool m_draggingSelection = false;
		protected Dictionary<LineStringSubEntity, Dictionary<int, Vector3>> m_selectionDragStart = null;

		protected bool m_selectingBox = false;
		protected Dictionary<LineStringSubEntity, HashSet<int>> m_currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();

		protected HashSet<LineStringSubEntity> m_selectedSubEntities = new HashSet<LineStringSubEntity>();
		protected Dictionary<LineStringSubEntity, HashSet<int>> m_selectedPoints = new Dictionary<LineStringSubEntity, HashSet<int>>();
		private Dictionary<LineStringSubEntity, HashSet<int>> m_highlightedPoints = new Dictionary<LineStringSubEntity, HashSet<int>>();

		private bool m_insertingPointsDisabled = true;
		private bool m_selectedRemovedEntity = false;
		private float m_stateEnteredTime = float.MinValue;
		private Vector3 m_reEnableInsertingPointsPosition;

		private LineStringSubEntity m_insertPointPreviewSubEntity = null;
		private int m_insertPointPreviewIndex = -1;
		public override EEditingStateType StateType => EEditingStateType.Edit;

		public EditLineStringsState(FSM a_fsm, PlanLayer a_planLayer, HashSet<LineStringSubEntity> a_selectedSubEntities) : base(a_fsm)
		{
			m_planLayer = a_planLayer;
			m_baseLayer = a_planLayer.BaseLayer as LineStringLayer;
			SetSelectedSubEntities(a_selectedSubEntities);
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);
			ToolBar toolbar = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar;

			toolbar.SetCreateMode(false);
			toolbar.SetButtonInteractable(FSM.ToolbarInput.Delete, true);
			if (m_planLayer.BaseLayer is ShippingLineStringLayer)
			{
				toolbar.SetButtonActive(FSM.ToolbarInput.ChangeDirection, true);
				toolbar.SetButtonInteractable(FSM.ToolbarInput.ChangeDirection, true);
			}

			foreach (LineStringSubEntity lse in m_selectedSubEntities)
			{
				lse.RedrawGameObject(SubEntityDrawMode.Selected);
			}

			UpdateActivePlanWindowToSelection();

			m_insertingPointsDisabled = true;
			m_stateEnteredTime = Time.time;
			m_reEnableInsertingPointsPosition = a_currentMousePosition;

			m_fsm.SetSnappingEnabled(true);
			IssueManager.Instance.SetIssueInteractability(false);
		}

		private void SetSelectedSubEntities(HashSet<LineStringSubEntity> a_subEntities)
		{
			m_selectedSubEntities = a_subEntities;
			AP_GeometryTool gt = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool;
			//Check if this is a line marked for removal, this limits editing and enables recall
			if (this is EditEnergyLineStringsState)
				foreach (LineStringSubEntity line in a_subEntities)
				{
					EnergyLineStringSubEntity energyLine = line as EnergyLineStringSubEntity;
					m_selectedRemovedEntity = line.IsPlannedForRemoval();
					bool canRecall = m_selectedRemovedEntity;
					//If a point connected to this line was moved or removed it cannot be recalled
					if (m_selectedRemovedEntity)
						foreach (Connection con in energyLine.Connections)
							if (con.point.IsPlannedForRemoval() || !con.point.IsNotShownInPlan())
								canRecall = false;
					gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, canRecall);
					break;
				}
			else
				foreach (LineStringSubEntity line in a_subEntities)
				{
					m_selectedRemovedEntity = line.IsPlannedForRemoval();
					gt.m_toolBar.SetButtonInteractable(FSM.ToolbarInput.Recall, m_selectedRemovedEntity);
					break;
				}
			gt.SetActivePlanWindowInteractability(!m_selectedRemovedEntity);
			foreach (LineStringSubEntity line in a_subEntities)
				line.SetInFrontOfLayer(true);
		}

		private void RedrawObject(LineStringSubEntity a_entity)
		{
			SubEntityDrawMode drawMode = m_selectedSubEntities.Contains(a_entity) ? SubEntityDrawMode.Selected : SubEntityDrawMode.Default;
			HashSet<int> selectedPointsForEntity;
			HashSet<int> highlightedPointsForEntity;
			m_selectedPoints.TryGetValue(a_entity, out selectedPointsForEntity);
			m_highlightedPoints.TryGetValue(a_entity, out highlightedPointsForEntity);
			a_entity.RedrawGameObject(drawMode, selectedPointsForEntity, highlightedPointsForEntity);
		}

		private void PreviewInsertPoint(Vector3 a_position)
		{
			int lineA, lineB;
			GetLineAt(a_position, m_selectedSubEntities, out m_insertPointPreviewSubEntity, out lineA, out lineB);
			if (lineA == -1)
				return;
			m_insertPointPreviewIndex = m_insertPointPreviewSubEntity.AddPointBetween(a_position, lineA, lineB);

			m_insertPointPreviewSubEntity.RedrawGameObject(SubEntityDrawMode.Selected, new HashSet<int>() { m_insertPointPreviewIndex }, null);
		}

		private void GetLineAt(Vector3 a_position, HashSet<LineStringSubEntity> a_selectedSubEntities, out LineStringSubEntity a_subEntity, out int a_lineA, out int a_lineB)
		{
			float threshold = VisualizationUtil.Instance.GetSelectMaxDistance();
			threshold *= threshold;

			a_lineA = -1;
			a_lineB = -1;
			a_subEntity = null;
			float closestDistanceSquared = float.MaxValue;

			foreach (LineStringSubEntity lsse in a_selectedSubEntities)
			{
				int lsseLineA, lsseLineB;
				float closestDistSq;
				lsse.GetLineAt(a_position, out lsseLineA, out lsseLineB, out closestDistSq);
				if (lsseLineA != -1 && closestDistSq < closestDistanceSquared)
				{
					a_lineA = lsseLineA;
					a_lineB = lsseLineB;
					a_subEntity = lsse;
					closestDistanceSquared = closestDistSq;
				}
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
			if (m_insertingPointsDisabled) return false;

			Dictionary<LineStringSubEntity, HashSet<int>> point = GetPointAt(a_position, m_selectedSubEntities);
			if (point != null)
			{
				return false;
			}

			LineStringSubEntity subEntity;
			GetLineAt(a_position, m_selectedSubEntities, out subEntity, out var lineA, out _);

			return lineA != -1;
		}

		private Dictionary<LineStringSubEntity, HashSet<int>> GetPointAt(Vector3 a_position, HashSet<LineStringSubEntity> a_selectedSubEntities)
		{
			float threshold = VisualizationUtil.Instance.GetSelectMaxDistance();
			threshold *= threshold;

			int closestPoint = -1;
			LineStringSubEntity closestLineStringSubEntity = null;
			float closestDistanceSquared = float.MaxValue;

			foreach (LineStringSubEntity subEntity in a_selectedSubEntities)
			{
				float closestDistSq;
				int point = subEntity.GetPointAt(a_position, out closestDistSq);
				if (point == -1 || !(closestDistSq < closestDistanceSquared))
					continue;
				closestPoint = point;
				closestLineStringSubEntity = subEntity;
				closestDistanceSquared = closestDistSq;
			}

			if (closestPoint == -1)
			{
				return null;
			}

			Dictionary<LineStringSubEntity, HashSet<int>> result = new Dictionary<LineStringSubEntity, HashSet<int>> {{closestLineStringSubEntity, new HashSet<int>() {closestPoint}}};
			return result;
		}

		protected LineStringSubEntity CreateNewPlanLineString(int a_persistentID, SubEntityDataCopy a_dataCopy)
		{
			LineStringEntity newEntity = m_baseLayer.CreateNewLineStringEntity(a_dataCopy.m_entityTypeCopy, m_planLayer);
			LineStringSubEntity newSubEntity = m_baseLayer.IsEnergyLineLayer() ? new EnergyLineStringSubEntity(newEntity) : new LineStringSubEntity(newEntity);
			newSubEntity.SetPersistentID(a_persistentID);
			newEntity.AddSubEntity(newSubEntity);
			newSubEntity.SetDataToCopy(a_dataCopy);
			newSubEntity.m_edited = true;

			m_fsm.AddToUndoStack(new CreateLineStringOperation(newSubEntity, m_planLayer, UndoOperation.EditMode.Modify, true));
			newSubEntity.DrawGameObject(m_baseLayer.LayerGameObject.transform);
			return newSubEntity;
		}

		protected void SwitchSelectionFromBaseLineStringToDuplicate(LineStringSubEntity a_baseLineString, LineStringSubEntity a_duplicate)
		{
			m_selectedSubEntities.Add(a_duplicate);
			a_duplicate.SetInFrontOfLayer(true);
			if (m_selectedPoints.ContainsKey(a_baseLineString)) { m_selectedPoints.Add(a_duplicate, m_selectedPoints[a_baseLineString]); }
			HashSet<int> duplicateSelection = m_selectedPoints.ContainsKey(a_duplicate) ? m_selectedPoints[a_duplicate] : null;
			m_selectedSubEntities.Remove(a_baseLineString);
			a_baseLineString.SetInFrontOfLayer(false);
			m_selectedPoints.Remove(a_baseLineString);

			//Change active geom 
			m_baseLayer.AddPreModifiedEntity(a_baseLineString.m_entity);
			m_baseLayer.m_activeEntities.Remove(a_baseLineString.m_entity as LineStringEntity);
			m_baseLayer.m_activeEntities.Add(a_duplicate.m_entity as LineStringEntity);

			//Redraw based on activity changes
			a_duplicate.RedrawGameObject(SubEntityDrawMode.Selected, duplicateSelection);
			a_baseLineString.RedrawGameObject();
		}

		protected virtual LineStringSubEntity StartModifyingSubEntity(LineStringSubEntity a_subEntity, bool a_insideUndoBatch)
		{
			if (m_planLayer == a_subEntity.m_entity.PlanLayer)
			{
				m_fsm.AddToUndoStack(new ModifyLineStringOperation(a_subEntity, m_planLayer, a_subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
				a_subEntity.m_edited = true;
			}
			else
			{
				if (!a_insideUndoBatch) { m_fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

				LineStringSubEntity duplicate = CreateNewPlanLineString(a_subEntity.GetPersistentID(), a_subEntity.GetDataCopy());
				SwitchSelectionFromBaseLineStringToDuplicate(a_subEntity, duplicate);
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
				// case 1: clicked on a point: select the point
				Dictionary<LineStringSubEntity, HashSet<int>> point = GetPointAt(a_worldPosition, m_selectedSubEntities);
				if (point != null)
				{
					//If we are an energy layer, ignore the first and last points
					foreach (var kvp in point)
						if (this is EditEnergyLineStringsState && kvp.Key.AreFirstOrLastPoints(kvp.Value))
							SelectPoints(point, false);
					SelectPoints(point, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
					return;
				}

				// case 2: clicked on a line + shift isn't pressed: add a point on the line and select the new point
				if (!m_insertingPointsDisabled && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
				{
					AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

					int lineA, lineB;
					LineStringSubEntity subEntity;
					GetLineAt(a_worldPosition, m_selectedSubEntities, out subEntity, out lineA, out lineB);
					if (lineA != -1)
					{
						subEntity = StartModifyingSubEntity(subEntity, false);
						subEntity.m_restrictionNeedsUpdate = true;

						int newPoint = subEntity.AddPointBetween(a_worldPosition, lineA, lineB);

						Dictionary<LineStringSubEntity, HashSet<int>> newSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
						newSelection.Add(subEntity, new HashSet<int>() { newPoint });

						SelectPoints(newSelection, false);
						return;
					}
				}

				// case 3: clicked on a selected linestring: do nothing
				LineStringSubEntity clickedSubEntity = GetSubEntityFromSelection(a_worldPosition, m_selectedSubEntities);
				if (clickedSubEntity == null && m_baseLayer != null) { clickedSubEntity = m_baseLayer.GetSubEntityAt(a_worldPosition) as LineStringSubEntity; }

				if (clickedSubEntity != null)
				{
					return;
				}

				// case 4: clicked on a linestring that is not selected + shift is pressed: add linestring to selected linestrings
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					clickedSubEntity = m_baseLayer.GetSubEntityAt(a_worldPosition) as LineStringSubEntity;

					if (clickedSubEntity != null)
					{
						if (clickedSubEntity.IsPlannedForRemoval())
							return;
						m_selectedSubEntities.Add(clickedSubEntity);
						clickedSubEntity.SetInFrontOfLayer(true);
						clickedSubEntity.RedrawGameObject(SubEntityDrawMode.Selected);
						UpdateActivePlanWindowToSelection();
						return;
					}
				}
			}

			// case 5: clicked somewhere else: deselect all and go back to selecting state
			m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
		}

		protected LineStringSubEntity GetSubEntityFromSelection(Vector2 a_position, HashSet<LineStringSubEntity> a_selection)
		{
			float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();

			Rect positionBounds = new Rect(a_position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

			List<LineStringSubEntity> collisions = new List<LineStringSubEntity>();

			foreach (LineStringSubEntity subEntity in a_selection)
			{
				if (positionBounds.Overlaps(subEntity.m_boundingBox))
				{
					collisions.Add(subEntity);
				}
			}

			if (collisions.Count == 0) { return null; }

			foreach (LineStringSubEntity collision in collisions)
			{
				if (collision.CollidesWithPoint(a_position, maxDistance))
				{
					return collision;
				}
			}

			return null;
		}

		public override void DoubleClick(Vector3 a_position)
		{
			if (m_selectedRemovedEntity)
				return;

			LineStringSubEntity clickedSubEntity = GetSubEntityFromSelection(a_position, m_selectedSubEntities);
			if (clickedSubEntity == null)
				return;
			HashSet<int> allPoints = new HashSet<int>();
			for (int i = 0; i < clickedSubEntity.GetPointCount(); ++i)
			{
				allPoints.Add(i);
			}
			Dictionary<LineStringSubEntity, HashSet<int>> newSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
			newSelection.Add(clickedSubEntity, allPoints);
			SelectPoints(newSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
		}

		protected void SelectPoints(Dictionary<LineStringSubEntity, HashSet<int>> a_newSelection, bool a_keepPreviousSelection)
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

		private void MergeSelectionBIntoSelectionA(Dictionary<LineStringSubEntity, HashSet<int>> a_a, Dictionary<LineStringSubEntity, HashSet<int>> a_b)
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
			Dictionary<LineStringSubEntity, HashSet<int>> point = GetPointAt(a_dragStart, m_selectedSubEntities);
			if (point != null)
			{
				LineStringSubEntity lsse = null;
				int pointIndex = -1;
				foreach (var kvp in point)
				{
					lsse = kvp.Key;
					foreach (int i in kvp.Value) { pointIndex = i; }
				}

				return m_selectedPoints.ContainsKey(lsse) && m_selectedPoints[lsse].Contains(pointIndex);
			}

			LineStringSubEntity subEntity = GetSubEntityFromSelection(a_dragStart, m_selectedSubEntities);

			return subEntity != null && m_selectedPoints.ContainsKey(subEntity) && m_selectedPoints[subEntity].Count == subEntity.GetPointCount();
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
						Dictionary<LineStringSubEntity, HashSet<int>> point = GetPointAt(a_currentPosition, m_selectedSubEntities);
						UpdateHighlightingPoints(point);
						if (point != null)
						{
							m_fsm.SetCursor(FSM.CursorType.Move);
						}
						else
						{
							m_fsm.SetCursor(FSM.CursorType.Default);
						}
					}
				}
			}
		}

		private void UpdateHighlightingPoints(Dictionary<LineStringSubEntity, HashSet<int>> a_newHighlightedPoints)
		{
			Dictionary<LineStringSubEntity, HashSet<int>> oldHighlightedPoints = m_highlightedPoints;
			m_highlightedPoints = a_newHighlightedPoints ?? new Dictionary<LineStringSubEntity, HashSet<int>>();

			foreach (KeyValuePair<LineStringSubEntity, HashSet<int>> kvp in oldHighlightedPoints)
			{
				if (!m_highlightedPoints.ContainsKey(kvp.Key))
				{
					RedrawObject(kvp.Key);
				}
			}

			foreach (KeyValuePair<LineStringSubEntity, HashSet<int>> kvp in m_highlightedPoints)
			{
				RedrawObject(kvp.Key);
			}
		}

		private void CreateUndoForDraggedSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			HashSet<LineStringSubEntity> selectedPointsKeys = new HashSet<LineStringSubEntity>(m_selectedPoints.Keys);
			foreach (LineStringSubEntity subEntity in selectedPointsKeys)
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
				Dictionary<LineStringSubEntity, HashSet<int>> point = GetPointAt(a_dragStartPosition, m_selectedSubEntities);
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

				m_selectionDragStart = new Dictionary<LineStringSubEntity, Dictionary<int, Vector3>>();
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
				SelectPoints(new Dictionary<LineStringSubEntity, HashSet<int>>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				m_selectingBox = true;
				m_currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();

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
				UpdateSelectionDragPositions(a_currentPosition - a_dragStartPosition);
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_currentPosition);
			}
		}

		protected virtual void UpdateSelectionDragPositions(Vector3 a_offset)
		{
			foreach (var kvp in m_selectedPoints)
			{
				foreach (int selectedPoint in kvp.Value)
				{
					kvp.Key.SetPointPosition(selectedPoint, m_selectionDragStart[kvp.Key][selectedPoint] + a_offset);
				}
				if (kvp.Value.Count != kvp.Key.GetPointCount())
					kvp.Key.m_restrictionNeedsUpdate = true;
				kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value, null);
			}
		}

		protected void UpdateBoxSelection(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);

			Dictionary<LineStringSubEntity, HashSet<int>> selectionsInBox = GetPointsInBox(a_dragStartPosition, a_currentPosition, m_selectedSubEntities);
			foreach (var kvp in selectionsInBox)
			{
				LineStringSubEntity subEntity = kvp.Key;
				bool redraw = false;
				if (!m_currentBoxSelection.ContainsKey(subEntity)) { redraw = true; }
				else
				{
					HashSet<int> currentPoints = m_currentBoxSelection[subEntity];
					foreach (int pointIndex in kvp.Value)
					{
						if (currentPoints.Contains(pointIndex))
							continue;
						redraw = true;
						break;
					}
				}
				if (!redraw)
					continue;
				HashSet<int> alreadySelected = m_selectedPoints.ContainsKey(subEntity) ? m_selectedPoints[subEntity] : null;
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, alreadySelected, new HashSet<int>(kvp.Value));
			}

			foreach (var kvp in m_currentBoxSelection)
			{
				LineStringSubEntity subEntity = kvp.Key;
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

		private Dictionary<LineStringSubEntity, HashSet<int>> GetPointsInBox(Vector3 a_boxCornerA, Vector3 a_boxCornerB, HashSet<LineStringSubEntity> a_selectedSubEntities)
		{
			Vector3 min = Vector3.Min(a_boxCornerA, a_boxCornerB);
			Vector3 max = Vector3.Max(a_boxCornerA, a_boxCornerB);

			Dictionary<LineStringSubEntity, HashSet<int>> result = new Dictionary<LineStringSubEntity, HashSet<int>>();

			foreach (LineStringSubEntity subEntity in a_selectedSubEntities)
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

				UpdateSelectionDragPositions(a_dragFinalPosition - a_dragStartPosition);
				m_draggingSelection = false;
			}
			else if (m_selectingBox)
			{
				UpdateBoxSelection(a_dragStartPosition, a_dragFinalPosition);

				SelectPoints(m_currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

				BoxSelect.HideBoxSelection();
				m_selectingBox = false;
				m_currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
			}
		}

		protected virtual void DeleteSelection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			if (m_selectedPoints.Count == 0)
			{
				//Delete all selected
				foreach (LineStringSubEntity subEntity in m_selectedSubEntities)
				{
					if (subEntity.m_entity.PlanLayer == m_planLayer)
					{
						m_fsm.AddToUndoStack(new RemoveLineStringOperation(subEntity, m_planLayer, UndoOperation.EditMode.Modify));
						m_baseLayer.RemoveSubEntity(subEntity);
						subEntity.RemoveGameObject();
					}
					else
					{
						m_fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
						m_baseLayer.RemoveSubEntity(subEntity);
						subEntity.RedrawGameObject();
					}
				}
			}
			else
			{
				//Delete selected points
				List<LineStringSubEntity> selectedPointsKeys = new List<LineStringSubEntity>(m_selectedPoints.Keys);
				foreach (LineStringSubEntity subEntity in selectedPointsKeys)
				{
					if (subEntity.GetPointCount() - m_selectedPoints[subEntity].Count < 2)
					{
						// remove linestring if it has fewer than 2 points left after deletion
						if (subEntity.m_entity.PlanLayer == m_planLayer)
						{
							m_fsm.AddToUndoStack(new RemoveLineStringOperation(subEntity, m_planLayer, UndoOperation.EditMode.Modify));
							m_baseLayer.RemoveSubEntity(subEntity);
							subEntity.RemoveGameObject();
						}
						else
						{
							m_fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, m_planLayer, m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
							m_baseLayer.RemoveSubEntity(subEntity);
							subEntity.RedrawGameObject();
						}

						m_selectedSubEntities.Remove(subEntity);
						subEntity.SetInFrontOfLayer(false);
					}
					else
					{
						LineStringSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);

						subEntityToModify.RemovePoints(m_selectedPoints[subEntityToModify]);

						subEntity.m_restrictionNeedsUpdate = true;
						subEntityToModify.RedrawGameObject();
					}
				}
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
		}

		private void UndoDeleteForSelection()
		{
			if (!m_selectedRemovedEntity)
				return;
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			foreach (LineStringSubEntity subEntity in m_selectedSubEntities)
			{
				if (!m_planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID()))
					continue;
				m_fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, m_planLayer, true));
				m_planLayer.RemovedGeometry.Remove(subEntity.GetPersistentID());
				subEntity.RestoreDependencies();
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, m_selectedPoints.ContainsKey(subEntity) ? m_selectedPoints[subEntity] : null);
			}
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
		}

		private void ChangeSelectionDirection()
		{
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
			LineStringSubEntity[] localSelectedEntities = new LineStringSubEntity[m_selectedSubEntities.Count];
			m_selectedSubEntities.CopyTo(localSelectedEntities);
			foreach (LineStringSubEntity subEntity in localSelectedEntities)
			{
				string oldDirection;
				oldDirection = subEntity.m_entity.DoesPropertyExist(ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY) ?
					subEntity.m_entity.GetMetaData(ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY) :
					ShippingLineStringEntity.DIRECTION_DEFAULT;

				string newDirection = ShippingLineStringEntity.CycleToNextDirection(oldDirection);

				LineStringSubEntity newSubEntity = StartModifyingSubEntity(subEntity, true);

				m_fsm.AddToUndoStack(new UndoOperationChangeMeta(newSubEntity.m_entity, ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY, oldDirection, newDirection));

				newSubEntity.m_entity.SetMetaData(ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY, newDirection);

				newSubEntity.m_entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Selected);
			}
			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleKeyboardEvents()
		{
			// hasn't got anything to do with keyboard events; added here because this function is called every frame
			if (m_insertingPointsDisabled && Time.time - m_stateEnteredTime > INSERT_POINT_DELAY)
			{
				m_insertingPointsDisabled = false;
				MouseMoved(m_reEnableInsertingPointsPosition, m_reEnableInsertingPointsPosition, false);
			}

			if (Input.GetKeyDown(KeyCode.Delete))
			{
				DeleteSelection();
			}

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
				case FSM.ToolbarInput.Delete:
					DeleteSelection();
					break;
				case FSM.ToolbarInput.Abort:
					m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, m_planLayer));
					break;
				case FSM.ToolbarInput.Recall:
					UndoDeleteForSelection();
					break;
				case FSM.ToolbarInput.ChangeDirection:
					ChangeSelectionDirection();
					break;
			}
		}

		public override void HandleEntityTypeChange(List<EntityType> a_newTypes)
		{
			List<LineStringSubEntity> subEntitiesWithDifferentTypes = new List<LineStringSubEntity>();

			//Find subentities with changed entity types
			foreach (LineStringSubEntity subEntity in m_selectedSubEntities)
			{
				if (subEntity.m_entity.EntityTypes.Count != a_newTypes.Count)
				{
					subEntitiesWithDifferentTypes.Add(subEntity);
					continue;
				}
				foreach (EntityType type in subEntity.m_entity.EntityTypes)
				{
					if (!a_newTypes.Contains(type))
					{
						subEntitiesWithDifferentTypes.Add(subEntity);
						break;
					}
				}
			}

			if (subEntitiesWithDifferentTypes.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (LineStringSubEntity subEntity in subEntitiesWithDifferentTypes)
			{
				LineStringSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.EntityTypes = a_newTypes;
				subEntityToModify.RedrawGameObject(SubEntityDrawMode.Selected, m_selectedPoints.ContainsKey(subEntityToModify) ? m_selectedPoints[subEntityToModify] : null);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleTeamChange(int a_newTeam)
		{
			List<LineStringSubEntity> subEntitiesWithDifferentTeam = new List<LineStringSubEntity>();

			//Find subentities with changed entity types
			foreach (LineStringSubEntity subEntity in m_selectedSubEntities)
			{
				if (subEntity.m_entity.Country != a_newTeam)
				{
					subEntitiesWithDifferentTeam.Add(subEntity);
				}
			}

			if (subEntitiesWithDifferentTeam.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (LineStringSubEntity subEntity in subEntitiesWithDifferentTeam)
			{
				LineStringSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.Country = a_newTeam;
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void HandleParameterChange(EntityPropertyMetaData a_parameter, string a_newValue)
		{
			List<LineStringSubEntity> subEntitiesWithDifferentParams = new List<LineStringSubEntity>();

			//Find subentities with changed entity types
			foreach (LineStringSubEntity subEntity in m_selectedSubEntities)
			{
				if (subEntity.m_entity.GetPropertyMetaData(a_parameter) != a_newValue)
				{
					subEntitiesWithDifferentParams.Add(subEntity);
				}
			}

			if (subEntitiesWithDifferentParams.Count == 0) { return; }

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());

			foreach (LineStringSubEntity subEntity in subEntitiesWithDifferentParams)
			{
				LineStringSubEntity subEntityToModify = StartModifyingSubEntity(subEntity, true);
				subEntityToModify.m_entity.SetPropertyMetaData(a_parameter, a_newValue);
			}

			m_fsm.AddToUndoStack(new BatchUndoOperationMarker());
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			foreach (LineStringSubEntity lsse in m_selectedSubEntities)
			{
				lsse.SetInFrontOfLayer(false);
				lsse.RedrawGameObject();
			}
			m_selectedSubEntities = new HashSet<LineStringSubEntity>();

			BoxSelect.HideBoxSelection();

			IssueManager.Instance.SetIssueInteractability(true);
		}

		private void UpdateActivePlanWindowToSelection()
		{
			List<List<EntityType>> selectedEntityTypes = new List<List<EntityType>>();
			int? selectedTeam = null;
			List<Dictionary<EntityPropertyMetaData, string>> selectedParams = new List<Dictionary<EntityPropertyMetaData, string>>();

			foreach (LineStringSubEntity lse in m_selectedSubEntities)
			{
				selectedEntityTypes.Add(lse.m_entity.EntityTypes);
				if (selectedTeam.HasValue && lse.m_entity.Country != selectedTeam.Value)
					selectedTeam = -1;
				else
					selectedTeam = lse.m_entity.Country;

				Dictionary<EntityPropertyMetaData, string> parameters = new Dictionary<EntityPropertyMetaData, string>();
				foreach (EntityPropertyMetaData p in m_baseLayer.m_propertyMetaData)
				{
					if (p.ShowInEditMode)
						parameters.Add(p, lse.m_entity.GetPropertyMetaData(p));
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
