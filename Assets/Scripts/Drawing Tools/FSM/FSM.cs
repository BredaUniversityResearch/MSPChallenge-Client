using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Text;
using Assets.Networking;
using Newtonsoft.Json.Linq;

public class FSM
{
    private static FSM instance;

    private const float DOUBLE_CLICK_MAX_INTERVAL = 0.5f;

    public enum ToolbarInput { Create, Edit, Undo, Redo, Delete, Abort, Accept, Cancel, Union, Intersect, Difference, /*Simplify,*/
                               RemoveHoles, FindGaps, SnapPoints, FixInvalid, SelectAll, Recall, ChangeDirection}

    public enum CursorType { Add, Complete, Default, Insert, Move, Invalid, Rescale, ZoomToArea, LayerProbe, Ruler }
    private CursorType currentCursor = CursorType.Default;

    private Texture2D cursorAdd;
    private Texture2D cursorComplete;
    private Texture2D cursorDefault;
    private Texture2D cursorInsert;
    private Texture2D cursorMove;
    private Texture2D cursorInvalid;
    private Texture2D cursorRescale;
    private Texture2D cursorZoomToArea;
    private Texture2D cursorLayerProbe;
    private Texture2D cursorRuler;

    private FSMState currentState;
    private FSMState interruptState = null;
    public FSMState InputReceivingState
    {
        get { return interruptState ?? currentState; }
    }

    private Vector3 leftMouseButtonDownStartPosition;
    private Vector3 previousMousePosition;

    private bool userMayBeClicking = false;
    private bool userMayBeDragging = false;
    private bool dragging = false;

    private Stack<UndoOperation> undoStack = new Stack<UndoOperation>();
    private Stack<UndoOperation> redoStack = new Stack<UndoOperation>();

    float previousClickTime = float.MinValue;
    Vector3 previousClickLocation = Vector3.zero;

    private bool snappingEnabled;

    public FSM()
    {
        instance = this;

        previousMousePosition = GetWorldMousePosition();

        currentState = new DefaultState(this);

        cursorAdd = Resources.Load<Texture2D>("ui_cursor_add");
        cursorComplete = Resources.Load<Texture2D>("ui_cursor_complete");
        cursorDefault = Resources.Load<Texture2D>("ui_cursor_default");
        cursorInsert = Resources.Load<Texture2D>("ui_cursor_insert");
        cursorMove = Resources.Load<Texture2D>("ui_cursor_move");
		cursorInvalid = Resources.Load<Texture2D>("ui_cursor_invalid");
        cursorRescale = Resources.Load<Texture2D>("ui_cursor_rescale");
        cursorZoomToArea = Resources.Load<Texture2D>("ui_cursor_area");
        cursorLayerProbe = Resources.Load<Texture2D>("ui_cursor_probe");
        cursorRuler = Resources.Load<Texture2D>("ui_cursor_ruler");

        SetCursor(CursorType.Default, true);

        //Add change callbacks
        InterfaceCanvas.Instance.activePlanWindow.typeChangeCallback = EntityTypeChanged;
        InterfaceCanvas.Instance.activePlanWindow.countryChangeCallback = TeamChanged;
        InterfaceCanvas.Instance.activePlanWindow.parameterChangeCallback = ParameterChanged;
    }

    public void SetCursor(CursorType cursorType, bool forceRedraw = false)
    {
        if (!forceRedraw && currentCursor == cursorType) { return; }
        currentCursor = cursorType;

        Texture2D cursorTexture = cursorDefault;
        switch (cursorType)
        {
            case CursorType.Add:
                cursorTexture = cursorAdd;
                break;
            case CursorType.Complete:
                cursorTexture = cursorComplete;
                break;
            case CursorType.Default:
                cursorTexture = cursorDefault;
                break;
            case CursorType.Insert:
                cursorTexture = cursorInsert;
                break;
            case CursorType.Move:
                cursorTexture = cursorMove;
                break;
			case CursorType.Invalid:
				cursorTexture = cursorInvalid;
				break;
            case CursorType.Rescale:
                cursorTexture = cursorRescale;
                break;
            case CursorType.ZoomToArea:
                cursorTexture = cursorZoomToArea;
                break;
            case CursorType.LayerProbe:
                cursorTexture = cursorLayerProbe;
                break;
            case CursorType.Ruler:
                cursorTexture = cursorRuler;
                break;
        }

        Cursor.SetCursor(cursorTexture, new Vector2(7, 7), CursorMode.Auto);
    }

    public CursorType CurrentCursorType
    {
        get { return currentCursor; }
    }

