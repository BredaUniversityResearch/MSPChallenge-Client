using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CradleImpactTool
{
	public static class DebugDraw
	{
		public static void DrawEllipse(this Transform a_transform, Vector2 a_radius, int a_segments, Color a_colour, float a_duration = 0)
		{
			float angle = 0f;
			Quaternion rot = Quaternion.LookRotation(a_transform.forward, a_transform.up);
			Vector3 lastPosition = Vector3.zero;
			Vector3 currentPoint = Vector3.zero;

			float step = (Mathf.PI * 2) / a_segments;

			for (int i = 0; i < a_segments + 1; i++)
			{
				currentPoint.x = Mathf.Sin(angle) * a_radius.x;
				currentPoint.y = Mathf.Cos(angle) * a_radius.y;

				Vector3 actualPosition = rot * currentPoint + a_transform.position; // Store current offset position
				if (i > 0)
				{
					Debug.DrawLine(lastPosition, actualPosition, a_colour, a_duration);
				}

				lastPosition = actualPosition; // This is the calculated position, not the point.
				angle += step;
			}
		}

		public static void DrawBox(Vector3 a_position, Rect rect, Color a_colour, float a_duration)
		{
			Vector3 topLeft = new Vector3(rect.xMin, rect.yMin, 0) + a_position;
			Vector3 topRight = new Vector3(rect.xMax, rect.yMin, 0) + a_position;
			Vector3 botLeft = new Vector3(rect.xMin, rect.yMax, 0) + a_position;
			Vector3 botRight = new Vector3(rect.xMax, rect.yMax, 0) + a_position;

			Debug.DrawLine(topLeft, topRight, a_colour, a_duration);
			Debug.DrawLine(topRight, botRight, a_colour, a_duration);
			Debug.DrawLine(botRight, botLeft, a_colour, a_duration);
			Debug.DrawLine(botLeft, topLeft, a_colour, a_duration);
		}

		public static void DrawBox(Vector3 a_position, Bounds rect, Color a_colour, float a_duration)
		{
			Vector3 topLeft = new Vector3(rect.min.x, rect.min.y, 0) + a_position;
			Vector3 topRight = new Vector3(rect.max.x, rect.min.y, 0) + a_position;
			Vector3 botLeft = new Vector3(rect.min.x, rect.max.y, 0) + a_position;
			Vector3 botRight = new Vector3(rect.max.x, rect.max.y, 0) + a_position;

			Debug.DrawLine(topLeft, topRight, a_colour, a_duration);
			Debug.DrawLine(topRight, botRight, a_colour, a_duration);
			Debug.DrawLine(botRight, botLeft, a_colour, a_duration);
			Debug.DrawLine(botLeft, topLeft, a_colour, a_duration);
		}

		public static void DrawBox(Vector3 a_position, Rect rect, Color a_colour)
		{
			Vector3 topLeft = new Vector3(rect.xMin, rect.yMin, 0) + a_position;
			Vector3 topRight = new Vector3(rect.xMax, rect.yMin, 0) + a_position;
			Vector3 botLeft = new Vector3(rect.xMin, rect.yMax, 0) + a_position;
			Vector3 botRight = new Vector3(rect.xMax, rect.yMax, 0) + a_position;

			Debug.DrawLine(topLeft, topRight, a_colour);
			Debug.DrawLine(topRight, botRight, a_colour);
			Debug.DrawLine(botRight, botLeft, a_colour);
			Debug.DrawLine(botLeft, topLeft, a_colour);
		}

		public static void DrawBox(Vector3 a_position, Bounds rect, Color a_colour)
		{
			Vector3 topLeft = new Vector3(rect.min.x, rect.min.y, 0) + a_position;
			Vector3 topRight = new Vector3(rect.max.x, rect.min.y, 0) + a_position;
			Vector3 botLeft = new Vector3(rect.min.x, rect.max.y, 0) + a_position;
			Vector3 botRight = new Vector3(rect.max.x, rect.max.y, 0) + a_position;

			Debug.DrawLine(topLeft, topRight, a_colour);
			Debug.DrawLine(topRight, botRight, a_colour);
			Debug.DrawLine(botRight, botLeft, a_colour);
			Debug.DrawLine(botLeft, topLeft, a_colour);
		}
	}
}