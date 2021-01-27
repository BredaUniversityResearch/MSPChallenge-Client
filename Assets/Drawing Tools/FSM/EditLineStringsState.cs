using Data_Model.Shipping;
using System.Collections.Generic;
using UnityEngine;

public class EditLineStringsState : FSMState
{
	private const float INSERT_POINT_DELAY = 0.5f;

	//protected LineStringLayer layer;
	protected LineStringLayer baseLayer;
	protected PlanLayer planLayer;

	protected bool draggingSelection = false;
	protected Dictionary<LineStringSubEntity, Dictionary<int, Vector3>> selectionDragStart = null;

	protected bool selectingBox = false;
	protected Dictionary<LineStringSubEntity, HashSet<int>> currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();

	protected HashSet<LineStringSubEntity> selectedSubEntities = new HashSet<LineStringSubEntity>();
	protected Dictionary<LineStringSubEntity, HashSet<int>> selectedPoints = new Dictionary<LineStringSubEntity, HashSet<int>>();
	private Dictionary<LineStringSubEntity, HashSet<int>> highlightedPoints = new Dictionary<LineStringSubEntity, HashSet<int>>();

	bool insertingPointsDisabled = true;
	protected bool selectedRemovedEntity = false;
	float stateEnteredTime = float.MinValue;
	Vector3 reEnableInsertingPointsPosition;

	LineStringSubEntity insertPointPreviewSubEntity = null;
	int insertPointPreviewIndex = -1;

	public EditLineStringsState(FSM fsm, PlanLayer planLayer, HashSet<LineStringSubEntity> selectedSubEntities) : base(fsm)
	{
		this.planLayer = planLayer;
		this.baseLayer = planLayer.BaseLayer as LineStringLayer;
		//if (layer.planLayer != null)
		//{
		//    planLayer = layer.planLayer;
		//    baseLayer = planLayer.BaseLayer as LineStringLayer;
		//}
		SetSelectedSubEntities(selectedSubEntities);
	}

	public override void EnterState(Vector3 currentMousePosition)
	{
		base.EnterState(currentMousePosition);

		UIManager.SetToolbarMode(ToolBar.DrawingMode.Edit);
		UIManager.ToolbarEnable(true, FSM.ToolbarInput.Delete);
		UIManager.ToolbarEnable(false, FSM.ToolbarInput.Abort);
        if (planLayer.BaseLayer is ShippingLineStringLayer)
        {
            UIManager.ToolbarVisibility(true, FSM.ToolbarInput.ChangeDirection);
            UIManager.ToolbarEnable(true, FSM.ToolbarInput.ChangeDirection);
        }
		//UIManager.SetActivePlanWindowInteractability(true, true);

		foreach (LineStringSubEntity lse in selectedSubEntities)
		{
			lse.RedrawGameObject(SubEntityDrawMode.Selected);
		}

		UpdateActivePlanWindowToSelection();

		insertingPointsDisabled = true;
		stateEnteredTime = Time.time;
		reEnableInsertingPointsPosition = currentMousePosition;

		fsm.SetSnappingEnabled(true);
		IssueManager.instance.SetIssueInteractability(false);
	}

	public void SetSelectedSubEntities(HashSet<LineStringSubEntity> subEntities)
	{
		selectedSubEntities = subEntities;
		//Check if this is a line marked for removal, this limits editing and enables recall
		if (this is EditEnergyLineStringsState)
			foreach (LineStringSubEntity line in subEntities)
			{
				EnergyLineStringSubEntity energyLine = line as EnergyLineStringSubEntity;
				selectedRemovedEntity = line.IsPlannedForRemoval();
				bool canRecall = selectedRemovedEntity;
				//If a point connected to this line was moved or removed it cannot be recalled
				if (selectedRemovedEntity)
					foreach (Connection con in energyLine.connections)
						if (con.point.IsPlannedForRemoval() || !con.point.IsNotShownInPlan())
							canRecall = false;
				UIManager.ToolbarEnable(canRecall, FSM.ToolbarInput.Recall);
				break;
			}
		else
			foreach (LineStringSubEntity line in subEntities)
			{
				selectedRemovedEntity = line.IsPlannedForRemoval();
				UIManager.ToolbarEnable(selectedRemovedEntity, FSM.ToolbarInput.Recall);
				break;
			}
		UIManager.SetActivePlanWindowChangeable(!selectedRemovedEntity);
	}

