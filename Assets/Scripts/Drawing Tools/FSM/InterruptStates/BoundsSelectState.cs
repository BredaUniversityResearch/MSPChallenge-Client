using UnityEngine;
using UnityEngine.UIElements;

namespace MSP2050.Scripts
{
	public class BoundsSelectState : FSMState
	{
		private FSM.CursorType m_previousCursorType;
		private GenericBoundsField m_boundsField;

		public BoundsSelectState(FSM a_fsm, GenericBoundsField a_boundsField) : base(a_fsm)
		{
			m_boundsField = a_boundsField;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			//Cache previous cursor & Set cursor
			m_previousCursorType = m_fsm.CurrentCursorType;
			m_fsm.SetCursor(FSM.CursorType.ZoomToArea);
        }

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			base.ExitState(a_currentMousePosition);
			BoxSelect.HideBoxSelection();
			m_fsm.SetCursor(m_previousCursorType);
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
			float xmin, xmax, ymin, ymax;
			if (a_dragStartPosition.x < a_dragFinalPosition.x)
			{
				xmin = a_dragStartPosition.x * Main.SCALE;
				xmax = a_dragFinalPosition.x * Main.SCALE;
			}
			else
			{
				xmin = a_dragFinalPosition.x * Main.SCALE;
				xmax = a_dragStartPosition.x * Main.SCALE;
			}
			if (a_dragStartPosition.y < a_dragFinalPosition.y)
			{
				ymin = a_dragStartPosition.y * Main.SCALE;
				ymax = a_dragFinalPosition.y * Main.SCALE;
			}
			else
			{
				ymin = a_dragFinalPosition.y * Main.SCALE;
				ymax = a_dragStartPosition.y * Main.SCALE;
			}

			m_boundsField.SetContent(new Vector4(xmin, ymin, xmax, ymax));
			m_fsm.SetInterruptState(null);
		}

	}
}