    public void StartEditingLayer(PlanLayer planLayer)
    {
        AbstractLayer layer = planLayer.BaseLayer;

        if (layer is PolygonLayer)
        {
            SetCurrentState(new SelectPolygonsState(this, planLayer));
        }
        else if (layer is LineStringLayer)
        {
            SetCurrentState(new SelectLineStringsState(this, planLayer));
        }
        else if (layer is PointLayer)
        {
            if (layer.IsEnergyPointLayer())
                SetCurrentState(new EditEnergyPointsState(this, planLayer));
            else
                SetCurrentState(new EditPointsState(this, planLayer));
        }        

        updateUndoRedoButtonEnabled();
    }

    public void StopEditing()
    {
        SetCurrentState(new DefaultState(this));
    }

    public void StartSetOperations()
    {
        SetCurrentState(new SetOperationsState(this));

        updateUndoRedoButtonEnabled();
    }

    public static void ToolbarButtonClicked(ToolbarInput toolbarInput)
    {
        instance.toolbarButtonClicked(toolbarInput);
    }

    private void toolbarButtonClicked(ToolbarInput toolbarInput)
    {
        SetInterruptState(null);

        switch (toolbarInput)
        { 
            case ToolbarInput.Accept:
				PlanDetails.instance.changesConfirmButton.onClick.Invoke();
				break;
            case ToolbarInput.Cancel:
				PlanDetails.instance.changesCancelButton.onClick.Invoke();             
                break;
            case ToolbarInput.Undo:
                undo();
                updateUndoRedoButtonEnabled();               
                break;
            case ToolbarInput.Redo:
                redo();
                updateUndoRedoButtonEnabled();                
                break;
            case ToolbarInput.SnapPoints:
                SetSnappingEnabled(!snappingEnabled);
                break;
            default:
                InputReceivingState.HandleToolbarInput(toolbarInput);
                break;
        }
    }

    public static void EntityTypeChanged(List<EntityType> newTypes)
    {
        instance.SetInterruptState(null);
        instance.currentState.HandleEntityTypeChange(newTypes);
    }

    public static void TeamChanged(int newTeam)
    {
        instance.SetInterruptState(null);
        instance.currentState.HandleTeamChange(newTeam);
    }

	public static void ParameterChanged(EntityPropertyMetaData parameter, string newValue)
	{
        instance.SetInterruptState(null);
        instance.currentState.HandleParameterChange(parameter, newValue);
	}

	public void SetSnappingEnabled(bool value)
    {
        snappingEnabled = value;
    }

    public void AddToUndoStack(UndoOperation undoOperation)
    {
        undoStack.Push(undoOperation);
        if (redoStack.Count > 0)
        {
            redoStack.Clear();
        }

        updateUndoRedoButtonEnabled();
    }

	private void GetChangedObjects(out HashSet<SubEntity> newSubEntities, out HashSet<SubEntity> modifiedSubEntities, out HashSet<SubEntity> removedSubEntities, out Dictionary<SubEntity, PlanLayer> planRemovalModifiedEntities)
	{
		newSubEntities = new HashSet<SubEntity>();
		modifiedSubEntities = new HashSet<SubEntity>();
		removedSubEntities = new HashSet<SubEntity>();
		planRemovalModifiedEntities = new Dictionary<SubEntity, PlanLayer>();

		foreach (UndoOperation operation in undoStack)
		{
			SubEntity subEntity = null;
			if (operation is ISubEntityHolder)
			{
				subEntity = (operation as ISubEntityHolder).GetSubEntity();
			}

			if (operation is RemovePolygonOperation || operation is RemoveLineStringOperation || operation is RemovePointOperation)
			{
				removedSubEntities.Add(subEntity);
				if (subEntity.GetPersistentID() != -1)
					planRemovalModifiedEntities[subEntity] = (operation as IPlanLayerHolder).GetPlanLayer();
			}
			else if (operation is ModifyPolygonOperation || operation is ModifyLineStringOperation || operation is ModifyPointOperation)
			{
				if (!removedSubEntities.Contains(subEntity) && !newSubEntities.Contains(subEntity))
				{
					modifiedSubEntities.Add(subEntity);
				}
			}
			else if (operation is CreatePolygonOperation ||
				operation is CreateLineStringOperation ||
				operation is CreatePointOperation)
			{
				if (removedSubEntities.Contains(subEntity))
				{
					removedSubEntities.Remove(subEntity);
				}
				else
				{
					newSubEntities.Add(subEntity);

					if (modifiedSubEntities.Contains(subEntity))
					{
						modifiedSubEntities.Remove(subEntity);
					}
				}
			}
			else if (operation is ModifyPolygonRemovalPlanOperation ||
					 operation is ModifyLineStringRemovalPlanOperation ||
					 operation is ModifyPointRemovalPlanOperation)
			{
				planRemovalModifiedEntities[subEntity] = (operation as IPlanLayerHolder).GetPlanLayer();
			}
		}
	}