	private void RedrawObject(LineStringSubEntity entity)
	{
		SubEntityDrawMode drawMode = selectedSubEntities.Contains(entity) ? SubEntityDrawMode.Selected : SubEntityDrawMode.Default;
		HashSet<int> selectedPointsForEntity;
		HashSet<int> highlightedPointsForEntity;
		selectedPoints.TryGetValue(entity, out selectedPointsForEntity);
		highlightedPoints.TryGetValue(entity, out highlightedPointsForEntity);
		entity.RedrawGameObject(drawMode, selectedPointsForEntity, highlightedPointsForEntity);
	}

	//private void updateRecallAvailability()
	//{
	//    bool anyRecallableSubEntities = false;
	//    if (selectedSubEntities != null && selectedSubEntities.Count > 0 && planLayer != null)
	//    {
	//        foreach (LineStringSubEntity subEntity in selectedSubEntities)
	//        {
	//            if (planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID()))
	//            {
	//                anyRecallableSubEntities = true;
	//            }
	//        }
	//    }
	//    UIManager.ToolbarEnable(anyRecallableSubEntities, FSM.ToolbarInput.Recall);
	//}

	private void previewInsertPoint(Vector3 position)
	{
		int lineA, lineB;
		getLineAt(position, selectedSubEntities, out insertPointPreviewSubEntity, out lineA, out lineB);
		if (lineA != -1)
		{
			insertPointPreviewIndex = insertPointPreviewSubEntity.AddPointBetween(position, lineA, lineB);

			insertPointPreviewSubEntity.RedrawGameObject(SubEntityDrawMode.Selected, new HashSet<int>() { insertPointPreviewIndex }, null);
		}
	}

	private void getLineAt(Vector3 position, HashSet<LineStringSubEntity> selectedSubEntities, out LineStringSubEntity subEntity, out int lineA, out int lineB)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		lineA = -1;
		lineB = -1;
		subEntity = null;
		float closestDistanceSquared = float.MaxValue;

