using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerProbeState : FSMState
	{
		private FSM.CursorType m_previousCursorType;
		private CustomToggle m_stateToggle;

		public LayerProbeState(FSM a_fsm, CustomToggle a_stateToggle) : base(a_fsm)
		{
			m_stateToggle = a_stateToggle;
		}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			base.EnterState(a_currentMousePosition);

			//Cache previous cursor & Set cursor
			m_previousCursorType = m_fsm.CurrentCursorType;
			m_fsm.SetCursor(FSM.CursorType.LayerProbe);
			m_stateToggle.isOn = true;
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			base.ExitState(a_currentMousePosition);
			m_fsm.SetCursor(m_previousCursorType);
			m_stateToggle.isOn = false;
		}

		public override void LeftMouseButtonDown(Vector3 a_position)
		{
			List<SubEntity> subEntities = new List<SubEntity>();
			List<AbstractLayer> loadedLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth(); // change this back to loaded layers by depth, for the layerprobe

			foreach (AbstractLayer layer in loadedLayers)
			{
				if (!layer.m_selectable) { continue; }

				foreach (SubEntity entity in layer.GetSubEntitiesAt(a_position))
				{
					if (entity.PlanState != SubEntityPlanState.NotShown)
						subEntities.Add(entity);
				}
			}

			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			Vector3 windowPosition = new Vector3(Input.mousePosition.x / scale, (Input.mousePosition.y - Screen.height) / scale);

			if (subEntities.Count > 0)
				InterfaceCanvas.Instance.layerProbeWindow.ShowLayerProbeWindow(subEntities, a_position, windowPosition);
			m_fsm.SetInterruptState(null);
		}
	}
}
