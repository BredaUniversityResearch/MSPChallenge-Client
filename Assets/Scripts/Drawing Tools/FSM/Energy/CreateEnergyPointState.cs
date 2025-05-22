using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	class CreateEnergyPointState : CreatePointsState
	{
		public CreateEnergyPointState(FSM a_fsm, PlanLayer a_planLayer) : base(a_fsm, a_planLayer)
		{
		}

		public override void LeftMouseButtonUp(Vector3 a_startPosition, Vector3 a_finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			PointEntity entity = baseLayer.CreateNewPointEntity(a_finalPosition, selectedType != null ? selectedType : new List<EntityType>() { baseLayer.m_entityTypes.GetFirstValue() }, planLayer);
			baseLayer.m_activeEntities.Add(entity);
			PointSubEntity subEntity = entity.GetSubEntity(0) as PointSubEntity;
			subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
			subEntity.m_edited = true;

			m_fsm.TriggerGeometryComplete();
			m_fsm.AddToUndoStack(new CreateEnergyPointOperation(subEntity, planLayer));
			if (!Input.GetKey(KeyCode.LeftShift))
				m_fsm.SetCurrentState(new EditEnergyPointsState(m_fsm, planLayer, new HashSet<PointSubEntity>() { subEntity }));
		}
	}
}
