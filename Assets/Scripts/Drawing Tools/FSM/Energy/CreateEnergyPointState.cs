using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class CreateEnergyPointState : CreatePointsState
{
    public CreateEnergyPointState(FSM fsm, PlanLayer planLayer) : base(fsm, planLayer)
    {
    }

    public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
    {
        AudioMain.PlaySound(AudioMain.ITEM_PLACED);

        List<EntityType> selectedType = UIManager.GetCurrentEntityTypeSelection();
        PointEntity entity = baseLayer.CreateNewPointEntity(finalPosition, selectedType != null ? selectedType : new List<EntityType>() { baseLayer.EntityTypes.GetFirstValue() }, planLayer);
        baseLayer.activeEntities.Add(entity);
        PointSubEntity subEntity = entity.GetSubEntity(0) as PointSubEntity;
        subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);

        fsm.AddToUndoStack(new CreateEnergyPointOperation(subEntity, planLayer));
    }
}

