using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class CreatingEnergyLineStringState : CreatingLineStringState
{
    public CreatingEnergyLineStringState(FSM fsm, PlanLayer planLayer, LineStringSubEntity subEntity) : base(fsm, planLayer, subEntity)
    { }

    public override void EnterState(Vector3 currentMousePosition)
    {
        EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)subEntity;
        if (cable.connections[0].point.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
        {
            //All points except sourcepoints are non-reference
            foreach (AbstractLayer layer in LayerManager.energyLayers)
            {
                if (layer.greenEnergy == cable.Entity.Layer.greenEnergy &&
                    (layer.editingType == AbstractLayer.EditingType.Socket ||
                    layer.editingType == AbstractLayer.EditingType.Transformer))
                    LayerManager.AddNonReferenceLayer(layer, true);
            }
        }
        else
        {
            //All points are non-reference
            foreach (AbstractLayer layer in LayerManager.energyLayers)
            {
                if (layer.greenEnergy == cable.Entity.Layer.greenEnergy)
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
        }

        base.EnterState(currentMousePosition);
    }

    protected override bool ClickingWouldFinishDrawing(Vector3 position, out Vector3 snappingPoint, out bool drawAsInvalid)
    {
        EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
        EnergyPointSubEntity point = LayerManager.GetEnergyPointAtPosition(position);
        if (point != null)
		{
			snappingPoint = point.GetPosition();
            if (point.CanConnectToEnergySubEntity(cable.connections.First().point))
            {
                drawAsInvalid = false;
                return true;
            }
            else
            {
                drawAsInvalid = true;
                return false;
            }
        }
		snappingPoint = position;
        drawAsInvalid = false;
		return false;
    }

    public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
    {
        AudioMain.PlaySound(AudioMain.ITEM_PLACED);

        EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
        EnergyPointSubEntity point = LayerManager.GetEnergyPointAtPosition(finalPosition);
        if (point != null)
        {
            if (point.CanConnectToEnergySubEntity(cable.connections.First().point))
            {
                //Valid energy points clicked: Finalize
                fsm.AddToUndoStack(new FinalizeEnergyLineStringOperation(subEntity, planLayer, point));
                FinalizeEnergyLineString(point);
                return;
            }
            else
            {
                //Invalid energy points clicked: do nothing
                subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
                return;
            }
        }

        //Empty space clicked: add point
		SubEntityDataCopy dataCopy = subEntity.GetDataCopy();
		subEntity.AddPoint(finalPosition);
        subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);

        fsm.AddToUndoStack(new ModifyEnergyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
    }

	public void FinalizeEnergyLineString(EnergyPointSubEntity point)
	{
		//Add connections
        EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
		subEntity.SetPointPosition(subEntity.GetPointCount() - 1, point.GetPosition());
		Connection con = new Connection(cable, point, false);
		cable.AddConnection(con);
		point.AddConnection(con);

		//Set entitytype
		List<EntityType> selectedType = UIManager.GetCurrentEntityTypeSelection();
		if (selectedType != null) { subEntity.Entity.EntityTypes = selectedType; }

		subEntity.restrictionNeedsUpdate = true;
		subEntity.UnHideRestrictionArea();
		subEntity.RedrawGameObject(SubEntityDrawMode.Default);

		subEntity = null; // set line string to null so the exit state function doesn't remove it

		fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, planLayer));
	}

    public override void HandleKeyboardEvents()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Abort();
        }
    }

    public override void Abort()
    {
        fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
        fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        LayerManager.SetNonReferenceLayers(new HashSet<AbstractLayer>() { PlanDetails.LayersTab.CurrentlyEditingBaseLayer }, false, true);
        base.ExitState(currentMousePosition);
    }
}