    public void SubmitAllChanges(BatchRequest batch)
    {
		GetChangedObjects(out var newSubEntities, out var modifiedSubEntities, out var removedSubEntities, out var planRemovalModifiedEntities);
		
        List<EnergyLineStringSubEntity> addedCables = new List<EnergyLineStringSubEntity>();

		//This includes both completely new geometry, as well as existing geometry that is newly modified in this plan
        foreach (SubEntity newSubEntity in newSubEntities)
        {
            newSubEntity.SubmitNew(batch);
            if (newSubEntity is EnergyLineStringSubEntity)//Check for created connections
                addedCables.Add(newSubEntity as EnergyLineStringSubEntity);
        }

		//These are only modified subentities that already existed on the planlayer, so just update the content
		foreach (SubEntity modifiedSubEntity in modifiedSubEntities)
        {
            if (modifiedSubEntity is EnergyLineStringSubEntity)//Check for updated connections
                addedCables.Add(modifiedSubEntity as EnergyLineStringSubEntity);

			modifiedSubEntity.SubmitUpdate(batch);
        }

		//These are subentities that used to be added/modified in the plan, but are no longer
		foreach (SubEntity removedSubEntity in removedSubEntities)
        {
            removedSubEntity.SubmitDelete(batch);
        }

        foreach (KeyValuePair<SubEntity, PlanLayer> kvp in planRemovalModifiedEntities)
        {
            if (kvp.Value.RemovedGeometry.Contains(kvp.Key.GetPersistentID()))
				kvp.Value.SubmitMarkForDeletion(kvp.Key, batch);
            else
				kvp.Value.SubmitUnmarkForDeletion(kvp.Key, batch);
        }

        //Submit connections operation
        if (addedCables.Count > 0)
        {
			SubmitConnections(addedCables, batch);
		}
    }

    public static void SubmitConnections(List<EnergyLineStringSubEntity> addedCables, BatchRequest batch)
    {
        for(int i = 0; i < addedCables.Count; i++)
        {
	        if (addedCables[i].connections == null || addedCables[i].connections.Count == 0)
	        {
				Debug.LogError($"Trying to submit a cable with no connections. Cable ID: {addedCables[i].GetDatabaseID()}");
			}
			else if (addedCables[i].connections.Count < 2)
	        {
		        Debug.LogError($"Trying to submit a cable with a missing connection. Cable ID: {addedCables[i].GetDatabaseID()}. Existing connection to point with ID: {addedCables[i].connections[0].point.GetDatabaseID()}");
	        }
			else
			{
	            EnergyPointSubEntity first = null, second = null;
	            foreach (Connection conn in addedCables[i].connections)
	            {
	                if (conn.connectedToFirst)
	                    first = conn.point;
	                else
	                    second = conn.point;
	            }

				Vector2 coordinate = first.GetPosition();

				JObject dataObject = new JObject();
				dataObject.Add("start", first.GetDataBaseOrBatchIDReference());
				dataObject.Add("end", second.GetDataBaseOrBatchIDReference());
				dataObject.Add("cable", addedCables[i].GetDataBaseOrBatchIDReference());
				dataObject.Add("coords", $"[{coordinate.x},{coordinate.y}]");
				batch.AddRequest(Server.CreateConnection(), dataObject, BatchRequest.BATCH_GROUP_CONNECTIONS);
			}
        }		
	}

    public void ClearUndoRedoAndFinishEditing()
    {
		GetChangedObjects(out var newSubEntities, out var modifiedSubEntities, out var removedSubEntities, out var planRemovalModifiedEntities);

		foreach (SubEntity newSubEntity in newSubEntities)
		{
			newSubEntity.FinishEditing();
		}

		foreach (SubEntity modifiedSubEntity in modifiedSubEntities)
		{
			modifiedSubEntity.FinishEditing();
		}

		foreach (SubEntity removedSubEntity in removedSubEntities)
		{
			removedSubEntity.FinishEditing();
		}

		undoStack.Clear();
        redoStack.Clear();
        updateUndoRedoButtonEnabled();
    }

