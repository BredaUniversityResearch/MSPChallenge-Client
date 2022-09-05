using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	class CreateEnergyPointState : CreatePointsState
	{
		public CreateEnergyPointState(FSM fsm, PlanLayer planLayer) : base(fsm, planLayer)
		{
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			List<EntityType> selectedType = InterfaceCanvas.GetCurrentEntityTypeSelection();
			PointEntity entity = baseLayer.CreateNewPointEntity(finalPosition, selectedType != null ? selectedType : new List<EntityType>() { baseLayer.EntityTypes.GetFirstValue() }, planLayer);
			baseLayer.activeEntities.Add(entity);
			PointSubEntity subEntity = entity.GetSubEntity(0) as PointSubEntity;
			subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);

			fsm.TriggerGeometryComplete();
			fsm.AddToUndoStack(new CreateEnergyPointOperation(subEntity, planLayer));
		}
	}
}

