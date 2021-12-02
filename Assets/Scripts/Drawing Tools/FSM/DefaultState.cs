using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DefaultState : FSMState
{
	private Entity currentHover = null;

	public DefaultState(FSM fsm) : base(fsm)
	{
	}

	public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
	{
		List<AbstractLayer> visibleLayers = LayerManager.GetVisibleLayersSortedByDepth();

		Entity hover = null;
		if (!cursorIsOverUI)
		{
			foreach (AbstractLayer layer in visibleLayers)
			{
				if (!layer.Selectable)
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

			if (hover != null && hover.Layer.Selectable == true)
			{
                HoveredSubEntity(hover.GetSubEntity(0), true);
            }

			currentHover = hover;
		}
	}

	public override void LeftClick(Vector3 worldPosition)
	{
		List<AbstractLayer> loadedLayers = LayerManager.GetVisibleLayersSortedByDepth(); // change this back to loaded layers by depth, for the layerprobe
		Vector3 windowPosition = Input.mousePosition;
		windowPosition.y -= Screen.height;


        if (Input.GetKey(KeyCode.LeftAlt))
        {
            List<SubEntity> subEntities = new List<SubEntity>();
            foreach (AbstractLayer layer in loadedLayers)
            {
                if (!layer.Selectable) { continue; }

                foreach (SubEntity entity in layer.GetSubEntitiesAt(worldPosition))
                {
                    if (entity.planState != SubEntityPlanState.NotShown)
                        subEntities.Add(entity);
                }
            }
            if (subEntities.Count > 0)
                UIManager.CreateLayerProbeWindow(subEntities, worldPosition, windowPosition);
        }
        else if (currentHover != null)
        {
            UIManager.CreatePropertiesWindow(currentHover.GetSubEntity(0), worldPosition, windowPosition);
        }
		

		//// This is if we dont want to be able to layer probe if no layers are visible at that position
		//if (currentHover != null)
		//{
		//    List<Entity> entities = new List<Entity>();
		//    List<Layer> loadedLayers = LayerManager.GetLoadedLayersSortedByDepth();

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
		//        UIManager.CreateLayerProbeWindow(entities, position + Vector3.one);
		//    }
		//    else
		//    {
		//        UIManager.CreatePropertiesWindow(currentHover, position + Vector3.one);
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
