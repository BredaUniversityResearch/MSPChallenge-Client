using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EnergyPolygonLayer : PolygonLayer
	{
		public PointLayer m_centerPointLayer;

		public EnergyPolygonLayer(LayerMeta a_layerMeta, List<SubEntityObject> a_layerObjects) : base(a_layerMeta, a_layerObjects)
		{
		}

		public override void Initialise()
		{
			//Create new point layer thats only used for centerpoints
			LayerMeta meta = new LayerMeta {
				layer_name = "CenterPointLayer_For_" + ShortName
			};
			m_centerPointLayer = new PointLayer(meta, new List<SubEntityObject>(), this);
			m_centerPointLayer.m_entityTypes.Add(0, new EntityType());
			m_centerPointLayer.m_editingType = EditingType.SourcePolygonPoint;
			m_centerPointLayer.m_greenEnergy = m_greenEnergy;
			PolicyLogicEnergy.Instance.AddEnergyPointLayer(m_centerPointLayer);
			m_centerPointLayer.DrawGameObject();
		}

		public override void LayerShown()
		{
			m_centerPointLayer.LayerGameObject.SetActive(true);
			m_centerPointLayer.LayerShown();
		}

		public override void LayerHidden()
		{
			m_centerPointLayer.LayerGameObject.SetActive(false);
			m_centerPointLayer.LayerHidden();
		}

		public override void UpdateScale(Camera a_targetCamera)
		{
			base.UpdateScale(a_targetCamera);
			m_centerPointLayer.UpdateScale(a_targetCamera);
		}

		public override void SetEntitiesActiveUpTo(int a_index, bool a_showRemovedInLatestPlan = true, bool a_showCurrentIfNotInfluencing = true)
		{
			m_centerPointLayer.m_activeEntities.Clear();

			base.SetEntitiesActiveUpTo(a_index, a_showRemovedInLatestPlan, a_showCurrentIfNotInfluencing);

			foreach (PolygonEntity entity in m_activeEntities)
				foreach (PolygonSubEntity subent in entity.GetSubEntities())
					m_centerPointLayer.m_activeEntities.Add((subent as EnergyPolygonSubEntity).m_sourcePoint.m_entity as PointEntity);

			foreach (PointEntity ent in m_centerPointLayer.Entities)
				ent.RedrawGameObjects(CameraManager.Instance.gameCamera);
		}
	}
}