    public void UndoAllAndClearStacks()
    {
        while (undoStack.Count > 0)
        {
            SingleClearingUndo();
        }

        undoStack.Clear();
        redoStack.Clear();

        updateUndoRedoButtonEnabled();
    }

    public void undo()
    {
        if (!(undoStack.Peek() is BatchUndoOperationMarker))
        {
            singleUndo();
        }
        else
        {
            singleUndo(); // remove first batch marker from the stack
            while (!(undoStack.Peek() is BatchUndoOperationMarker) && undoStack.Count > 0)
            {
                singleUndo();
            }
            singleUndo(); // remove second batch marker from the stack
        }
        if (undoStack.Count > 0 && undoStack.Peek() is ConcatOperationMarker)
        {
            singleUndo(); //Remove concat operation from stack
            undo();
        }
    }

    private void singleUndo()
    {
        UndoOperation redo;
        UndoOperation undo = undoStack.Pop();
        Debug.Log("Undid: " + undo.GetType().Name);
        undo.Undo(this, out redo);
        redoStack.Push(redo);
    }

    private void SingleClearingUndo()
    {
        UndoOperation undo = undoStack.Pop();
        UndoOperation redo;
        undo.Undo(this, out redo, true);
    }

    private void redo()
    {
        if (!(redoStack.Peek() is BatchUndoOperationMarker))
        {
            singleRedo();
        }
        else
        {
            singleRedo(); // remove first batch marker from the stack
            while (!(redoStack.Peek() is BatchUndoOperationMarker) && redoStack.Count > 0)
            {
                singleRedo();
            }
            singleRedo(); // remove second batch marker from the stack
        }
        if (redoStack.Count > 0 && redoStack.Peek() is ConcatOperationMarker)
        {
            singleRedo(); //Remove concat operation from stack
            redo();
        }
    }

    private void singleRedo()
    {
        UndoOperation undo;
		UndoOperation redo = redoStack.Pop();
		Debug.Log("Redid: " + redo.GetType().Name);
		redo.Undo(this, out undo);
        undoStack.Push(undo);
    }

    private void updateUndoRedoButtonEnabled()
    {
        UIManager.ToolbarEnable(undoStack.Count > 0, ToolbarInput.Undo);
        UIManager.ToolbarEnable(redoStack.Count > 0, ToolbarInput.Redo);
    }

    public FSMState GetCurrentState()
    {
        return currentState;
    }

    public FSMState GetInterruptState()
    {
        return interruptState;
    }

    public void AbortCurrentState()
    {
        currentState.Abort();
    }

    public void SetCurrentState(FSMState newState)
    {
        Vector3 mousePosition = GetWorldMousePosition();

        currentState.ExitState(mousePosition);

        userMayBeClicking = false;
        dragging = false;

        SetCursor(CursorType.Default);
        SetSnappingEnabled(false);

        currentState = newState;
        newState.EnterState(mousePosition);
    }

    public void SetInterruptState(FSMState newInterruptState)
    {
        Vector3 mousePosition = GetWorldMousePosition();
        if (interruptState != null)
            interruptState.ExitState(mousePosition);

        userMayBeClicking = false;
        dragging = false;

        interruptState = newInterruptState;
        if(newInterruptState != null)
            newInterruptState.EnterState(mousePosition);
    }

    public Vector3 GetWorldMousePosition()
    {
        Vector3 position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
        position.z = 0;

        if (snappingEnabled) { position = getSnappedMousePosition(position); }

        return position;
    }

    protected Vector3 getSnappedMousePosition(Vector3 mousePosition)
    {
        List<SubEntity> subEntities = new List<SubEntity>();
        List<AbstractLayer> visibleLayers = LayerManager.GetVisibleLayersSortedByDepth();

        foreach (AbstractLayer layer in visibleLayers)
        {
            List<SubEntity> layerSubEntities = layer.GetSubEntitiesAt(mousePosition);

            foreach (SubEntity layerSubEntity in layerSubEntities)
            {
                if (layerSubEntity.SnappingToThisEnabled)
                {
                    subEntities.Add(layerSubEntity);
                }
            }
        }

        if (subEntities.Count == 0) { return mousePosition; }

        Vector3 snappedPosition = Vector3.zero;
        float snappedPositionSqrDistance = float.MaxValue;

        foreach (SubEntity subEntity in subEntities)
        {
            Vector3 closestPoint = subEntity.GetPointClosestTo(mousePosition);
            float sqrDistance = (mousePosition - closestPoint).sqrMagnitude;

            if (sqrDistance < snappedPositionSqrDistance)
            {
                snappedPosition = closestPoint;
                snappedPositionSqrDistance = sqrDistance;
            }
        }

        float selectMaxDistance = VisualizationUtil.GetSelectMaxDistance();
        if (snappedPositionSqrDistance > selectMaxDistance * selectMaxDistance)
        {
            return mousePosition;
        }
        else
        {
            return snappedPosition;
        }
    }

