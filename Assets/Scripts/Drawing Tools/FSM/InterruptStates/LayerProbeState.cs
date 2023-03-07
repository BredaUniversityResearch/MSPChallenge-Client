using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerProbeState : FSMState
	{
		FSM.CursorType previousCursorType;
        CustomToggle stateToggle;

		public LayerProbeState(FSM fsm, CustomToggle stateToggle) : base(fsm)
		{
			this.stateToggle = stateToggle;
		}

		public override void EnterState(Vector3 currentMousePosition)
		{
			base.EnterState(currentMousePosition);

			//Cache previous cursor & Set cursor
			previousCursorType = fsm.CurrentCursorType;
			fsm.SetCursor(FSM.CursorType.LayerProbe);
			stateToggle.isOn = true;
		}

		public override void ExitState(Vector3 currentMousePosition)
		{
			base.ExitState(currentMousePosition);
			fsm.SetCursor(previousCursorType);
			stateToggle.isOn = false;
		}

		public override void LeftMouseButtonDown(Vector3 position)
		{
			List<SubEntity> subEntities = new List<SubEntity>();
			List<AbstractLayer> loadedLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth(); // change this back to loaded layers by depth, for the layerprobe

			foreach (AbstractLayer layer in loadedLayers)
			{
				if (!layer.Selectable) { continue; }

				foreach (SubEntity entity in layer.GetSubEntitiesAt(position))
				{
					if (entity.planState != SubEntityPlanState.NotShown)
						subEntities.Add(entity);
				}
			}

			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			Vector3 windowPosition = new Vector3(Input.mousePosition.x / scale, (Input.mousePosition.y - Screen.height) / scale);

			if (subEntities.Count > 0)
				InterfaceCanvas.Instance.layerProbeWindow.ShowLayerProbeWindow(subEntities, position, windowPosition);
			fsm.SetInterruptState(null);
		}

	}
}

