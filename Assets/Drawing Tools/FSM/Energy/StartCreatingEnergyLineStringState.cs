using UnityEngine;
using System.Collections.Generic;
using System.Text;

class StartCreatingEnergyLineStringState : StartCreatingLineStringState
{
	bool validPointTooltip = false;

    public StartCreatingEnergyLineStringState(FSM fsm, PlanLayer planLayer) : base(fsm, planLayer)
    {}

    public override void EnterState(Vector3 currentMousePosition)
    {
        //All points are non-reference
        foreach (AbstractLayer layer in LayerManager.energyLayers)
        {
            if (layer.greenEnergy == planLayer.BaseLayer.greenEnergy)
            {
                if (layer.editingType == AbstractLayer.EditingType.SourcePolygon)
                {
                    //Get and add the centerpoint layer
                    EnergyPolygonLayer polyLayer = (EnergyPolygonLayer)layer;
                    LayerManager.AddNonReferenceLayer(polyLayer.centerPointLayer, true);
                }
                else if (layer.editingType == AbstractLayer.EditingType.SourcePoint ||
                layer.editingType == AbstractLayer.EditingType.Socket ||
                layer.editingType == AbstractLayer.EditingType.Transformer)
                {
                    LayerManager.AddNonReferenceLayer(layer, true);
                }
            }
        }
        base.EnterState(currentMousePosition);
    }

	//Overriden to change cursor when not on a valid starting point
	public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
	{
		if (!cursorIsOverUI)
		{
			EnergyPointSubEntity point = LayerManager.GetEnergyPointAtPosition(currentPosition);
			if (point == null || !point.CanCableStartAtSubEntity(planLayer.BaseLayer.greenEnergy))
			{
				fsm.SetCursor(FSM.CursorType.Invalid);
				if (!showingToolTip || validPointTooltip)
				{
					TooltipManager.ForceSetToolTip("Select valid starting point");
					validPointTooltip = false;
				}
			}
			else
			{
				fsm.SetCursor(FSM.CursorType.Add);
				if (!showingToolTip || !validPointTooltip)
				{
					List<EntityType> entityTypes = UIManager.GetCurrentEntityTypeSelection();
					StringBuilder sb = new StringBuilder("Creating: " + entityTypes[0].Name);
					for (int i = 1; i < entityTypes.Count; i++)
						sb.Append("\n& " + entityTypes[i].Name);
					TooltipManager.ForceSetToolTip(sb.ToString());
					validPointTooltip = true;
				}
			}
			showingToolTip = true;
		}
		else
		{
			fsm.SetCursor(FSM.CursorType.Default);
			if (showingToolTip)
				TooltipManager.HideTooltip();
			showingToolTip = false;

		}
	}

	public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
    {        
        EnergyPointSubEntity point = LayerManager.GetEnergyPointAtPosition(finalPosition);
        if (point == null || !point.CanCableStartAtSubEntity(planLayer.BaseLayer.greenEnergy))
            return;

        LineStringEntity entity = baseLayer.CreateNewEnergyLineStringEntity(point.GetPosition(), new List<EntityType>() { baseLayer.EntityTypes.GetFirstValue() }, point, planLayer);
        baseLayer.activeEntities.Add(entity);
        entity.EntityTypes = UIManager.GetCurrentEntityTypeSelection();
        LineStringSubEntity subEntity = entity.GetSubEntity(0) as LineStringSubEntity;

        subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
        fsm.SetCurrentState(new CreatingEnergyLineStringState(fsm, planLayer, subEntity));
        fsm.AddToUndoStack(new CreateEnergyLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
 
        AudioMain.PlaySound(AudioMain.ITEM_PLACED);
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        LayerManager.SetNonReferenceLayers(new HashSet<AbstractLayer>() { PlanDetails.LayersTab.CurrentlyEditingBaseLayer }, false, true);
        base.ExitState(currentMousePosition);
    }

}

