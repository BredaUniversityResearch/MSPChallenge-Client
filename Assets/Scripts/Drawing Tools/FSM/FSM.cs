using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace MSP2050.Scripts
{
	public class FSM
	{
		private static FSM Instance;

		private const float DOUBLE_CLICK_MAX_INTERVAL = 0.5f;

		public enum ToolbarInput { Create, Edit, Undo, Redo, Delete, Recall, ChangeDirection, Abort }//Union, Intersect, Difference, Simplify,
			//RemoveHoles, FindGaps, SnapPoints, FixInvalid, SelectAll}

		public enum CursorType { Add, Complete, Default, Insert, Move, Invalid, Rescale, ZoomToArea, LayerProbe, Ruler }
		private CursorType m_currentCursor = CursorType.Default;

		private Texture2D m_cursorAdd;
		private Texture2D m_cursorComplete;
		private Texture2D m_cursorDefault;
		private Texture2D m_cursorInsert;
		private Texture2D m_cursorMove;
		private Texture2D m_cursorInvalid;
		private Texture2D m_cursorRescale;
		private Texture2D m_cursorZoomToArea;
		private Texture2D m_cursorLayerProbe;
		private Texture2D m_cursorRuler;

		private FSMState m_currentState;
		private FSMState m_interruptState = null;
		public static FSMState CurrentState => Instance.m_currentState;
		private FSMState InputReceivingState => m_interruptState ?? m_currentState;

		public event Action OnGeometryCompleted;

		private Vector3 m_leftMouseButtonDownStartPosition;
		private Vector3 m_previousMousePosition;

		private bool m_userMayBeClicking = false;
		private bool m_userMayBeDragging = false;
		private bool m_dragging = false;

		private Stack<UndoOperation> m_undoStack = new Stack<UndoOperation>();
		private Stack<UndoOperation> m_redoStack = new Stack<UndoOperation>();

		private float m_previousClickTime = float.MinValue;
		private Vector3 m_previousClickLocation = Vector3.zero;

		private bool m_snappingEnabled;
		
		public CursorType CurrentCursorType => m_currentCursor;

		public FSM()
		{
			Instance = this;

			m_previousMousePosition = GetWorldMousePosition();

			m_currentState = new DefaultState(this);

			m_cursorAdd = Resources.Load<Texture2D>("ui_cursor_add");
			m_cursorComplete = Resources.Load<Texture2D>("ui_cursor_complete");
			m_cursorDefault = Resources.Load<Texture2D>("ui_cursor_default");
			m_cursorInsert = Resources.Load<Texture2D>("ui_cursor_insert");
			m_cursorMove = Resources.Load<Texture2D>("ui_cursor_move");
			m_cursorInvalid = Resources.Load<Texture2D>("ui_cursor_invalid");
			m_cursorRescale = Resources.Load<Texture2D>("ui_cursor_rescale");
			m_cursorZoomToArea = Resources.Load<Texture2D>("ui_cursor_area");
			m_cursorLayerProbe = Resources.Load<Texture2D>("ui_cursor_probe");
			m_cursorRuler = Resources.Load<Texture2D>("ui_cursor_ruler");

			SetCursor(CursorType.Default, true);

			//Add change callbacks
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_typeChangeCallback = EntityTypeChanged;
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_countryChangeCallback = TeamChanged;
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_parameterChangeCallback = ParameterChanged;
		}

		public void SetCursor(CursorType a_cursorType, bool a_forceRedraw = false)
		{
			if (!a_forceRedraw && m_currentCursor == a_cursorType) { return; }
			m_currentCursor = a_cursorType;

			Texture2D cursorTexture = m_cursorDefault;
			switch (a_cursorType)
			{
				case CursorType.Add:
					cursorTexture = m_cursorAdd;
					break;
				case CursorType.Complete:
					cursorTexture = m_cursorComplete;
					break;
				case CursorType.Default:
					cursorTexture = m_cursorDefault;
					break;
				case CursorType.Insert:
					cursorTexture = m_cursorInsert;
					break;
				case CursorType.Move:
					cursorTexture = m_cursorMove;
					break;
				case CursorType.Invalid:
					cursorTexture = m_cursorInvalid;
					break;
				case CursorType.Rescale:
					cursorTexture = m_cursorRescale;
					break;
				case CursorType.ZoomToArea:
					cursorTexture = m_cursorZoomToArea;
					break;
				case CursorType.LayerProbe:
					cursorTexture = m_cursorLayerProbe;
					break;
				case CursorType.Ruler:
					cursorTexture = m_cursorRuler;
					break;
			}

			Cursor.SetCursor(cursorTexture, new Vector2(7, 7), CursorMode.Auto);
		}

		public void StartEditingLayer(PlanLayer a_planLayer)
		{
			AbstractLayer layer = a_planLayer.BaseLayer;

			if (m_currentState.StateType == FSMState.EEditingStateType.Create)
			{
				if (layer is PolygonLayer)
				{
					SetCurrentState(new StartCreatingPolygonState(this, a_planLayer));
				}
				else if (layer is LineStringLayer)
				{
					if(layer.IsEnergyLayer())
						SetCurrentState(new StartCreatingEnergyLineStringState(this, a_planLayer));
					else
						SetCurrentState(new StartCreatingLineStringState(this, a_planLayer));
				}
				else if (layer is PointLayer)
				{
					if (layer.IsEnergyLayer())
						SetCurrentState(new CreateEnergyPointState(this, a_planLayer));
					else
						SetCurrentState(new CreatePointsState(this, a_planLayer));
				}
			}
			else if (layer is PolygonLayer)
			{
				SetCurrentState(new SelectPolygonsState(this, a_planLayer));
			}
			else if (layer is LineStringLayer)
			{
				SetCurrentState(new SelectLineStringsState(this, a_planLayer));
			}
			else if (layer is PointLayer)
			{
				if (layer.IsEnergyPointLayer())
					SetCurrentState(new EditEnergyPointsState(this, a_planLayer));
				else
					SetCurrentState(new EditPointsState(this, a_planLayer));
			}

			UpdateUndoRedoButtonEnabled();
		}

		public void StopEditing()
		{
			SetCurrentState(new DefaultState(this));
		}
		
		public static void ToolbarButtonClicked(ToolbarInput a_toolbarInput)
		{
			Instance.ToolbarButtonClickedLocal(a_toolbarInput);
		}

		private void ToolbarButtonClickedLocal(ToolbarInput a_toolbarInput)
		{
			SetInterruptState(null);

			switch (a_toolbarInput)
			{ 
				case ToolbarInput.Undo:
					Undo();
					UpdateUndoRedoButtonEnabled();               
					break;
				case ToolbarInput.Redo:
					Redo();
					UpdateUndoRedoButtonEnabled();                
					break;
				default:
					InputReceivingState.HandleToolbarInput(a_toolbarInput);
					break;
			}
		}

		private static void EntityTypeChanged(List<EntityType> a_newTypes)
		{
			Instance.SetInterruptState(null);
			Instance.m_currentState.HandleEntityTypeChange(a_newTypes);
		}

		private static void TeamChanged(int a_newTeam)
		{
			Instance.SetInterruptState(null);
			Instance.m_currentState.HandleTeamChange(a_newTeam);
		}

		private static void ParameterChanged(EntityPropertyMetaData a_parameter, string a_newValue)
		{
			Instance.SetInterruptState(null);
			Instance.m_currentState.HandleParameterChange(a_parameter, a_newValue);
		}

		public void SetSnappingEnabled(bool a_value)
		{
			m_snappingEnabled = a_value;
		}

		public void AddToUndoStack(UndoOperation a_undoOperation)
		{
			m_undoStack.Push(a_undoOperation);
			if (m_redoStack.Count > 0)
			{
				m_redoStack.Clear();
			}

			UpdateUndoRedoButtonEnabled();
		}

		public void ClearUndoRedo()
		{
			m_undoStack.Clear();
			m_redoStack.Clear();
		}
		
		public void Undo(bool a_addToRedo = true)
		{
			if (!(m_undoStack.Peek() is BatchUndoOperationMarker))
			{
				SingleUndo(a_addToRedo);
			}
			else
			{
				SingleUndo(a_addToRedo); // remove first batch marker from the stack
				while (!(m_undoStack.Peek() is BatchUndoOperationMarker) && m_undoStack.Count > 0)
				{
					SingleUndo(a_addToRedo);
				}
				SingleUndo(a_addToRedo); // remove second batch marker from the stack
			}
			if (m_undoStack.Count > 0 && m_undoStack.Peek() is ConcatOperationMarker)
			{
				SingleUndo(a_addToRedo); //Remove concat operation from stack
				Undo(a_addToRedo);
			}
		}

		private void SingleUndo(bool a_addToRedo = true)
		{
			UndoOperation redo;
			UndoOperation undo = m_undoStack.Pop();
			Debug.Log("Undid: " + undo.GetType().Name);
			undo.Undo(this, out redo);
			if (a_addToRedo)
			{
				m_redoStack.Push(redo);
			}
		}

		private void Redo()
		{
			if (!(m_redoStack.Peek() is BatchUndoOperationMarker))
			{
				SingleRedo();
			}
			else
			{
				SingleRedo(); // remove first batch marker from the stack
				while (!(m_redoStack.Peek() is BatchUndoOperationMarker) && m_redoStack.Count > 0)
				{
					SingleRedo();
				}
				SingleRedo(); // remove second batch marker from the stack
			}
			if (m_redoStack.Count > 0 && m_redoStack.Peek() is ConcatOperationMarker)
			{
				SingleRedo(); //Remove concat operation from stack
				Redo();
			}
		}

		private void SingleRedo()
		{
			UndoOperation undo;
			UndoOperation redo = m_redoStack.Pop();
			Debug.Log("Redid: " + redo.GetType().Name);
			redo.Undo(this, out undo);
			m_undoStack.Push(undo);
		}

		public void UpdateUndoRedoButtonEnabled()
		{
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonInteractable(ToolbarInput.Redo, m_redoStack.Count > 0);
			InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.m_toolBar.SetButtonInteractable(ToolbarInput.Undo, m_undoStack.Count > 0);
		}

		public FSMState GetCurrentState()
		{
			return m_currentState;
		}

		public void AbortCurrentState()
		{
			m_currentState.Abort();
		}

		public void SetCurrentState(FSMState a_newState)
		{
			Vector3 mousePosition = GetWorldMousePosition();

			m_currentState.ExitState(mousePosition);

			m_userMayBeClicking = false;
			m_dragging = false;

			SetCursor(CursorType.Default);
			SetSnappingEnabled(false);

			m_currentState = a_newState;
			a_newState.EnterState(mousePosition);
		}

		public void SetInterruptState(FSMState a_newInterruptState)
		{
			Vector3 mousePosition = GetWorldMousePosition();
			if (m_interruptState != null)
				m_interruptState.ExitState(mousePosition);

			m_userMayBeClicking = false;
			m_dragging = false;

			m_interruptState = a_newInterruptState;
			if(a_newInterruptState != null)
				a_newInterruptState.EnterState(mousePosition);
		}

		public Vector3 GetWorldMousePosition()
		{
			Vector3 position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
			position.z = 0;

			if (m_snappingEnabled) { position = GetSnappedMousePosition(position); }

			return position;
		}

		private Vector3 GetSnappedMousePosition(Vector3 a_mousePosition)
		{
			List<SubEntity> subEntities = new List<SubEntity>();
			List<AbstractLayer> visibleLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth();

			foreach (AbstractLayer layer in visibleLayers)
			{
				List<SubEntity> layerSubEntities = layer.GetSubEntitiesAt(a_mousePosition);

				foreach (SubEntity layerSubEntity in layerSubEntities)
				{
					if (layerSubEntity.SnappingToThisEnabled)
					{
						subEntities.Add(layerSubEntity);
					}
				}
			}

			if (subEntities.Count == 0) { return a_mousePosition; }

			Vector3 snappedPosition = Vector3.zero;
			float snappedPositionSqrDistance = float.MaxValue;

			foreach (SubEntity subEntity in subEntities)
			{
				Vector3 closestPoint = subEntity.GetPointClosestTo(a_mousePosition);
				float sqrDistance = (a_mousePosition - closestPoint).sqrMagnitude;

				if (!(sqrDistance < snappedPositionSqrDistance))
					continue;
				snappedPosition = closestPoint;
				snappedPositionSqrDistance = sqrDistance;
			}

			float selectMaxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();
			if (snappedPositionSqrDistance > selectMaxDistance * selectMaxDistance)
			{
				return a_mousePosition;
			}
			return snappedPosition;
		}

		public void Update()
		{
			bool cursorIsOverUI = EventSystem.current.IsPointerOverGameObject();
			if (cursorIsOverUI) { m_userMayBeClicking = false; m_userMayBeDragging = false; }

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

				if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && m_undoStack.Count > 0)
				{
					SetInterruptState(null);
					Undo();
					UpdateUndoRedoButtonEnabled();
				}
				else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && m_redoStack.Count > 0)
				{
					SetInterruptState(null);
					Redo();
					UpdateUndoRedoButtonEnabled();
				}

			}

			// skip mouse input handling if the cursor is outside of the viewport (unless the user is dragging)
			if (!m_dragging && (
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

				m_leftMouseButtonDownStartPosition = mousePosition;
				m_userMayBeClicking = true;
				m_userMayBeDragging = true;
			}


			if (Input.GetMouseButtonUp(0))
			{
				if (!cursorIsOverUI && (m_userMayBeClicking || m_dragging || m_userMayBeDragging)) { InputReceivingState.LeftMouseButtonUp(m_leftMouseButtonDownStartPosition, mousePosition); }

				if (m_userMayBeClicking)
				{
					if (Time.time - m_previousClickTime < DOUBLE_CLICK_MAX_INTERVAL && (mousePosition - m_previousClickLocation).magnitude < VisualizationUtil.Instance.GetMouseMoveThreshold())
					{
						InputReceivingState.DoubleClick(mousePosition);
					}

					InputReceivingState.LeftClick(mousePosition);

					m_previousClickTime = Time.time;
					m_previousClickLocation = mousePosition;
				}

				m_userMayBeClicking = false;

				if (m_dragging)
				{
					InputReceivingState.StoppedDragging(m_leftMouseButtonDownStartPosition, mousePosition);
				}
				m_dragging = false;
				m_userMayBeDragging = false;
			}

			if (m_userMayBeClicking)
			{
				float threshold = VisualizationUtil.Instance.GetMouseMoveThreshold();
				threshold *= threshold;

				if ((mousePosition - m_leftMouseButtonDownStartPosition).sqrMagnitude > threshold)
				{
					m_userMayBeClicking = false;
				}
			}

			if (mousePosition != m_previousMousePosition && !m_userMayBeClicking)
			{
				InputReceivingState.MouseMoved(m_previousMousePosition, mousePosition, cursorIsOverUI);
				if (!m_dragging && m_userMayBeDragging && Input.GetMouseButton(0))
				{
					InputReceivingState.StartedDragging(m_leftMouseButtonDownStartPosition, mousePosition);
					m_dragging = true;
				}
				else if (m_dragging)
				{
					InputReceivingState.Dragging(m_leftMouseButtonDownStartPosition, mousePosition);
				}
			}

			m_previousMousePosition = mousePosition;
		}

		public static void CameraZoomChanged()
		{
			if (Instance == null)
				return;
			Instance.InputReceivingState.HandleCameraZoomChanged();
		}

		public void TriggerGeometryComplete()
		{
			OnGeometryCompleted?.Invoke();
		}
	}
}
