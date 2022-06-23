using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public static class GeometryOperations
	{
		public static float intConverstion = 100000000000000.0f;

		public static bool LineIntersection(List<Vector3> source, List<Vector3> target, out List<Vector3> intersectionPoints)
		{
			List<ClipperLib.IntPoint> a0 = (VectorToIntPoint(source));
			List<ClipperLib.IntPoint> b0 = (VectorToIntPoint(target));

			ClipperLib.Clipper clipper = new ClipperLib.Clipper();
			clipper.AddPath(a0, ClipperLib.PolyType.ptSubject, false);
			clipper.AddPath(b0, ClipperLib.PolyType.ptClip, false);

			List<List<ClipperLib.IntPoint>> solution = new List<List<ClipperLib.IntPoint>>();

			clipper.Execute(ClipperLib.ClipType.ctIntersection, solution, ClipperLib.PolyFillType.pftEvenOdd);

			if (solution.Count > 0)
			{
				intersectionPoints = new List<Vector3>();
				foreach (List<ClipperLib.IntPoint> point in solution)
				{
					intersectionPoints.Add(Util.GetCentroid(IntPointToVector(point)));
				}
				return true;
			}

			intersectionPoints = null;
			return false;
		}

		public static bool OverlapPolygonLine(SubEntity polygonSource, SubEntity lineTarget, out List<Vector3> intersectionCentre)
		{
			((PolygonSubEntity)polygonSource).ValidateWindingOrders();

			List<ClipperLib.IntPoint> a0 = VectorToIntPoint(((PolygonSubEntity)polygonSource).GetPoints());
			List<ClipperLib.IntPoint> b0 = VectorToIntPoint(((LineStringSubEntity)lineTarget).GetPoints());


			// maybe reverse the clip and subject
			ClipperLib.Clipper clipper = new ClipperLib.Clipper();
			clipper.AddPath(b0, ClipperLib.PolyType.ptSubject, false);
			clipper.AddPath(a0, ClipperLib.PolyType.ptClip, true);

			//List<List<IntPoint>> solution = new List<List<IntPoint>>();
			ClipperLib.PolyTree solution = new ClipperLib.PolyTree();

			clipper.Execute(ClipperLib.ClipType.ctIntersection, solution, ClipperLib.PolyFillType.pftEvenOdd);


			if (solution.Total > 0)
			{
				intersectionCentre = new List<Vector3>(solution.Childs.Count);
				foreach (ClipperLib.PolyNode node in solution.Childs)
				{
					List<Vector3> intersectionPoints = IntPointToVector(node.Contour);
					intersectionCentre.Add(Util.GetCenter(intersectionPoints));
				}
				return true;
			}

			intersectionCentre = null;
			return false;
		}

		public static bool Overlap(SubEntity source, SubEntity target, out List<Vector3> intersectionCentre)
		{
			PolygonSubEntity sourcePolygon = (PolygonSubEntity)source;
			PolygonSubEntity targetPolygon = (PolygonSubEntity)target;
			sourcePolygon.ValidateWindingOrders();
			targetPolygon.ValidateWindingOrders();

			List<ClipperLib.IntPoint> a0 = VectorToIntPoint(sourcePolygon.GetPoints());
			List<ClipperLib.IntPoint> b0 = VectorToIntPoint(targetPolygon.GetPoints());

			ClipperLib.Clipper clipper = new ClipperLib.Clipper();
			clipper.AddPath(a0, ClipperLib.PolyType.ptSubject, true);
			clipper.AddPath(b0, ClipperLib.PolyType.ptClip, true);

			List<List<ClipperLib.IntPoint>> solution = new List<List<ClipperLib.IntPoint>>();

			clipper.Execute(ClipperLib.ClipType.ctIntersection, solution, ClipperLib.PolyFillType.pftEvenOdd);

			if (solution.Count > 0)
			{
				intersectionCentre = new List<Vector3>();
				foreach (List<ClipperLib.IntPoint> point in solution)
				{
					intersectionCentre.Add(Util.GetCentroid(IntPointToVector(point)));
				}
				return true;
			}

			intersectionCentre = null;
			return false;
		}

		public static bool Overlap(HashSet<SubEntity> source, HashSet<SubEntity> target, out List<Vector3> intersectionCentre)
		{
			List<List<ClipperLib.IntPoint>> a0 = new List<List<ClipperLib.IntPoint>>();// (VectorToIntPoint(source.GetPolygon()));
			List<List<ClipperLib.IntPoint>> b0 = new List<List<ClipperLib.IntPoint>>();// (VectorToIntPoint(target.GetPolygon()));

			foreach (PolygonSubEntity sub in source)
			{
				sub.ValidateWindingOrders();
				a0.Add(VectorToIntPoint(sub.GetPoints()));
			}

			foreach (PolygonSubEntity sub in target)
			{
				sub.ValidateWindingOrders();
				b0.Add(VectorToIntPoint(sub.GetPoints()));
			}

			ClipperLib.Clipper clipper = new ClipperLib.Clipper();
			clipper.AddPaths(a0, ClipperLib.PolyType.ptSubject, true);
			clipper.AddPaths(b0, ClipperLib.PolyType.ptClip, true);

			List<List<ClipperLib.IntPoint>> solution = new List<List<ClipperLib.IntPoint>>();

			clipper.Execute(ClipperLib.ClipType.ctIntersection, solution, ClipperLib.PolyFillType.pftEvenOdd);

			if (solution.Count > 0)
			{
				intersectionCentre = new List<Vector3>();
				foreach (List<ClipperLib.IntPoint> point in solution)
				{
					intersectionCentre.Add(Util.GetCentroid(IntPointToVector(point)));
				}
				return true;
			}

			intersectionCentre = null;
			return false;
		}

		public static List<Vector3> IntPointToVector(List<ClipperLib.IntPoint> points)
		{
			List<Vector3> verts = new List<Vector3>(points.Count);

			for (int i = 0; i < points.Count; i++)
			{
				verts.Add(new Vector3(points[i].X / intConverstion, points[i].Y / intConverstion));
			}

			return verts;
		}

		public static List<ClipperLib.IntPoint> VectorToIntPoint(List<Vector3> points)
		{
			List<ClipperLib.IntPoint> verts = new List<ClipperLib.IntPoint>();

			for (int i = 0; i < points.Count; i++)
			{
				verts.Add(new ClipperLib.IntPoint(points[i].x * intConverstion, points[i].y * intConverstion));
			}

			return verts;
		}
	}
}
