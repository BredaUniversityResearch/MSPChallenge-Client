using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;

public static class SetOperations
{
	public static float intConverstion = 100000000000000.0f;

	public static List<List<Vector3>> Boolean(PolygonEntity subject, HashSet<PolygonEntity> clip, ClipType type, out List<List<List<Vector3>>> holes)
	{
		List<List<IntPoint>> A = new List<List<IntPoint>>();
		List<List<IntPoint>> B = new List<List<IntPoint>>();

		for (int i = 0; i < subject.GetSubEntityCount(); i++)
		{
			PolygonSubEntity subEntity = subject.GetSubEntities()[i];

			List<List<IntPoint>> a0 = new List<List<IntPoint>>();
			a0.Add(VectorToIntPoint(subEntity.GetPoints()));

			if (subEntity.GetHoleCount() > 0)
			{
				foreach (List<Vector3> hole in subEntity.GetHoles())
				{
					a0.Add(VectorToIntPoint(hole));
				}
			}

			A = doUnion(A, a0);
		}

		foreach (PolygonEntity clipEntity in clip)
		{
			for (int i = 0; i < clipEntity.GetSubEntityCount(); i++)
			{
				PolygonSubEntity clipSubEntity = clipEntity.GetSubEntities()[i];

				List<List<IntPoint>> b0 = new List<List<IntPoint>>();
				b0.Add(VectorToIntPoint(clipSubEntity.GetPoints()));

				if (clipSubEntity.GetHoleCount() > 0)
				{
					foreach (List<Vector3> hole in clipSubEntity.GetHoles())
					{
						b0.Add(VectorToIntPoint(hole));
					}
				}

				B = doUnion(B, b0);
			}
		}

		List<List<Vector3>> poly = doOperation(A, B, type, PolyFillType.pftEvenOdd, out holes);

		return poly;
	}

	public static bool LineIntersection(List<Vector3> source, List<Vector3> target, out List<Vector3> intersectionPoints)
	{
		List<IntPoint> a0 = (VectorToIntPoint(source));
		List<IntPoint> b0 = (VectorToIntPoint(target));

		Clipper clipper = new Clipper();
		clipper.AddPath(a0, PolyType.ptSubject, false);
		clipper.AddPath(b0, PolyType.ptClip, false);

		List<List<IntPoint>> solution = new List<List<IntPoint>>();

		clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd);

		if (solution.Count > 0)
		{
			intersectionPoints = new List<Vector3>();
			foreach (List<IntPoint> point in solution)
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

		List<IntPoint> a0 = VectorToIntPoint(((PolygonSubEntity)polygonSource).GetPoints());
		List<IntPoint> b0 = VectorToIntPoint(((LineStringSubEntity)lineTarget).GetPoints());


		// maybe reverse the clip and subject
		Clipper clipper = new Clipper();
		clipper.AddPath(b0, PolyType.ptSubject, false);
		clipper.AddPath(a0, PolyType.ptClip, true);

		//List<List<IntPoint>> solution = new List<List<IntPoint>>();
		PolyTree solution = new PolyTree();

		clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd);


		if (solution.Total > 0)
		{
			intersectionCentre = new List<Vector3>(solution.Childs.Count);
			foreach (PolyNode node in solution.Childs)
			{
				List<Vector3> intersectionPoints = IntPointToVector(node.Contour);
				intersectionCentre.Add(Util.GetCenter(intersectionPoints));
			}
			return true;
		}
		//if (solution.Count > 0)
		//{
		//    intersectionCentre = new List<Vector3>();
		//    foreach (List<IntPoint> point in solution)
		//    {
		//        intersectionCentre.Add(Util.GetCentroid(IntPointToVector(point)));
		//    }
		//    return true;
		//}

