using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DefaultState : FSMState
	{
		private Entity currentHover = null;

		public DefaultState(FSM fsm) : base(fsm)
		{
		}

		public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
		{
			List<AbstractLayer> visibleLayers = LayerManager.Instance.GetVisibleLayersSortedByDepth();

			Entity hover = null;
			if (!cursorIsOverUI)
			{
				foreach (AbstractLayer layer in visibleLayers)
				{
					if (!layer.m_selectable)
					{
						continue;
					}

					Entity layerHover = layer.GetEntityAt(currentPosition);
					if (layerHover != null)
					{
						hover = layerHover;
						break;
					}
				}
			}

			if (hover != currentHover)
			{
				if (currentHover != null)
				{
					HoveredSubEntity(currentHover.GetSubEntity(0), false);

				}

				if (hover != null && hover.Layer.m_selectable == true)
				{
					HoveredSubEntity(hover.GetSubEntity(0), true);
				}

				currentHover = hover;
			}
		}

		public override void LeftClick(Vector3 worldPosition)
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

					foreach (SubEntity entity in layer.GetSubEntitiesAt(worldPosition))
					{
						if (entity.PlanState != SubEntityPlanState.NotShown)
							subEntities.Add(entity);
					}
				}
				if (subEntities.Count > 0)
					InterfaceCanvas.Instance.layerProbeWindow.ShowLayerProbeWindow(subEntities, worldPosition, windowPosition);
			}
			else if (currentHover != null)
			{
				InterfaceCanvas.Instance.propertiesWindow.ShowPropertiesWindow(currentHover.GetSubEntity(0), worldPosition, windowPosition);
			}
		

			//// This is if we dont want to be able to layer probe if no layers are visible at that position
			//if (currentHover != null)
			//{
			//    List<Entity> entities = new List<Entity>();
			//    List<Layer> loadedLayers = LayerManager.Instance.GetLoadedLayersSortedByDepth();

			//    foreach (Layer layer in loadedLayers)
			//    {
			//        foreach (Entity entity in layer.GetEntitiesAt(position))
			//        {
			//            entities.Add(entity);
			//        }
			//    }

			//    position = Input.mousePosition;
			//    position.y -= Screen.height;

			//    if (entities.Count > 1)
			//    {
			//        InterfaceCanvas.CreateLayerProbeWindow(entities, position + Vector3.one);
			//    }
			//    else
			//    {
			//        InterfaceCanvas.CreatePropertiesWindow(currentHover, position + Vector3.one);
			//    }
			//}
		}

		public override void ExitState(Vector3 currentMousePosition)
		{
			if (currentHover != null)
			{
				HoveredSubEntity(currentHover.GetSubEntity(0), true);           
			}
		}
	}
}
