using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;

public class EditPolygonsState : FSMState
{
	private const float INSERT_POINT_DELAY = 0.5f;

	//protected PolygonLayer layer;
	protected PolygonLayer baseLayer;
	protected PlanLayer planLayer;

	protected bool draggingSelection = false;
	protected Dictionary<PolygonSubEntity, Dictionary<int, Vector3>> selectionDragStart = null;

	protected bool selectingBox = false;
	protected Dictionary<PolygonSubEntity, HashSet<int>> currentBoxSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();

	protected HashSet<PolygonSubEntity> selectedSubEntities = new HashSet<PolygonSubEntity>();
	protected HashSet<EnergyPointSubEntity> selectedSourcePoints = new HashSet<EnergyPointSubEntity>();
	private Dictionary<PolygonSubEntity, HashSet<int>> selectedPoints = new Dictionary<PolygonSubEntity, HashSet<int>>();
	private Dictionary<PolygonSubEntity, HashSet<int>> highlightedPoints = new Dictionary<PolygonSubEntity, HashSet<int>>(); 

	//Dictionary<PolygonSubEntity, HashSet<int>> previousHover = null;

	bool insertingPointsDisabled = true;
	protected bool selectedRemovedEntity = false;
	float stateEnteredTime = float.MinValue;
	Vector3 reEnableInsertingPointsPosition;

	PolygonSubEntity insertPointPreviewSubEntity = null;
	int insertPointPreviewIndex = -1;

	public EditPolygonsState(FSM fsm, PlanLayer planLayer, HashSet<PolygonSubEntity> selectedSubEntities) : base(fsm)
	{
		this.planLayer = planLayer;
		this.baseLayer = planLayer.BaseLayer as PolygonLayer;
		//if (layer.planLayer != null)
		//{
		//    planLayer = layer.planLayer;
		//    baseLayer = planLayer.BaseLayer as PolygonLayer;
		//}
		SetSelectedSubEntities(selectedSubEntities);
	}

	public override void EnterState(Vector3 currentMousePosition)
	{
		base.EnterState(currentMousePosition);

		UIManager.SetToolbarMode(ToolBar.DrawingMode.Edit);
		UIManager.ToolbarEnable(true, FSM.ToolbarInput.Delete);
		UIManager.ToolbarEnable(false, FSM.ToolbarInput.Abort);
		//UIManager.SetActivePlanWindowInteractability(true, true);
		IssueManager.instance.SetIssueInteractability(false);
		
		foreach (PolygonSubEntity pse in selectedSubEntities)
		{
			pse.PerformValidityCheck(false);
			pse.RedrawGameObject(SubEntityDrawMode.Selected);
		}

		UpdateActivePlanWindowToSelection();

		insertingPointsDisabled = true;
		stateEnteredTime = Time.time;
		reEnableInsertingPointsPosition = currentMousePosition;

		fsm.SetSnappingEnabled(true);
	}

	//private void updateRecallAvailability()
	//{
	//    bool anyRecallableSubEntities = false;
	//    if (selectedSubEntities != null && selectedSubEntities.Count > 0 /*&& planLayer != null*/)
	//    {
	//        foreach (PolygonSubEntity subEntity in selectedSubEntities)
	//        {
	//            if (planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID()))
	//            {
	//                anyRecallableSubEntities = true;
	//            }
	//        }
	//    }
	//    UIManager.ToolbarEnable(anyRecallableSubEntities, FSM.ToolbarInput.Recall);
	//}

	private void RedrawObject(PolygonSubEntity entity)
	{
		SubEntityDrawMode drawMode = selectedSubEntities.Contains(entity) ? SubEntityDrawMode.Selected : SubEntityDrawMode.Default;
		HashSet<int> selectedPointsForEntity;
		HashSet<int> highlightedPointsForEntity;
		selectedPoints.TryGetValue(entity, out selectedPointsForEntity);
		highlightedPoints.TryGetValue(entity, out highlightedPointsForEntity);
		entity.RedrawGameObject(drawMode, selectedPointsForEntity, highlightedPointsForEntity);
	}

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

	private void getLineAt(Vector3 position, HashSet<PolygonSubEntity> selectedSubEntities, out PolygonSubEntity subEntity, out int lineA, out int lineB)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		lineA = -1;
		lineB = -1;
		subEntity = null;
		float closestDistanceSquared = float.MaxValue;