		intersectionCentre = null;
		return false;
	}


	public static bool Overlap(SubEntity source, SubEntity target, out List<Vector3> intersectionCentre)
	{
		PolygonSubEntity sourcePolygon = (PolygonSubEntity)source;
		PolygonSubEntity targetPolygon = (PolygonSubEntity)target;
		sourcePolygon.ValidateWindingOrders();
		targetPolygon.ValidateWindingOrders();

		List<IntPoint> a0 = VectorToIntPoint(sourcePolygon.GetPoints());
		List<IntPoint> b0 = VectorToIntPoint(targetPolygon.GetPoints());

		Clipper clipper = new Clipper();
		clipper.AddPath(a0, PolyType.ptSubject, true);
		clipper.AddPath(b0, PolyType.ptClip, true);

		List<List<IntPoint>> solution = new List<List<IntPoint>>();

		clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd);

		if (solution.Count > 0)
		{
			intersectionCentre = new List<Vector3>();
			foreach (List<IntPoint> point in solution)
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
		List<List<IntPoint>> a0 = new List<List<IntPoint>>();// (VectorToIntPoint(source.GetPolygon()));
		List<List<IntPoint>> b0 = new List<List<IntPoint>>();// (VectorToIntPoint(target.GetPolygon()));

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

		Clipper clipper = new Clipper();
		clipper.AddPaths(a0, PolyType.ptSubject, true);
		clipper.AddPaths(b0, PolyType.ptClip, true);

		List<List<IntPoint>> solution = new List<List<IntPoint>>();

		clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd);

		if (solution.Count > 0)
		{
			intersectionCentre = new List<Vector3>();
			foreach (List<IntPoint> point in solution)
			{
				intersectionCentre.Add(Util.GetCentroid(IntPointToVector(point)));
			}
			return true;
		}

		intersectionCentre = null;
		return false;
	}

	public static PolygonEntity BooleanP(PolygonEntity subject, HashSet<PolygonEntity> clip, ClipType type)
	{
		subject.ValidateWindingOrders();
		foreach (PolygonEntity clipEntity in clip)
		{
			clipEntity.ValidateWindingOrders();
		}

		List<List<List<Vector3>>> holes;
		List<List<Vector3>> polys = Boolean(subject, clip, type, out holes);

		if (polys.Count == 0)
		{
			return null;
		}

		Debug.LogError("Set operations arent supposed to work");
		return null;
		//PolygonEntity entity = null;// layer.CreateNewPolygonEntity(subject.EntityTypes);

		//for (int i = 0; i < polys.Count; i++)
		//{
		//	PolygonSubEntity subEntity = layer.editingType == AbstractLayer.EditingType.SourcePolygon ? new EnergyPolygonSubEntity(entity) : new PolygonSubEntity(entity);
		//	subEntity.SetPolygon(polys[i]);

		//	if (holes[i].Count > 0)
		//	{
		//		for (int j = 0; j < holes[i].Count; j++)
		//		{
		//			List<Vector3> holevertices = new List<Vector3>();

		//			for (int k = 0; k < holes[i][j].Count; k++)
		//			{
		//				holevertices.Add(holes[i][j][k]);
		//			}
		//			subEntity.AddHole(holevertices);
		//		}
		//	}

		//	entity.AddSubEntity(subEntity);
		//}

		//entity.ReplaceMetaData(subject.metaData);

		//return entity;
	}

	private static List<List<Vector3>> doOperation(List<List<IntPoint>> A, List<List<IntPoint>> B, ClipType clipType, PolyFillType fillType, out List<List<List<Vector3>>> holes)
	{
		Clipper clipper = new Clipper();

		clipper.AddPaths(A, PolyType.ptSubject, true);
		clipper.AddPaths(B, PolyType.ptClip, true);

		PolyTree solution = new PolyTree();

		clipper.Execute(clipType, solution, fillType);

		List<List<Vector3>> polygon = new List<List<Vector3>>();
		holes = new List<List<List<Vector3>>>();

		AddChildGeometry(solution.Childs, ref polygon, ref holes);

		return polygon;
	}

	private static List<List<IntPoint>> doUnion(List<List<IntPoint>> a, List<List<IntPoint>> b)
	{
		Clipper clipper = new Clipper();

		clipper.AddPaths(a, PolyType.ptSubject, true);
		clipper.AddPaths(b, PolyType.ptClip, true);

		List<List<IntPoint>> solution = new List<List<IntPoint>>();

		clipper.Execute(ClipType.ctUnion, solution);

		return solution;
	}

	private static void AddChildGeometry(List<PolyNode> child, ref List<List<Vector3>> polygon, ref List<List<List<Vector3>>> holes, int parentIndex = -1)
	{
		for (int i = 0; i < child.Count; i++)
		{
			List<IntPoint> shape = child[i].Contour;

			if (!child[i].IsHole)
			{
				polygon.Add(IntPointToVector(shape));
				holes.Add(new List<List<Vector3>>());
				parentIndex = holes.Count - 1;
			}
			else
			{
				holes[parentIndex].Add(IntPointToVector(shape));
			}

			if (child[i].Childs.Count > 0)
			{
				AddChildGeometry(child[i].Childs, ref polygon, ref holes, parentIndex);
			}
		}
	}

	public static List<Vector3> IntPointToVector(List<IntPoint> points)
	{
		List<Vector3> verts = new List<Vector3>(points.Count);

		for (int i = 0; i < points.Count; i++)
		{
			verts.Add(new Vector3(points[i].X / intConverstion, points[i].Y / intConverstion));
		}

		return verts;
	}

	public static List<IntPoint> VectorToIntPoint(List<Vector3> points)
	{
		List<IntPoint> verts = new List<IntPoint>();

		for (int i = 0; i < points.Count; i++)
		{
			verts.Add(new IntPoint(points[i].x * intConverstion, points[i].y * intConverstion));
		}

		return verts;
	}
}
