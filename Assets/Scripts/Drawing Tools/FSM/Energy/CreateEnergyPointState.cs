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

			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			PointEntity entity = baseLayer.CreateNewPointEntity(finalPosition, selectedType != null ? selectedType : new List<EntityType>() { baseLayer.m_entityTypes.GetFirstValue() }, planLayer);
			baseLayer.m_activeEntities.Add(entity);
			PointSubEntity subEntity = entity.GetSubEntity(0) as PointSubEntity;
			subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
			subEntity.m_edited = true;

			fsm.TriggerGeometryComplete();
			fsm.AddToUndoStack(new CreateEnergyPointOperation(subEntity, planLayer));
		}
	}
}