		foreach (PolygonSubEntity pse in selectedSubEntities)
		{
			int pseLineA, pseLineB;
			float closestDistSq;
			pse.GetLineAt(position, out pseLineA, out pseLineB, out closestDistSq);
			if (pseLineA != -1 && closestDistSq < closestDistanceSquared)
			{
				lineA = pseLineA;
				lineB = pseLineB;
				subEntity = pse;
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

		Dictionary<PolygonSubEntity, HashSet<int>> point = getPointAt(position, selectedSubEntities);
		if (point != null)
		{
			return false;
		}

		int lineA, lineB;
		PolygonSubEntity subEntity;
		getLineAt(position, selectedSubEntities, out subEntity, out lineA, out lineB);

		return lineA != -1;
	}

	private Dictionary<PolygonSubEntity, HashSet<int>> getPointAt(Vector3 position, HashSet<PolygonSubEntity> selectedSubEntities)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		int closestPoint = -1;
		PolygonSubEntity closestPolygonSubEntity = null;
		float closestDistanceSquared = float.MaxValue;

		foreach (PolygonSubEntity subEntity in selectedSubEntities)
		{
			float closestDistSq;
			int point = subEntity.GetPointAt(position, out closestDistSq);
			if (point != -1 && closestDistSq < closestDistanceSquared)
			{
				closestPoint = point;
				closestPolygonSubEntity = subEntity;
				closestDistanceSquared = closestDistSq;
			}
		}

		if (closestPoint == -1) { return null; }

		Dictionary<PolygonSubEntity, HashSet<int>> result = new Dictionary<PolygonSubEntity, HashSet<int>>();
		result.Add(closestPolygonSubEntity, new HashSet<int>() { closestPoint });
		return result;
	}

	protected PolygonSubEntity createNewPlanPolygon(SubEntityDataCopy dataCopy, int persistentID)
	{
		PolygonEntity newEntity = baseLayer.CreateNewPolygonEntity(dataCopy.entityTypeCopy, planLayer);
		PolygonSubEntity newSubEntity = baseLayer.editingType == AbstractLayer.EditingType.SourcePolygon ? new EnergyPolygonSubEntity(newEntity) : new PolygonSubEntity(newEntity);
		newSubEntity.SetPersistentID(persistentID);
		((PolygonEntity)newSubEntity.Entity).AddSubEntity(newSubEntity);
		newSubEntity.SetDataToCopy(dataCopy);
        newSubEntity.edited = true;

        fsm.AddToUndoStack(new CreatePolygonOperation(newSubEntity, planLayer, UndoOperation.EditMode.Modify, true));
		newSubEntity.DrawGameObject(baseLayer.LayerGameObject.transform);
		return newSubEntity;
	}

	protected void switchSelectionFromBasePolygonToDuplicate(PolygonSubEntity basePolygon, PolygonSubEntity duplicate)
	{
		AddSelectedSubEntity(duplicate, false);
		if (selectedPoints.ContainsKey(basePolygon)) { selectedPoints.Add(duplicate, selectedPoints[basePolygon]); }
		HashSet<int> duplicateSelection = selectedPoints.ContainsKey(duplicate) ? selectedPoints[duplicate] : null;
		RemoveSelectedSubEntity(basePolygon);
		selectedPoints.Remove(basePolygon);

		//Change active geom 
		baseLayer.AddPreModifiedEntity(basePolygon.Entity);
		baseLayer.activeEntities.Remove(basePolygon.Entity as PolygonEntity);
		baseLayer.activeEntities.Add(duplicate.Entity as PolygonEntity);
		if (baseLayer.IsEnergyPolyLayer())
		{
			EnergyPolygonSubEntity baseEnergyPoly = (basePolygon as EnergyPolygonSubEntity);
			EnergyPolygonSubEntity energyDuplicate = (duplicate as EnergyPolygonSubEntity);
			baseEnergyPoly.DeactivateSourcePoint();
			energyDuplicate.sourcePoint.RedrawGameObject();
		}

		//Redraw based on activity changes
		duplicate.RedrawGameObject(SubEntityDrawMode.Selected, duplicateSelection);
		basePolygon.RedrawGameObject();
	}