		foreach (LineStringSubEntity lsse in selectedSubEntities)
		{
			int lsseLineA, lsseLineB;
			float closestDistSq;
			lsse.GetLineAt(position, out lsseLineA, out lsseLineB, out closestDistSq);
			if (lsseLineA != -1 && closestDistSq < closestDistanceSquared)
			{
				lineA = lsseLineA;
				lineB = lsseLineB;
				subEntity = lsse;
				closestDistanceSquared = closestDistSq;
			}
		}
	}

	private void removeInsertPointPreview()
	{
		insertPointPreviewSubEntity.RemovePoints(new HashSet<int>() { insertPointPreviewIndex });

		HashSet<int> originalSelection = null;
		if (selectedPoints.ContainsKey(insertPointPreviewSubEntity))
		{
			originalSelection = selectedPoints[insertPointPreviewSubEntity];
		}
		insertPointPreviewSubEntity.RedrawGameObject(SubEntityDrawMode.Selected, originalSelection, null);
		insertPointPreviewSubEntity = null;
		insertPointPreviewIndex = -1;
	}

	private bool clickingWouldInsertAPoint(Vector3 position)
	{
		if (insertingPointsDisabled) { return false; }

		Dictionary<LineStringSubEntity, HashSet<int>> point = getPointAt(position, selectedSubEntities);
		if (point != null)
		{
			return false;
		}

		int lineA, lineB;
		LineStringSubEntity subEntity;
		getLineAt(position, selectedSubEntities, out subEntity, out lineA, out lineB);

		return lineA != -1;
	}

	private Dictionary<LineStringSubEntity, HashSet<int>> getPointAt(Vector3 position, HashSet<LineStringSubEntity> selectedSubEntities)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		int closestPoint = -1;
		LineStringSubEntity closestLineStringSubEntity = null;
		float closestDistanceSquared = float.MaxValue;

		foreach (LineStringSubEntity subEntity in selectedSubEntities)
		{
			float closestDistSq;
			int point = subEntity.GetPointAt(position, out closestDistSq);
			if (point != -1 && closestDistSq < closestDistanceSquared)
			{
				closestPoint = point;
				closestLineStringSubEntity = subEntity;
				closestDistanceSquared = closestDistSq;
			}
		}

		if (closestPoint == -1)
		{
			return null;
		}

		Dictionary<LineStringSubEntity, HashSet<int>> result = new Dictionary<LineStringSubEntity, HashSet<int>> {{closestLineStringSubEntity, new HashSet<int>() {closestPoint}}};
		return result;
	}

	protected LineStringSubEntity createNewPlanLineString(int persistentID, SubEntityDataCopy dataCopy)
	{
		LineStringEntity newEntity = baseLayer.CreateNewLineStringEntity(dataCopy.entityTypeCopy, planLayer);
		LineStringSubEntity newSubEntity = baseLayer.IsEnergyLineLayer() ? new EnergyLineStringSubEntity(newEntity) : new LineStringSubEntity(newEntity);
		newSubEntity.SetPersistentID(persistentID);
		newEntity.AddSubEntity(newSubEntity);
		newSubEntity.SetDataToCopy(dataCopy);

		fsm.AddToUndoStack(new CreateLineStringOperation(newSubEntity, planLayer, UndoOperation.EditMode.Modify, true));
		newSubEntity.DrawGameObject(baseLayer.LayerGameObject.transform);
		return newSubEntity;
	}

	protected void switchSelectionFromBaseLineStringToDuplicate(LineStringSubEntity baseLineString, LineStringSubEntity duplicate)
	{
		selectedSubEntities.Add(duplicate);
		if (selectedPoints.ContainsKey(baseLineString)) { selectedPoints.Add(duplicate, selectedPoints[baseLineString]); }
		HashSet<int> duplicateSelection = selectedPoints.ContainsKey(duplicate) ? selectedPoints[duplicate] : null;
		selectedSubEntities.Remove(baseLineString);
		selectedPoints.Remove(baseLineString);

		//Change active geom 
		baseLayer.AddPreModifiedEntity(baseLineString.Entity);
		baseLayer.activeEntities.Remove(baseLineString.Entity as LineStringEntity);
		baseLayer.activeEntities.Add(duplicate.Entity as LineStringEntity);

		//Redraw based on activity changes
		duplicate.RedrawGameObject(SubEntityDrawMode.Selected, duplicateSelection);
		baseLineString.RedrawGameObject();
	}

	protected virtual LineStringSubEntity startModifyingSubEntity(LineStringSubEntity subEntity, bool insideUndoBatch)
	{
		if (planLayer == subEntity.Entity.PlanLayer)
		{
			fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
		}
		else
		{
			if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

			LineStringSubEntity duplicate = createNewPlanLineString(subEntity.GetPersistentID(), subEntity.GetDataCopy());
			switchSelectionFromBaseLineStringToDuplicate(subEntity, duplicate);
			subEntity = duplicate;

			if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }
		}
		return subEntity;
	}

	public override void LeftClick(Vector3 worldPosition)
	{
		if (insertPointPreviewSubEntity != null)
		{
			removeInsertPointPreview();
		}
		if (!selectedRemovedEntity)
		{
			// case 1: clicked on a point: select the point
			Dictionary<LineStringSubEntity, HashSet<int>> point = getPointAt(worldPosition, selectedSubEntities);
			if (point != null)
			{
				//If we are an energy layer, ignore the first and last points
				foreach (var kvp in point)
					if (this is EditEnergyLineStringsState && kvp.Key.AreFirstOrLastPoints(kvp.Value))
						selectPoints(point, false);
				selectPoints(point, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
				return;
			}

			// case 2: clicked on a line + shift isn't pressed: add a point on the line and select the new point
			if (!insertingPointsDisabled && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
			{
				AudioMain.PlaySound(AudioMain.ITEM_PLACED);

				int lineA, lineB;
				LineStringSubEntity subEntity;
				getLineAt(worldPosition, selectedSubEntities, out subEntity, out lineA, out lineB);
				if (lineA != -1)
				{
					subEntity = startModifyingSubEntity(subEntity, false);
					subEntity.restrictionNeedsUpdate = true;

					int newPoint = subEntity.AddPointBetween(worldPosition, lineA, lineB);

					Dictionary<LineStringSubEntity, HashSet<int>> newSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
					newSelection.Add(subEntity, new HashSet<int>() { newPoint });

					selectPoints(newSelection, false);
					return;
				}
			}

			// case 3: clicked on a selected linestring: do nothing
			LineStringSubEntity clickedSubEntity = getSubEntityFromSelection(worldPosition, selectedSubEntities);
			if (clickedSubEntity == null && baseLayer != null) { clickedSubEntity = baseLayer.GetSubEntityAt(worldPosition) as LineStringSubEntity; }

			if (clickedSubEntity != null)
			{
				return;
			}

			// case 4: clicked on a linestring that is not selected + shift is pressed: add linestring to selected linestrings
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				clickedSubEntity = baseLayer.GetSubEntityAt(worldPosition) as LineStringSubEntity;

				if (clickedSubEntity != null)
				{
					if (clickedSubEntity.IsPlannedForRemoval())
						return;
					selectedSubEntities.Add(clickedSubEntity);
					clickedSubEntity.RedrawGameObject(SubEntityDrawMode.Selected);
					UpdateActivePlanWindowToSelection();
					return;
				}
			}
		}

		// case 5: clicked somewhere else: deselect all and go back to selecting state
		fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
	}

	protected LineStringSubEntity getSubEntityFromSelection(Vector2 position, HashSet<LineStringSubEntity> selection)
	{
		float maxDistance = VisualizationUtil.GetSelectMaxDistance();

		Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

		List<LineStringSubEntity> collisions = new List<LineStringSubEntity>();

		foreach (LineStringSubEntity subEntity in selection)
		{
			if (positionBounds.Overlaps(subEntity.BoundingBox))
			{
				collisions.Add(subEntity);
			}
		}

		if (collisions.Count == 0) { return null; }

		foreach (LineStringSubEntity collision in collisions)
		{
			if (collision.CollidesWithPoint(position, maxDistance))
			{
				return collision;
			}
		}

		return null;
	}

	public override void DoubleClick(Vector3 position)
	{
		if (selectedRemovedEntity)
			return;

		LineStringSubEntity clickedSubEntity = getSubEntityFromSelection(position, selectedSubEntities);
		if (clickedSubEntity != null)
		{
			HashSet<int> allPoints = new HashSet<int>();
			for (int i = 0; i < clickedSubEntity.GetPointCount(); ++i)
			{
				allPoints.Add(i);
			}
			Dictionary<LineStringSubEntity, HashSet<int>> newSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
			newSelection.Add(clickedSubEntity, allPoints);
			selectPoints(newSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
		}
	}

	protected void selectPoints(Dictionary<LineStringSubEntity, HashSet<int>> newSelection, bool keepPreviousSelection)
	{
		if (!keepPreviousSelection)
		{
			foreach (var kvp in selectedPoints)
			{
				kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected);
			}

			foreach (var kvp in newSelection)
			{
				kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value);
			}

			selectedPoints = newSelection;
		}
		else
		{
			mergeSelectionBIntoSelectionA(selectedPoints, newSelection);

			foreach (var kvp in selectedPoints)
			{
				kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value);
			}
		}

		UIManager.ToolbarEnable(selectedRemovedEntity, FSM.ToolbarInput.Recall);
		UIManager.SetActivePlanWindowChangeable(!selectedRemovedEntity);
	}

	private void mergeSelectionBIntoSelectionA(Dictionary<LineStringSubEntity, HashSet<int>> a, Dictionary<LineStringSubEntity, HashSet<int>> b)
	{
		foreach (var kvp in b)
		{
			if (!a.ContainsKey(kvp.Key))
			{
				a.Add(kvp.Key, kvp.Value);
			}
			else
			{
				foreach (int index in kvp.Value)
				{
					a[kvp.Key].Add(index);
				}
			}
		}
	}

	private bool draggingWouldMoveCurrentSelection(Vector3 dragStart)
	{
		Dictionary<LineStringSubEntity, HashSet<int>> point = getPointAt(dragStart, selectedSubEntities);
		if (point != null)
		{
			LineStringSubEntity lsse = null;
			int pointIndex = -1;
			foreach (var kvp in point)
			{
				lsse = kvp.Key;
				foreach (int i in kvp.Value) { pointIndex = i; }
			}

			return selectedPoints.ContainsKey(lsse) && selectedPoints[lsse].Contains(pointIndex);
		}

		LineStringSubEntity subEntity = getSubEntityFromSelection(dragStart, selectedSubEntities);

		return subEntity != null && selectedPoints.ContainsKey(subEntity) && selectedPoints[subEntity].Count == subEntity.GetPointCount();
	}

	public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
	{
		if (insertingPointsDisabled)
		{
			reEnableInsertingPointsPosition = currentPosition;
		}

		if (insertPointPreviewSubEntity != null)
		{
			removeInsertPointPreview();
		}

		if (!draggingSelection && !selectingBox)
		{
			if (cursorIsOverUI)
			{
				fsm.SetCursor(FSM.CursorType.Default);
			}
			else if (!selectedRemovedEntity)
			{
				if (clickingWouldInsertAPoint(currentPosition))
				{
					previewInsertPoint(currentPosition);
					fsm.SetCursor(FSM.CursorType.Insert);
				}
				else
				{
					if (draggingWouldMoveCurrentSelection(currentPosition))
					{
						fsm.SetCursor(FSM.CursorType.Move);
					}
					else
					{
						Dictionary<LineStringSubEntity, HashSet<int>> point = getPointAt(currentPosition, selectedSubEntities);
						UpdateHighlightingPoints(point);
						if (point != null)
						{
							fsm.SetCursor(FSM.CursorType.Move);
						}
						else
						{
							fsm.SetCursor(FSM.CursorType.Default);
						}
					}
				}
			}
		}
	}

	private void UpdateHighlightingPoints(Dictionary<LineStringSubEntity, HashSet<int>> newHighlightedPoints)
	{
		Dictionary<LineStringSubEntity, HashSet<int>> oldHighlightedPoints = highlightedPoints;
		if (newHighlightedPoints != null)
		{
			highlightedPoints = newHighlightedPoints;
		}
		else
		{
			highlightedPoints = new Dictionary<LineStringSubEntity, HashSet<int>>();
		}

		foreach (KeyValuePair<LineStringSubEntity, HashSet<int>> kvp in oldHighlightedPoints)
		{
			if (!highlightedPoints.ContainsKey(kvp.Key))
			{
				RedrawObject(kvp.Key);
			}
		}

		foreach (KeyValuePair<LineStringSubEntity, HashSet<int>> kvp in highlightedPoints)
		{
			RedrawObject(kvp.Key);
		}
	}

	private void createUndoForDraggedSelection()
	{
		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		HashSet<LineStringSubEntity> selectedPointsKeys = new HashSet<LineStringSubEntity>(selectedPoints.Keys);
		foreach (LineStringSubEntity subEntity in selectedPointsKeys)
		{
			startModifyingSubEntity(subEntity, true);
		}

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition)
	{
		if (insertPointPreviewSubEntity != null)
		{
			removeInsertPointPreview();
		}
		if (selectedRemovedEntity)
			return;

		if (draggingWouldMoveCurrentSelection(dragStartPosition))
		{
			draggingSelection = true;
		}
		else
		{
			Dictionary<LineStringSubEntity, HashSet<int>> point = getPointAt(dragStartPosition, selectedSubEntities);
			if (point != null)
			{
				draggingSelection = true;
				selectPoints(point, false);
			}
		}

		if (draggingSelection)
		{
			createUndoForDraggedSelection();

			// if the user is dragging a point, this offset is used to make sure the user is dragging the center of the point (to make snapping work correctly)
			Vector3 offset = getSelectionDragOffset(dragStartPosition);

			selectionDragStart = new Dictionary<LineStringSubEntity, Dictionary<int, Vector3>>();
			foreach (var kvp in selectedPoints)
			{
				Dictionary<int, Vector3> dragStartEntry = new Dictionary<int, Vector3>();
				foreach (int index in kvp.Value)
				{
					dragStartEntry.Add(index, kvp.Key.GetPointPosition(index) + offset);
				}
				selectionDragStart.Add(kvp.Key, dragStartEntry);
			}
		}
		else
		{
			selectPoints(new Dictionary<LineStringSubEntity, HashSet<int>>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

			selectingBox = true;
			currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();

			BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);
		}
	}

	private Vector3 getSelectionDragOffset(Vector3 dragStartPosition)
	{
		Vector3 closestPoint = Vector3.zero;
		float closestPointSqrDistance = float.MaxValue;

		foreach (var kvp in selectedPoints)
		{
			foreach (int index in kvp.Value)
			{
				Vector3 selectedPoint = kvp.Key.GetPointPosition(index);
				float sqrDistance = (selectedPoint - dragStartPosition).sqrMagnitude;
				if (sqrDistance < closestPointSqrDistance)
				{
					closestPoint = selectedPoint;
					closestPointSqrDistance = sqrDistance;
				}
			}
		}

		float maxDistance = VisualizationUtil.GetSelectMaxDistance();
		if (closestPointSqrDistance < maxDistance * maxDistance)
		{
			return dragStartPosition - closestPoint;
		}
		else
		{
			return Vector3.zero;
		}
	}

	public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
	{
		if (selectedRemovedEntity)
			return;

		if (draggingSelection)
		{
			UpdateSelectionDragPositions(currentPosition - dragStartPosition);
		}
		else if (selectingBox)
		{
			UpdateBoxSelection(dragStartPosition, currentPosition);
		}
	}

	protected virtual void UpdateSelectionDragPositions(Vector3 offset)
	{
		foreach (var kvp in selectedPoints)
		{
			foreach (int selectedPoint in kvp.Value)
			{
				kvp.Key.SetPointPosition(selectedPoint, selectionDragStart[kvp.Key][selectedPoint] + offset);
			}
			if (kvp.Value.Count != kvp.Key.GetPointCount())
				kvp.Key.restrictionNeedsUpdate = true;
			kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value, null);
		}
	}

	protected void UpdateBoxSelection(Vector3 dragStartPosition, Vector3 currentPosition)
	{
		BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);

		Dictionary<LineStringSubEntity, HashSet<int>> selectionsInBox = getPointsInBox(dragStartPosition, currentPosition, selectedSubEntities);
		foreach (var kvp in selectionsInBox)
		{
			LineStringSubEntity subEntity = kvp.Key;
			bool redraw = false;
			if (!currentBoxSelection.ContainsKey(subEntity)) { redraw = true; }
			else
			{
				HashSet<int> currentPoints = currentBoxSelection[subEntity];
				foreach (int pointIndex in kvp.Value)
				{
					if (!currentPoints.Contains(pointIndex))
					{
						redraw = true;
						break;
					}
				}
			}
			if (redraw)
			{
				HashSet<int> alreadySelected = selectedPoints.ContainsKey(subEntity) ? selectedPoints[subEntity] : null;
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, alreadySelected, new HashSet<int>(kvp.Value));
			}
		}

		foreach (var kvp in currentBoxSelection)
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
					if (!boxPoints.Contains(pointIndex))
					{
						redraw = true;
						hoverPoints = new HashSet<int>(boxPoints);
						break;
					}
				}
			}
			if (redraw)
			{
				HashSet<int> alreadySelected = selectedPoints.ContainsKey(subEntity) ? selectedPoints[subEntity] : null;
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, alreadySelected, hoverPoints);
			}
		}

		currentBoxSelection = selectionsInBox;
	}

	private Dictionary<LineStringSubEntity, HashSet<int>> getPointsInBox(Vector3 boxCornerA, Vector3 boxCornerB, HashSet<LineStringSubEntity> selectedSubEntities)
	{
		Vector3 min = Vector3.Min(boxCornerA, boxCornerB);
		Vector3 max = Vector3.Max(boxCornerA, boxCornerB);

		Dictionary<LineStringSubEntity, HashSet<int>> result = new Dictionary<LineStringSubEntity, HashSet<int>>();

		foreach (LineStringSubEntity subEntity in selectedSubEntities)
		{
			HashSet<int> points = subEntity.GetPointsInBox(min, max);
			if (points != null)
			{
				result.Add(subEntity, points);
			}
		}

		return result;
	}

	public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
	{
		if (draggingSelection)
		{
			AudioMain.PlaySound(AudioMain.ITEM_MOVED);

			UpdateSelectionDragPositions(dragFinalPosition - dragStartPosition);
			draggingSelection = false;
		}
		else if (selectingBox)
		{
			UpdateBoxSelection(dragStartPosition, dragFinalPosition);

			selectPoints(currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

			BoxSelect.HideBoxSelection();
			selectingBox = false;
			currentBoxSelection = new Dictionary<LineStringSubEntity, HashSet<int>>();
		}
	}

	protected virtual void deleteSelection()
	{
		fsm.AddToUndoStack(new BatchUndoOperationMarker());

        if (selectedPoints.Count == 0)
        {
            //Delete all selected
            foreach (LineStringSubEntity subEntity in selectedSubEntities)
            {
                if (subEntity.Entity.PlanLayer == planLayer)
                {
                    fsm.AddToUndoStack(new RemoveLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Modify));
                    baseLayer.RemoveSubEntity(subEntity);
                    subEntity.RemoveGameObject();
                }
                else
                {
                    fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                    baseLayer.RemoveSubEntity(subEntity);
                    subEntity.RedrawGameObject();
                }
            }
        }
        else
        {
            //Delete selected points
            List<LineStringSubEntity> selectedPointsKeys = new List<LineStringSubEntity>(selectedPoints.Keys);
            foreach (LineStringSubEntity subEntity in selectedPointsKeys)
            {
                if (subEntity.GetPointCount() - selectedPoints[subEntity].Count < 2)
                {
                    // remove linestring if it has fewer than 2 points left after deletion
                    if (subEntity.Entity.PlanLayer == planLayer)
                    {
                        fsm.AddToUndoStack(new RemoveLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Modify));
                        baseLayer.RemoveSubEntity(subEntity);
                        subEntity.RemoveGameObject();
                    }
                    else
                    {
                        fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                        baseLayer.RemoveSubEntity(subEntity);
                        subEntity.RedrawGameObject();
                    }

                    selectedSubEntities.Remove(subEntity);
                }
                else
                {
                    LineStringSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);

                    subEntityToModify.RemovePoints(selectedPoints[subEntityToModify]);

                    subEntity.restrictionNeedsUpdate = true;
                    subEntityToModify.RedrawGameObject();
                }
            }
        }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
		fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
	}

	private void undoDeleteForSelection()
	{
		if (selectedRemovedEntity)
		{
			fsm.AddToUndoStack(new BatchUndoOperationMarker());
			foreach (LineStringSubEntity subEntity in selectedSubEntities)
			{
				if (planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID()))
				{
					fsm.AddToUndoStack(new ModifyLineStringRemovalPlanOperation(subEntity, planLayer, true));
					planLayer.RemovedGeometry.Remove(subEntity.GetPersistentID());
					subEntity.RestoreDependencies();
					subEntity.RedrawGameObject(SubEntityDrawMode.Selected, selectedPoints.ContainsKey(subEntity) ? selectedPoints[subEntity] : null);
				}
			}
			fsm.AddToUndoStack(new BatchUndoOperationMarker());
			fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
		}
	}

	private void ChangeSelectionDirection()
	{
		fsm.AddToUndoStack(new BatchUndoOperationMarker());
		LineStringSubEntity[] localSelectedEntities = new LineStringSubEntity[selectedSubEntities.Count];
		selectedSubEntities.CopyTo(localSelectedEntities);
		foreach (LineStringSubEntity subEntity in localSelectedEntities)
		{
			string oldDirection;
			if (subEntity.Entity.DoesPropertyExist(ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY))
			{
				oldDirection = subEntity.Entity.GetMetaData(ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY);
			}
			else
			{
				oldDirection = ShippingLineStringEntity.DIRECTION_DEFAULT;
			}

			string newDirection = ShippingLineStringEntity.CycleToNextDirection(oldDirection);

			LineStringSubEntity newSubEntity = startModifyingSubEntity(subEntity, true);

			fsm.AddToUndoStack(new UndoOperationChangeMeta(newSubEntity.Entity, ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY, oldDirection, newDirection));

			newSubEntity.Entity.SetMetaData(ShippingLineStringEntity.SHIPPING_DIRECTION_META_KEY, newDirection);

			newSubEntity.Entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Selected);
		}
		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	//private void simplifySelection()
	//{
	//	UIManager.CreateSingleValueWindow("Simplify line string", "tolerance", "0.5", 200, (value) =>
	//	{
	//		fsm.AddToUndoStack(new BatchUndoOperationMarker());

	//		foreach (LineStringSubEntity subEntity in selectedSubEntities)
	//		{
	//			fsm.AddToUndoStack(new ModifyLineStringOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
	//			subEntity.Simplify(Util.ParseToFloat(value, 0.5f));
	//			subEntity.RedrawGameObject(SubEntityDrawMode.Selected);
	//		}

	//		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	//	});
	//}

	public override void HandleKeyboardEvents()
	{
		// hasn't got anything to do with keyboard events; added here because this function is called every frame
		if (insertingPointsDisabled && Time.time - stateEnteredTime > INSERT_POINT_DELAY)
		{
			insertingPointsDisabled = false;
			MouseMoved(reEnableInsertingPointsPosition, reEnableInsertingPointsPosition, false);
		}

		if (Input.GetKeyDown(KeyCode.Delete))
		{
			deleteSelection();
		}

		if (Input.GetKeyDown(KeyCode.Return))
		{
			if (baseLayer.IsEnergyLineLayer())
				fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, planLayer));
			else
				fsm.SetCurrentState(new StartCreatingLineStringState(fsm, planLayer));
		}
	}

	public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
	{
		switch (toolbarInput)
		{
		case FSM.ToolbarInput.Create:
			if (baseLayer.IsEnergyLineLayer())
				fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, planLayer));
			else
				fsm.SetCurrentState(new StartCreatingLineStringState(fsm, planLayer));
			break;
		case FSM.ToolbarInput.Delete:
			deleteSelection();
			break;
		case FSM.ToolbarInput.Abort:
			fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
			break;
		//case FSM.ToolbarInput.Simplify:
		//	simplifySelection();
		//	break;
		case FSM.ToolbarInput.SelectAll:
			if (baseLayer.IsEnergyLineLayer())
				fsm.SetCurrentState(new EditEnergyLineStringsState(fsm, planLayer, new HashSet<LineStringSubEntity>((baseLayer as LineStringLayer).GetAllSubEntities())));
			else
				fsm.SetCurrentState(new EditLineStringsState(fsm, planLayer, new HashSet<LineStringSubEntity>((baseLayer as LineStringLayer).GetAllSubEntities())));
			break;
		case FSM.ToolbarInput.Recall:
			undoDeleteForSelection();
			break;
		case FSM.ToolbarInput.ChangeDirection:
			ChangeSelectionDirection();
			break;
		}
	}

	public override void HandleEntityTypeChange(List<EntityType> newTypes)
	{
		List<LineStringSubEntity> subEntitiesWithDifferentTypes = new List<LineStringSubEntity>();

		//Find subentities with changed entity types
		foreach (LineStringSubEntity subEntity in selectedSubEntities)
		{
			if (subEntity.Entity.EntityTypes.Count != newTypes.Count)
			{
				subEntitiesWithDifferentTypes.Add(subEntity);
				continue;
			}
			foreach (EntityType type in subEntity.Entity.EntityTypes)
			{
				if (!newTypes.Contains(type))
				{
					subEntitiesWithDifferentTypes.Add(subEntity);
					break;
				}
			}
		}

		if (subEntitiesWithDifferentTypes.Count == 0) { return; }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (LineStringSubEntity subEntity in subEntitiesWithDifferentTypes)
		{
			LineStringSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.EntityTypes = newTypes;
            subEntityToModify.RedrawGameObject(SubEntityDrawMode.Selected, selectedPoints.ContainsKey(subEntityToModify) ? selectedPoints[subEntityToModify] : null);
        }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void HandleTeamChange(int newTeam)
	{
		List<LineStringSubEntity> subEntitiesWithDifferentTeam = new List<LineStringSubEntity>();

		//Find subentities with changed entity types
		foreach (LineStringSubEntity subEntity in selectedSubEntities)
		{
			if (subEntity.Entity.Country != newTeam)
			{
				subEntitiesWithDifferentTeam.Add(subEntity);
			}
		}

		if (subEntitiesWithDifferentTeam.Count == 0) { return; }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (LineStringSubEntity subEntity in subEntitiesWithDifferentTeam)
		{
			LineStringSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.Country = newTeam;
		}

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void HandleParameterChange(EntityPropertyMetaData parameter, string newValue)
	{
		List<LineStringSubEntity> subEntitiesWithDifferentParams = new List<LineStringSubEntity>();

		//Find subentities with changed entity types
		foreach (LineStringSubEntity subEntity in selectedSubEntities)
		{
			if (subEntity.Entity.GetPropertyMetaData(parameter) != newValue)
			{
				subEntitiesWithDifferentParams.Add(subEntity);
			}
		}

		if (subEntitiesWithDifferentParams.Count == 0) { return; }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (LineStringSubEntity subEntity in subEntitiesWithDifferentParams)
		{
			LineStringSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.SetPropertyMetaData(parameter, newValue);
		}

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void ExitState(Vector3 currentMousePosition)
	{
		foreach (LineStringSubEntity lsse in selectedSubEntities)
		{
			lsse.RedrawGameObject();
		}
		selectedSubEntities = new HashSet<LineStringSubEntity>();

		BoxSelect.HideBoxSelection();

		IssueManager.instance.SetIssueInteractability(true);

		// make sure the entity type dropdown shows a valid value
		//UIManager.SetCurrentEntityTypeSelection(UIManager.GetCurrentEntityTypeSelection());
	}

	private void UpdateActivePlanWindowToSelection()
	{
		List<List<EntityType>> selectedEntityTypes = new List<List<EntityType>>();
		int? selectedTeam = null;
		List<Dictionary<EntityPropertyMetaData, string>> selectedParams = new List<Dictionary<EntityPropertyMetaData, string>>();

		foreach (LineStringSubEntity lse in selectedSubEntities)
		{
			selectedEntityTypes.Add(lse.Entity.EntityTypes);
			if (selectedTeam.HasValue && lse.Entity.Country != selectedTeam.Value)
				selectedTeam = -1;
			else
				selectedTeam = lse.Entity.Country;

			Dictionary<EntityPropertyMetaData, string> parameters = new Dictionary<EntityPropertyMetaData, string>();
			foreach (EntityPropertyMetaData p in baseLayer.propertyMetaData)
			{
				if (p.ShowInEditMode)
					parameters.Add(p, lse.Entity.GetPropertyMetaData(p));
			}
			selectedParams.Add(parameters);
		}

		UIManager.SetActiveplanWindowToSelection(
			selectedEntityTypes.Count > 0 ? selectedEntityTypes : null,
			selectedTeam ?? -2,
			selectedParams);
	}
}