    public void Update()
    {
        bool cursorIsOverUI = EventSystem.current.IsPointerOverGameObject();
        if (cursorIsOverUI) { userMayBeClicking = false; userMayBeDragging = false; }

        if (Input.GetKey(KeyCode.F10))
        {
			int size = 1;
			bool takeShot = false;
			if(Input.GetKeyDown(KeyCode.Keypad1))
			{
				size = 1;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad2))
			{
				size = 2;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad3))
			{
				size = 3;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad4))
			{
				size = 4;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad5))
			{
				size = 5;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad6))
			{
				size = 6;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad7))
			{
				size = 7;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad8))
			{
				size = 8;
				takeShot = true;
			}
			if (Input.GetKeyDown(KeyCode.Keypad9))
			{
				size = 9;
				takeShot = true;
			}
			if (takeShot)
			{
				Debug.Log("Making screenshot, supersize: " + size.ToString());
				ScreenCapture.CaptureScreenshot("Screenshot.png", size);
				Debug.Log("ScreenShot Taken, stored in: " +  Application.persistentDataPath);
			}
        }

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            InputReceivingState.HandleKeyboardEvents();

            //Only allow undo/redo while not in interrupt state

            if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && undoStack.Count > 0)
            {
                SetInterruptState(null);
                undo();
                updateUndoRedoButtonEnabled();
            }
            else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && redoStack.Count > 0)
            {
                SetInterruptState(null);
                redo();
                updateUndoRedoButtonEnabled();
            }

        }

        // skip mouse input handling if the cursor is outside of the viewport (unless the user is dragging)
        if (!dragging && (
            Input.mousePosition.x < 0 || Input.mousePosition.y < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height
            ))
        {
            return;
        }
		//Prev Return
        Vector3 mousePosition = GetWorldMousePosition();

        if (Input.GetMouseButtonDown(0) && !cursorIsOverUI && !Main.ControlKeyDown)
        {
            InputReceivingState.LeftMouseButtonDown(mousePosition);

            leftMouseButtonDownStartPosition = mousePosition;
            userMayBeClicking = true;
            userMayBeDragging = true;
        }


        if (Input.GetMouseButtonUp(0))
        {
            if (!cursorIsOverUI && (userMayBeClicking || dragging || userMayBeDragging)) { InputReceivingState.LeftMouseButtonUp(leftMouseButtonDownStartPosition, mousePosition); }

            if (userMayBeClicking)
            {
                if (Time.time - previousClickTime < DOUBLE_CLICK_MAX_INTERVAL && (mousePosition - previousClickLocation).magnitude < VisualizationUtil.GetMouseMoveThreshold())
                {
                    InputReceivingState.DoubleClick(mousePosition);
                }

                InputReceivingState.LeftClick(mousePosition);

                previousClickTime = Time.time;
                previousClickLocation = mousePosition;
            }

            userMayBeClicking = false;

            if (dragging)
            {
                InputReceivingState.StoppedDragging(leftMouseButtonDownStartPosition, mousePosition);
            }
            dragging = false;
            userMayBeDragging = false;
        }

		if (userMayBeClicking)
        {
            float threshold = VisualizationUtil.GetMouseMoveThreshold();
            threshold *= threshold;

            if ((mousePosition - leftMouseButtonDownStartPosition).sqrMagnitude > threshold)
            {
                userMayBeClicking = false;
            }
        }

        if (mousePosition != previousMousePosition && !userMayBeClicking)
        {
            InputReceivingState.MouseMoved(previousMousePosition, mousePosition, cursorIsOverUI);
            if (!dragging && userMayBeDragging && Input.GetMouseButton(0))
            {
                InputReceivingState.StartedDragging(leftMouseButtonDownStartPosition, mousePosition);
                dragging = true;
            }
            else if (dragging)
            {
                InputReceivingState.Dragging(leftMouseButtonDownStartPosition, mousePosition);
            }
        }

        previousMousePosition = mousePosition;
	}

    public static void CameraZoomChanged()
    {
        if (instance == null)
            return;
        instance.InputReceivingState.HandleCameraZoomChanged();
    }
}