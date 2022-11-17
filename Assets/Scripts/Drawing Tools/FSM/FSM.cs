using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace MSP2050.Scripts
{
	public class FSM
	{
		private static FSM instance;

		private const float DOUBLE_CLICK_MAX_INTERVAL = 0.5f;

		public enum ToolbarInput { Create, Edit, Undo, Redo, Delete, Recall, ChangeDirection, Abort }//Union, Intersect, Difference, Simplify,
			//RemoveHoles, FindGaps, SnapPoints, FixInvalid, SelectAll}

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
		public static FSMState CurrentState => instance.currentState;
		public FSMState InputReceivingState => interruptState ?? currentState;

		public event Action onGeometryCompleted;

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
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_typeChangeCallback = EntityTypeChanged;
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_countryChangeCallback = TeamChanged;
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_parameterChangeCallback = ParameterChanged;
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

			if (currentState.StateType == FSMState.EEditingStateType.Create)
			{
				if (layer is PolygonLayer)
				{
					SetCurrentState(new StartCreatingPolygonState(this, planLayer));
				}
				else if (layer is LineStringLayer)
				{
					if(layer.IsEnergyLayer())
						SetCurrentState(new StartCreatingEnergyLineStringState(this, planLayer));
					else
						SetCurrentState(new StartCreatingLineStringState(this, planLayer));
				}
				else if (layer is PointLayer)
				{
					if (layer.IsEnergyLayer())
						SetCurrentState(new CreateEnergyPointState(this, planLayer));
					else
						SetCurrentState(new CreatePointsState(this, planLayer));
				}
			}
			else if (layer is PolygonLayer)
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
		
		public static void ToolbarButtonClicked(ToolbarInput toolbarInput)
		{
			instance.toolbarButtonClicked(toolbarInput);
		}

		private void toolbarButtonClicked(ToolbarInput toolbarInput)
		{
			SetInterruptState(null);

			switch (toolbarInput)
			{ 
				case ToolbarInput.Undo:
					undo();
					updateUndoRedoButtonEnabled();               
					break;
				case ToolbarInput.Redo:
					redo();
					updateUndoRedoButtonEnabled();                
					break;
				//case ToolbarInput.SnapPoints:
				//	SetSnappingEnabled(!snappingEnabled);
				//	break;
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

		public void ClearUndoRedo()
		{
			undoStack.Clear();
			redoStack.Clear();
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
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonInteractable(ToolbarInput.Redo, redoStack.Count > 0);
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonInteractable(ToolbarInput.Undo, undoStack.Count > 0);
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
			List<AbstractLayer> visibleLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth();

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

			float selectMaxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();
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
					if (Time.time - previousClickTime < DOUBLE_CLICK_MAX_INTERVAL && (mousePosition - previousClickLocation).magnitude < VisualizationUtil.Instance.GetMouseMoveThreshold())
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
				float threshold = VisualizationUtil.Instance.GetMouseMoveThreshold();
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

		public void TriggerGeometryComplete()
		{
			onGeometryCompleted?.Invoke();
		}
	}
}