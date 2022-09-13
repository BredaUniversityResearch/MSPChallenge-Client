using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementEditingLayerType : ATutorialRequirement
	{
		enum EGeometryType { Point, Line, Polygon }
		[SerializeField] private EGeometryType m_type;

		public override bool EvaluateRequirement()
		{
			AbstractLayer layer = PlanDetails.LayersTab.CurrentlyEditingBaseLayer;
			if (layer == null)
				return false;
			switch (m_type)
			{
				case EGeometryType.Line:
					return layer is LineStringLayer;
				case EGeometryType.Polygon:
					return layer is PolygonLayer;
				default:
					return layer is PointLayer;
			}
		}
	}
}
