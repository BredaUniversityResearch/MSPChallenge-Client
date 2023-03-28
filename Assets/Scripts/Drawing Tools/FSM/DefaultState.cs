using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DefaultState : FSMState
	{
		private Entity m_currentHover = null;

		public DefaultState(FSM a_fsm) : base(a_fsm)
		{
		}

		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			List<AbstractLayer> visibleLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth();

			Entity hover = null;
			if (!a_cursorIsOverUI)
			{
				foreach (AbstractLayer layer in visibleLayers)
				{
					if (!layer.m_selectable)
					{
						continue;
					}

					Entity layerHover = layer.GetEntityAt(a_currentPosition);
					if (layerHover == null)
						continue;
					hover = layerHover;
					break;
				}
			}

			if (hover == m_currentHover)
				return;
			if (m_currentHover != null)
			{
				HoveredSubEntity(m_currentHover.GetSubEntity(0), false);

			}

			if (hover != null && hover.Layer.m_selectable == true)
			{
				HoveredSubEntity(hover.GetSubEntity(0), true);
			}

			m_currentHover = hover;
		}

		public override void LeftClick(Vector3 a_worldPosition)
		{
			List<AbstractLayer> loadedLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth(); // change this back to loaded layers by depth, for the layerprobe
			float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
			Vector3 windowPosition = new Vector3(Input.mousePosition.x / scale, (Input.mousePosition.y - Screen.height) / scale);

			if (Input.GetKey(KeyCode.LeftAlt))
			{
				List<SubEntity> subEntities = new List<SubEntity>();
				foreach (AbstractLayer layer in loadedLayers)
				{
					if (!layer.m_selectable) { continue; }

					foreach (SubEntity entity in layer.GetSubEntitiesAt(a_worldPosition))
					{
						if (entity.PlanState != SubEntityPlanState.NotShown)
							subEntities.Add(entity);
					}
				}
				if (subEntities.Count > 0)
					InterfaceCanvas.Instance.layerProbeWindow.ShowLayerProbeWindow(subEntities, a_worldPosition, windowPosition);
			}
			else if (m_currentHover != null)
			{
				InterfaceCanvas.Instance.propertiesWindow.ShowPropertiesWindow(m_currentHover.GetSubEntity(0), a_worldPosition, windowPosition);
			}
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			if (m_currentHover != null)
			{
				HoveredSubEntity(m_currentHover.GetSubEntity(0), true);           
			}
		}
	}
}
