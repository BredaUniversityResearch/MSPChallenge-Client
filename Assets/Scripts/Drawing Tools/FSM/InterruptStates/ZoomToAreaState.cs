using UnityEngine;

namespace MSP2050.Scripts
{
	public class ZoomToAreaState : FSMState
	{
		private FSM.CursorType m_previousCursorType;
		private CustomToggle m_stateToggle;

		public ZoomToAreaState(FSM a_fsm, CustomToggle a_mapScaleToolButton) : base(a_fsm)
		{
			m_stateToggle = a_mapScaleToolButton;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			//Cache previous cursor & Set cursor
			m_previousCursorType = m_fsm.CurrentCursorType;
			m_fsm.SetCursor(FSM.CursorType.ZoomToArea);
			m_stateToggle.isOn = true;
        }

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			base.ExitState(a_currentMousePosition);
			BoxSelect.HideBoxSelection();
			m_fsm.SetCursor(m_previousCursorType);
			m_stateToggle.isOn = false;
		}

		public override void StartedDragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);
		}

		public override void Dragging(Vector3 a_dragStartPosition, Vector3 a_currentPosition)
		{
			BoxSelect.DrawBoxSelection(a_dragStartPosition, a_currentPosition);
		}

		public override void StoppedDragging(Vector3 a_dragStartPosition, Vector3 a_dragFinalPosition)
		{
			float xmin, width, ymin, height;
			if (a_dragStartPosition.x < a_dragFinalPosition.x)
			{
				xmin = a_dragStartPosition.x;
				width = Mathf.Max(1f, a_dragFinalPosition.x - a_dragStartPosition.x);
			}
			else
			{
				xmin = a_dragFinalPosition.x;
				width = Mathf.Max(1f, a_dragStartPosition.x - a_dragFinalPosition.x);
			}
			if (a_dragStartPosition.y < a_dragFinalPosition.y)
			{
				ymin = a_dragStartPosition.y;
				height = Mathf.Max(1f, a_dragFinalPosition.y - a_dragStartPosition.y);
			}
			else
			{
				ymin = a_dragFinalPosition.y;
				height = Mathf.Max(1f, a_dragStartPosition.y - a_dragFinalPosition.y);
			}

			CameraManager.Instance.ZoomToBounds(new Rect(xmin, ymin, width, height), 1f);
			m_fsm.SetInterruptState(null);
		}

	}
}
