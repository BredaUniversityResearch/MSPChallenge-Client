using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LineStringEntity : Entity
	{
		private List<LineStringSubEntity> m_lineStringSubEntities;

		public LineStringEntity(LineStringLayer a_layer, PlanLayer a_planLayer, List<EntityType> a_entityType) : base(a_layer, a_entityType, a_planLayer.Plan.Country)
		{
			PlanLayer = a_planLayer;
			m_lineStringSubEntities = new List<LineStringSubEntity>();
		}

		public LineStringEntity(LineStringLayer a_layer, SubEntityObject a_layerObject) : base(a_layer, a_layerObject)
		{
			m_lineStringSubEntities = new List<LineStringSubEntity>();
			if (a_layer.m_editingType == AbstractLayer.EditingType.Cable)
				m_lineStringSubEntities.Add(new EnergyLineStringSubEntity(this, a_layerObject, a_layerObject.id));
			else
				m_lineStringSubEntities.Add(new LineStringSubEntity(this, a_layerObject, a_layerObject.id));
		}

		public override SubEntity GetSubEntity(int a_index)
		{
			return m_lineStringSubEntities[a_index];
		}

		public LineStringSubEntity GetLineStringSubEntity()
		{
			return m_lineStringSubEntities[0];
		}

		public override int GetSubEntityCount()
		{
			return m_lineStringSubEntities.Count;
		}

		public void AddSubEntity(LineStringSubEntity a_subEntity)
		{
			m_lineStringSubEntities.Add(a_subEntity);
		}

		public List<LineStringSubEntity> GetSubEntities()
		{
			return m_lineStringSubEntities;
		}

		public override void RemoveGameObjects()
		{
			foreach (LineStringSubEntity lsse in m_lineStringSubEntities)
			{
				lsse.RemoveGameObject();
			}
			m_lineStringSubEntities = null;
		}

		public override void DrawGameObjects(Transform a_parent, SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default)
		{
			foreach (LineStringSubEntity lsse in m_lineStringSubEntities)
			{
				lsse.DrawGameObject(a_parent, a_drawMode);
			}
		}

		public override void RedrawGameObjects(Camera a_targetCamera, SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, bool a_forceScaleUpdate = false)
		{
			if (a_forceScaleUpdate)
			{
				foreach (LineStringSubEntity lsse in m_lineStringSubEntities)
				{
					lsse.RedrawGameObject(a_drawMode);
					lsse.UpdateScale(a_targetCamera);
				}
			}
			else
				foreach (LineStringSubEntity lsse in m_lineStringSubEntities)
					lsse.RedrawGameObject(a_drawMode);
		}

		public override float GetRestrictionAreaSurface()
		{
			if (m_lineStringSubEntities[0].m_restrictionArea)
				return InterfaceCanvas.Instance.mapScale.GetRealWorldPolygonAreaInSquareKm(m_lineStringSubEntities[0].m_restrictionArea.polygon);
			return m_lineStringSubEntities[0].LineLengthKm * Mathf.Max(GetCurrentRestrictionSize(), 0.25f);
			//TODO: optimize this so it no longer uses the polygon area, but one only based on restriction size and length
		}
	}
}
