using UnityEngine;

namespace CradleImpactTool
{
	public static class BoundsExtensions
	{
		public static Vector2 GetClosestPoint(this Bounds a_bounds, Vector2 a_point)
		{
			Vector2 topLeft = new Vector2(a_bounds.min.x, a_bounds.min.y);
			Vector2 topRight = new Vector2(a_bounds.max.x, a_bounds.min.y);
			Vector2 bottomLeft = new Vector2(a_bounds.min.x, a_bounds.max.y);
			Vector2 bottomRight = new Vector2(a_bounds.max.x, a_bounds.max.y);

			Vector2[] points = new Vector2[]
			{
				Line.GetClosestPoint(a_point, topLeft, topRight),
				Line.GetClosestPoint(a_point, topLeft, bottomLeft),
				Line.GetClosestPoint(a_point, bottomRight, topRight),
				Line.GetClosestPoint(a_point, bottomRight, bottomLeft),
			};

			Vector2 closest = Vector2.zero;
			float closestDist = float.MaxValue;
			foreach (Vector2 point in points)
			{
				float dist = (a_point - point).sqrMagnitude;
				if (dist < closestDist)
				{
					closest = point;
					closestDist = dist;
				}
			}

			return closest;
		}
	}
}