	protected virtual PolygonSubEntity startModifyingSubEntity(PolygonSubEntity subEntity, bool insideUndoBatch)
	{
		if (subEntity.Entity.PlanLayer == planLayer)
		{
			fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
            subEntity.edited = true;
		}
		else
		{
			if (!insideUndoBatch) { fsm.AddToUndoStack(new BatchUndoOperationMarker()); }

			PolygonSubEntity duplicate = createNewPlanPolygon(subEntity.GetDataCopy(), subEntity.GetPersistentID());
			switchSelectionFromBasePolygonToDuplicate(subEntity, duplicate);
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
			// case 0: control is pressed: try to select another sub entity at this position
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				selectNextSubEntity(worldPosition);
				return;
			}

			// case 1: clicked on a point: select the point
			Dictionary<PolygonSubEntity, HashSet<int>> point = getPointAt(worldPosition, selectedSubEntities);
			if (point != null)
			{
				selectPoints(point, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
				return;
			}

			// case 2: clicked on a line + shift isn't pressed: add a point on the line and select the new point
			if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
			{
				AudioMain.PlaySound(AudioMain.ITEM_PLACED);

				int lineA, lineB;
				PolygonSubEntity subEntity;
				getLineAt(worldPosition, selectedSubEntities, out subEntity, out lineA, out lineB);
				if (lineA != -1)
				{
					subEntity = startModifyingSubEntity(subEntity, false);
					subEntity.restrictionNeedsUpdate = true;

					int newPoint = subEntity.AddPointBetween(worldPosition, lineA, lineB);

					Dictionary<PolygonSubEntity, HashSet<int>> newSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();
					newSelection.Add(subEntity, new HashSet<int>() { newPoint });

					selectPoints(newSelection, false);
					return;
				}
			}

			// case 3: clicked on a selected polygon: do nothing
			PolygonSubEntity clickedSubEntity = getSubEntityFromSelection(worldPosition, selectedSubEntities);
			if (clickedSubEntity != null)
			{
				return;
			}

			// case 4: clicked on a polygon that is not selected + shift is pressed: add polygon to selected polygons
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				clickedSubEntity = baseLayer.GetSubEntityAt(worldPosition) as PolygonSubEntity;

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
		fsm.SetCurrentState(new SelectPolygonsState(fsm, planLayer));
	}

	private PolygonSubEntity getSubEntityFromSelection(Vector2 position, HashSet<PolygonSubEntity> selection)
	{
        float maxDistance = VisualizationUtil.GetSelectMaxDistancePolygon();

		Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

		List<PolygonSubEntity> collisions = new List<PolygonSubEntity>();

		foreach (PolygonSubEntity subEntity in selection)
		{
			if (positionBounds.Overlaps(subEntity.BoundingBox))
			{
				collisions.Add(subEntity);
			}
		}

		if (collisions.Count == 0) { return null; }

		foreach (PolygonSubEntity collision in collisions)
		{
			if (collision.CollidesWithPoint(position, maxDistance))
			{
				return collision;
			}
		}

		return null;
	}

	private void selectNextSubEntity(Vector3 position)
	{
		List<SubEntity> subEntities = baseLayer.GetSubEntitiesAt(position);
		if (baseLayer != null) { subEntities.AddRange(baseLayer.GetSubEntitiesAt(position)); }
		if (subEntities.Count > 0)
		{
			int selectIndex = 0;
			for (int i = 0; i < subEntities.Count; ++i)
			{
				if (selectedSubEntities.Contains(subEntities[i] as PolygonSubEntity))
				{
					selectIndex = (i + 1) % subEntities.Count;
				}
			}
			if (baseLayer.IsEnergyPolyLayer())
				fsm.SetCurrentState(new EditEnergyPolygonState(fsm, planLayer, new HashSet<PolygonSubEntity> { subEntities[selectIndex] as PolygonSubEntity }));
			else
				fsm.SetCurrentState(new EditPolygonsState(fsm, planLayer, new HashSet<PolygonSubEntity> { subEntities[selectIndex] as PolygonSubEntity }));
		}
	}

	public override void DoubleClick(Vector3 position)
	{
		if (selectedRemovedEntity)
			return;

		PolygonSubEntity clickedSubEntity = getSubEntityFromSelection(position, selectedSubEntities);
		if (clickedSubEntity != null)
		{
			HashSet<int> allPoints = new HashSet<int>();
			int totalPointCount = clickedSubEntity.GetTotalPointCount();
			for (int i = 0; i < totalPointCount; ++i)
			{
				allPoints.Add(i);
			}
			Dictionary<PolygonSubEntity, HashSet<int>> newSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();
			newSelection.Add(clickedSubEntity, allPoints);
			selectPoints(newSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
		}
	}

	protected void selectPoints(Dictionary<PolygonSubEntity, HashSet<int>> newSelection, bool keepPreviousSelection)
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

			//SetSelectedSubEntities(newSelection);
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

		//UIManager.ToolbarEnable(selectedPoints.Count > 0, FSM.ToolbarInput.Delete);
		//updateRecallAvailability();
		UIManager.SetActivePlanWindowChangeable(!selectedRemovedEntity);
		UIManager.ToolbarEnable(selectedRemovedEntity, FSM.ToolbarInput.Recall);
	}

	private void mergeSelectionBIntoSelectionA(Dictionary<PolygonSubEntity, HashSet<int>> a, Dictionary<PolygonSubEntity, HashSet<int>> b)
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
		Dictionary<PolygonSubEntity, HashSet<int>> point = getPointAt(dragStart, selectedSubEntities);
		if (point != null)
		{
			PolygonSubEntity pse = null;
			int pointIndex = -1;
			foreach (var kvp in point)
			{
				pse = kvp.Key;
				foreach (int i in kvp.Value) { pointIndex = i; }
			}

			return selectedPoints.ContainsKey(pse) && selectedPoints[pse].Contains(pointIndex);
		}

		PolygonSubEntity subEntity = getSubEntityFromSelection(dragStart, selectedSubEntities);

		return subEntity != null && selectedPoints.ContainsKey(subEntity) && selectedPoints[subEntity].Count == subEntity.GetTotalPointCount();
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
						Dictionary<PolygonSubEntity, HashSet<int>> point = getPointAt(currentPosition, selectedSubEntities);
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

	private void UpdateHighlightingPoints(Dictionary<PolygonSubEntity, HashSet<int>> newHighlightedPoints)
	{
		Dictionary<PolygonSubEntity, HashSet<int>> oldHighlightedPoints = highlightedPoints;
		if (newHighlightedPoints != null)
		{
			highlightedPoints = newHighlightedPoints;
		}
		else
		{
			highlightedPoints = new Dictionary<PolygonSubEntity, HashSet<int>>();
		}

		foreach (KeyValuePair<PolygonSubEntity, HashSet<int>> kvp in oldHighlightedPoints)
		{
			if (!highlightedPoints.ContainsKey(kvp.Key))
			{
				RedrawObject(kvp.Key);
			}
		}

		foreach (KeyValuePair<PolygonSubEntity, HashSet<int>> kvp in highlightedPoints)
		{
			RedrawObject(kvp.Key);
		}
	}

	protected void createUndoForDraggedSelection()
	{
		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		HashSet<PolygonSubEntity> selectedPointsKeys = new HashSet<PolygonSubEntity>(selectedPoints.Keys);
		foreach (PolygonSubEntity subEntity in selectedPointsKeys)
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
			Dictionary<PolygonSubEntity, HashSet<int>> point = getPointAt(dragStartPosition, selectedSubEntities);
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

			selectionDragStart = new Dictionary<PolygonSubEntity, Dictionary<int, Vector3>>();
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
			selectPoints(new Dictionary<PolygonSubEntity, HashSet<int>>(), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

			selectingBox = true;
			currentBoxSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();

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
			updateSelectionDragPositions(currentPosition - dragStartPosition, true);//Was false, set to true to update energypoly centerpoints
		}
		else if (selectingBox)
		{
			updateBoxSelection(dragStartPosition, currentPosition);
		}
	}

	protected void updateSelectionDragPositions(Vector3 offset, bool updateBoundingBoxes)
	{
		foreach (var kvp in selectedPoints)
		{
			foreach (int selectedPoint in kvp.Value)
			{
				kvp.Key.SetPointPosition(selectedPoint, selectionDragStart[kvp.Key][selectedPoint] + offset, updateBoundingBoxes);
			}
			if (updateBoundingBoxes && kvp.Value.Count != kvp.Key.GetPolygonPointCount())
				kvp.Key.restrictionNeedsUpdate = true;
			kvp.Key.PerformValidityCheck(false);
			kvp.Key.RedrawGameObject(SubEntityDrawMode.Selected, kvp.Value, null);
		}
	}

	protected void updateBoxSelection(Vector3 dragStartPosition, Vector3 currentPosition)
	{
		BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);

		Dictionary<PolygonSubEntity, HashSet<int>> selectionsInBox = getPointsInBox(dragStartPosition, currentPosition, selectedSubEntities);
		foreach (var kvp in selectionsInBox)
		{
			PolygonSubEntity subEntity = kvp.Key;
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
			PolygonSubEntity subEntity = kvp.Key;
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

	private Dictionary<PolygonSubEntity, HashSet<int>> getPointsInBox(Vector3 boxCornerA, Vector3 boxCornerB, HashSet<PolygonSubEntity> selectedSubEntities)
	{
		Vector3 min = Vector3.Min(boxCornerA, boxCornerB);
		Vector3 max = Vector3.Max(boxCornerA, boxCornerB);

		Dictionary<PolygonSubEntity, HashSet<int>> result = new Dictionary<PolygonSubEntity, HashSet<int>>();

		foreach (PolygonSubEntity subEntity in selectedSubEntities)
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

			//TODO: Update restriction polygon
			updateSelectionDragPositions(dragFinalPosition - dragStartPosition, true);
			draggingSelection = false;
		}
		else if (selectingBox)
		{
			updateBoxSelection(dragStartPosition, dragFinalPosition);

			selectPoints(currentBoxSelection, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

			BoxSelect.HideBoxSelection();
			selectingBox = false;
			currentBoxSelection = new Dictionary<PolygonSubEntity, HashSet<int>>();
		}
	}

	protected int getNumberOfSelectedPointsOnContour(PolygonSubEntity subEntity, HashSet<int> selectedPoints)
	{
		int result = 0;
		int pointsOnContour = subEntity.GetPolygonPointCount();
		foreach (int selectedPoint in selectedPoints)
		{
			if (selectedPoint < pointsOnContour) { result++; }
		}
		return result;
	}

	protected virtual void OnPolygonRemoved(SubEntity removedSubEntity)
	{
	}

	protected virtual void OnPolygonModifiedViaRemoval(SubEntity modifiedSubEntity)
	{
	}

	protected virtual void deleteSelection()
	{
		fsm.AddToUndoStack(new BatchUndoOperationMarker());

        if (selectedPoints.Count == 0)
        {
            //Delete all selected subentities
            foreach (PolygonSubEntity subEntity in selectedSubEntities)
            {
                if (subEntity.Entity.PlanLayer == planLayer)
                {
                    fsm.AddToUndoStack(new RemovePolygonOperation(subEntity, planLayer, UndoOperation.EditMode.Modify));
                    OnPolygonRemoved(subEntity);
                    baseLayer.RemoveSubEntity(subEntity);
                    subEntity.RemoveGameObject();
                }
                else
                {
                    fsm.AddToUndoStack(new ModifyPolygonRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                    OnPolygonModifiedViaRemoval(subEntity);
                    baseLayer.RemoveSubEntity(subEntity);
                    subEntity.RedrawGameObject();
                }
            }
        }
        else
        {
            //Delete all selected points
            List<PolygonSubEntity> selectedPointsKeys = new List<PolygonSubEntity>(selectedPoints.Keys);
            foreach (PolygonSubEntity subEntity in selectedPointsKeys)
            {
                if (subEntity.GetPolygonPointCount() - getNumberOfSelectedPointsOnContour(subEntity, selectedPoints[subEntity]) < 3)
                {
                    // remove polygon if it has fewer than 3 points left on the contour after deletion
                    if (subEntity.Entity.PlanLayer == planLayer)
                    {
                        fsm.AddToUndoStack(new RemovePolygonOperation(subEntity, planLayer, UndoOperation.EditMode.Modify));
                        OnPolygonRemoved(subEntity);
                        baseLayer.RemoveSubEntity(subEntity);
                        subEntity.RemoveGameObject();
                    }
                    else
                    {
                        fsm.AddToUndoStack(new ModifyPolygonRemovalPlanOperation(subEntity, planLayer, planLayer.RemovedGeometry.Contains(subEntity.GetPersistentID())));
                        OnPolygonModifiedViaRemoval(subEntity);
                        baseLayer.RemoveSubEntity(subEntity);
                        subEntity.RedrawGameObject();
                    }
                    RemoveSelectedSubEntity(subEntity);
                }
                else
                {
                    PolygonSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);

                    subEntityToModify.RemovePoints(selectedPoints[subEntityToModify]);

                    int holeCount = subEntityToModify.GetHoleCount();
                    for (int i = holeCount - 1; i >= 0; --i)
                    {
                        if (subEntityToModify.GetHolePointCount(i) < 3)
                        {
                            // remove hole if it has fewer than 3 points left
                            subEntityToModify.RemoveHole(i);
                        }
                    }

                    subEntityToModify.restrictionNeedsUpdate = true;
                    subEntityToModify.PerformValidityCheck(false);
                    subEntityToModify.RedrawGameObject();
                }
            }
        }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
		fsm.SetCurrentState(new SelectPolygonsState(fsm, planLayer));
	}

	private void undoDeleteForSelection()
	{
		if (selectedRemovedEntity)
		{
			fsm.AddToUndoStack(new BatchUndoOperationMarker());
			foreach (PolygonSubEntity subEntity in selectedSubEntities)
			{
				fsm.AddToUndoStack(new ModifyPolygonRemovalPlanOperation(subEntity, planLayer, true));
				planLayer.RemovedGeometry.Remove(subEntity.GetPersistentID());
				subEntity.RestoreDependencies();
				subEntity.RedrawGameObject(SubEntityDrawMode.Selected, selectedPoints.ContainsKey(subEntity) ? selectedPoints[subEntity] : null);

			}
			fsm.AddToUndoStack(new BatchUndoOperationMarker());

			fsm.SetCurrentState(new SelectPolygonsState(fsm, planLayer));
		}
	}

	//private void simplifySelection()
	//{
	//	UIManager.CreateSingleValueWindow("Simplify Polygon", "tolerance", "0.5", 200, (value) =>
	//	{
	//		fsm.AddToUndoStack(new BatchUndoOperationMarker());

	//		List<PolygonSubEntity> toRemove = new List<PolygonSubEntity>();

	//		foreach (PolygonSubEntity subEntity in selectedSubEntities)
	//		{
	//			fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));

 //               subEntity.edited = true;
	//			selectPoints(new Dictionary<PolygonSubEntity, HashSet<int>>(), false); // clear selection

	//			subEntity.Simplify(Util.ParseToFloat(value, 0.5f));

	//			if (subEntity.GetPolygonPointCount() < 3)
	//			{
	//				toRemove.Add(subEntity);
	//			}
	//			else
	//			{
	//				for (int i = subEntity.GetHoleCount() - 1; i >= 0; --i)
	//				{
	//					if (subEntity.GetHolePointCount(i) < 3) { subEntity.RemoveHole(i); }
	//				}

	//				subEntity.PerformValidityCheck(false);
	//				subEntity.RedrawGameObject(SubEntityDrawMode.Selected);
	//			}
	//		}

	//		foreach (PolygonSubEntity subEntity in toRemove)
	//		{
	//			fsm.AddToUndoStack(new RemovePolygonOperation(subEntity, planLayer, UndoOperation.EditMode.Modify));
	//			baseLayer.RemoveSubEntity(subEntity);
	//			RemoveSelectedSubEntity(subEntity);
	//			subEntity.RemoveGameObject();
	//		}

	//		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	//	});
	//}

	//private void tryFixingInvalidPolygonsInSelection()
	//{
	//	UIManager.CreateSingleValueWindow("Try To Fix Polygon", "fix offset", "0.01", 200, (value) =>
	//	{
	//		tryFixingInvalidPolygonsInSelection(Util.ParseToFloat(value, 0.01f));
	//	});
	//}

	//private void tryFixingInvalidPolygonsInSelection(float fixOffset)
	//{
	//	selectPoints(new Dictionary<PolygonSubEntity, HashSet<int>>(), false); // clear selection

	//	fsm.AddToUndoStack(new BatchUndoOperationMarker());

	//	List<PolygonSubEntity> newSubEntities = new List<PolygonSubEntity>();
	//	foreach (PolygonSubEntity subEntity in selectedSubEntities)
	//	{
	//		fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));
	//		Util.RemoveSpikes(subEntity.GetPoints(), subEntity.GetHoles());
	//		Util.PlacePointsAtSelfIntersections(subEntity.GetPoints(), subEntity.GetHoles());

	//		List<List<Vector3>> separatedPolygons = new List<List<Vector3>> { subEntity.GetPoints() };
	//		List<List<List<Vector3>>> separatedHoles = new List<List<List<Vector3>>> { subEntity.GetHoles() };

	//		Util.SeparatePolygons(ref separatedPolygons, ref separatedHoles);

	//		if (separatedPolygons.Count > 0)
	//		{
	//			subEntity.SetDataToCopiedValues(separatedPolygons[0], separatedHoles[0], subEntity.Entity.EntityTypes);
	//			subEntity.PerformValidityCheck(false);
	//			subEntity.TryFixingSelfIntersections(fixOffset);
	//			subEntity.RedrawGameObject(SubEntityDrawMode.Selected);

	//			for (int i = 1; i < separatedPolygons.Count; ++i)
	//			{
	//				PolygonSubEntity newSubEntity = baseLayer.editingType == AbstractLayer.EditingType.SourcePolygon ? new EnergyPolygonSubEntity(subEntity.Entity) : new PolygonSubEntity(subEntity.Entity);
	//				newSubEntity.SetDataToCopiedValues(separatedPolygons[i], separatedHoles[i], subEntity.Entity.EntityTypes);
	//				(newSubEntity.Entity as PolygonEntity).AddSubEntity(newSubEntity);
	//				fsm.AddToUndoStack(new CreatePolygonOperation(newSubEntity, planLayer, UndoOperation.EditMode.Modify));
	//				newSubEntity.PerformValidityCheck(false);
	//				newSubEntity.TryFixingSelfIntersections(fixOffset);
	//				newSubEntity.DrawGameObject(baseLayer.LayerGameObject.transform, SubEntityDrawMode.Selected);

	//				newSubEntities.Add(newSubEntity);
	//			}
	//		}
	//	}

	//	foreach (PolygonSubEntity newSubEntity in newSubEntities)
	//	{
	//		AddSelectedSubEntity(newSubEntity);
	//	}

	//	fsm.AddToUndoStack(new BatchUndoOperationMarker());
	//}

	private void removeHolesFromSelection()
	{
		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (PolygonSubEntity subEntity in selectedSubEntities)
		{
			fsm.AddToUndoStack(new ModifyPolygonOperation(subEntity, planLayer, subEntity.GetDataCopy(), UndoOperation.EditMode.Modify));

            subEntity.edited = true;
			subEntity.RemoveAllHoles();
			subEntity.PerformValidityCheck(false);
			subEntity.RedrawGameObject(SubEntityDrawMode.Selected);
		}

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

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
			fsm.SetCurrentState(new StartCreatingPolygonState(fsm, planLayer));
		}
	}

	public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
	{
		switch (toolbarInput)
		{
		case FSM.ToolbarInput.Create:
			fsm.SetCurrentState(new StartCreatingPolygonState(fsm, planLayer));
			break;
		case FSM.ToolbarInput.Delete:
			deleteSelection();
			break;
		case FSM.ToolbarInput.Abort:
			fsm.SetCurrentState(new SelectPolygonsState(fsm, planLayer));
			break;
		//case FSM.ToolbarInput.Simplify:
			//simplifySelection();
			//PolygonOffset();
			//break;
		case FSM.ToolbarInput.FixInvalid:
			//tryFixingInvalidPolygonsInSelection();
			break;
		case FSM.ToolbarInput.RemoveHoles:
			removeHolesFromSelection();
			break;
		case FSM.ToolbarInput.FindGaps:
			baseLayer.CreateInvertedLayer();
			break;
		case FSM.ToolbarInput.SelectAll:
			if (baseLayer.IsEnergyPolyLayer())
				fsm.SetCurrentState(new EditEnergyPolygonState(fsm, planLayer, new HashSet<PolygonSubEntity>((baseLayer as PolygonLayer).GetAllSubEntities())));
			else
				fsm.SetCurrentState(new EditPolygonsState(fsm, planLayer, new HashSet<PolygonSubEntity>((baseLayer as PolygonLayer).GetAllSubEntities())));
			break;
		case FSM.ToolbarInput.Recall:
			undoDeleteForSelection();
			break;
		}
	}

	//public void PolygonOffset()
	//{
	//    foreach (SubEntity subEnt in selectedSubEntities)
	//    {
	//        ClipperOffset co = new ClipperOffset();
	//        PolygonSubEntity poly = subEnt as PolygonSubEntity;
	//        co.AddPath(SetOperations.VectorToIntPoint(poly.GetPolygon()), JoinType.jtMiter, EndType.etClosedPolygon);
	//        List<List<IntPoint>> csolution = new List<List<IntPoint>>();
	//        co.Execute(ref csolution, 5000000000000000);

	//        foreach (List<IntPoint> newPoly in csolution)
	//        {
	//            PolygonEntity polyent = layer.CreateNewPolygonEntity(layer.GetEntityTypeByKey(0));
	//            PolygonSubEntity polysubent = new PolygonSubEntity(polyent);
	//            polyent.AddSubEntity(polysubent);
	//            polysubent.SetPolygon(SetOperations.IntPointToVector(newPoly));
	//            polyent.DrawGameObjects(layer.LayerGameObject.transform);
	//            break;
	//        }
	//        break;
	//    }
	//}

	public override void HandleEntityTypeChange(List<EntityType> newTypes)
	{
		if (newTypes.Count <= 0) { return; }

		List<PolygonSubEntity> subEntitiesWithDifferentTypes = new List<PolygonSubEntity>();

		//Find subentities with changed entity types
		foreach (PolygonSubEntity subEntity in selectedSubEntities)
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

		foreach (PolygonSubEntity subEntity in subEntitiesWithDifferentTypes)
		{
			PolygonSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.EntityTypes = newTypes;
            subEntityToModify.RedrawGameObject(SubEntityDrawMode.Selected, selectedPoints.ContainsKey(subEntityToModify) ? selectedPoints[subEntityToModify] : null);
        }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void HandleTeamChange(int newTeam)
	{
		List<PolygonSubEntity> subEntitiesWithDifferentTeam = new List<PolygonSubEntity>();

		//Find subentities with changed entity types
		foreach (PolygonSubEntity subEntity in selectedSubEntities)
		{
			if (subEntity.Entity.Country != newTeam)
			{
				subEntitiesWithDifferentTeam.Add(subEntity);
			}
		}

		if (subEntitiesWithDifferentTeam.Count == 0) { return; }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (PolygonSubEntity subEntity in subEntitiesWithDifferentTeam)
		{
			PolygonSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.Country = newTeam;
        }

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void HandleParameterChange(EntityPropertyMetaData parameter, string newValue)
	{
		List<PolygonSubEntity> subEntitiesWithDifferentParams = new List<PolygonSubEntity>();

		//Find subentities with changed entity types
		foreach (PolygonSubEntity subEntity in selectedSubEntities)
		{
			if (subEntity.Entity.GetPropertyMetaData(parameter) != newValue)
			{
				subEntitiesWithDifferentParams.Add(subEntity);
			}
		}

		if (subEntitiesWithDifferentParams.Count == 0) { return; }

		fsm.AddToUndoStack(new BatchUndoOperationMarker());

		foreach (PolygonSubEntity subEntity in subEntitiesWithDifferentParams)
		{
			PolygonSubEntity subEntityToModify = startModifyingSubEntity(subEntity, true);
			subEntityToModify.Entity.SetPropertyMetaData(parameter, newValue);
		}

		fsm.AddToUndoStack(new BatchUndoOperationMarker());
	}

	public override void ExitState(Vector3 currentMousePosition)
	{
		//if (previousHover != null)
		//{
		//    previousHover.PolygonSubEntity.RedrawGameObject();
		//}

		foreach (PolygonSubEntity pse in selectedSubEntities)
		{
			pse.RedrawGameObject();
		}
		selectedSubEntities = new HashSet<PolygonSubEntity>();
		selectedSourcePoints = new HashSet<EnergyPointSubEntity>();

		BoxSelect.HideBoxSelection();
		IssueManager.instance.SetIssueInteractability(true);

		// make sure the entity type dropdown shows a valid value
		//UIManager.SetCurrentEntityTypeSelection(UIManager.GetCurrentEntityTypeSelection());
	}

	public void AddSelectedSubEntity(PolygonSubEntity subEntity, bool updateActivePlanWindow = true)
	{
		selectedSubEntities.Add(subEntity);
		if (this is EditEnergyPolygonState)
			selectedSourcePoints.Add((subEntity as EnergyPolygonSubEntity).sourcePoint);
        if(updateActivePlanWindow)
		    UpdateActivePlanWindowToSelection();
	}

	public void SetSelectedSubEntities(HashSet<PolygonSubEntity> subEntities)
	{
		selectedSubEntities = subEntities;
		foreach (PolygonSubEntity poly in subEntities) //Check if this is a polygon marked for removal, this limits editing
		{
			selectedRemovedEntity = poly.IsPlannedForRemoval();
			UIManager.ToolbarEnable(selectedRemovedEntity, FSM.ToolbarInput.Recall);
			UIManager.SetActivePlanWindowChangeable(!selectedRemovedEntity);
			break;
		}
		if (this is EditEnergyPolygonState)
		{
			selectedSourcePoints = new HashSet<EnergyPointSubEntity>();
			foreach (PolygonSubEntity poly in selectedSubEntities)
				selectedSourcePoints.Add((poly as EnergyPolygonSubEntity).sourcePoint);
		}
	}

	public void RemoveSelectedSubEntity(PolygonSubEntity subEntity)
	{
		selectedSubEntities.Remove(subEntity);
		if (this is EditEnergyPolygonState)
			selectedSourcePoints.Remove((subEntity as EnergyPolygonSubEntity).sourcePoint);
	}

	private void UpdateActivePlanWindowToSelection()
	{
		List<List<EntityType>> selectedEntityTypes = new List<List<EntityType>>();
		int? selectedTeam = null;
		List<Dictionary<EntityPropertyMetaData, string>> selectedParams = new List<Dictionary<EntityPropertyMetaData, string>>();

		foreach (PolygonSubEntity pse in selectedSubEntities)
		{
			selectedEntityTypes.Add(pse.Entity.EntityTypes);
			if (selectedTeam.HasValue && pse.Entity.Country != selectedTeam.Value)
				selectedTeam = -1;
			else
				selectedTeam = pse.Entity.Country;

			Dictionary<EntityPropertyMetaData, string> parameters = new Dictionary<EntityPropertyMetaData, string>();
			foreach (EntityPropertyMetaData p in baseLayer.propertyMetaData)
			{
				if (p.ShowInEditMode)
					parameters.Add(p, pse.Entity.GetPropertyMetaData(p));
			}
			selectedParams.Add(parameters);
		}

		UIManager.SetActiveplanWindowToSelection(
			selectedEntityTypes.Count > 0 ? selectedEntityTypes : null,
			selectedTeam ?? -2,
			selectedParams);
	}
}
