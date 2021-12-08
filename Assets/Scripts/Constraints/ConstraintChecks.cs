using SoftwareRasterizerLib;
using System.Collections.Generic;
using UnityEngine;

public static class ConstraintChecks
{
	/// <summary>
	/// The generic check delegate.
	/// </summary>
	/// <param name="a">SubEntity A</param>
	/// <param name="b">Entity B</param>
	/// <param name="target">The constraint we are checking</param>
	/// <param name="targetPlanLayer">The plan layer we are checking issues for. Any issues will be submitted to this layer.</param>
	/// <param name="checkForPlan">The plan we are checking the issues for. The planLayer may or may not be contained within this plan. This is for tracking issues that are cross-plan.</param>
	/// <param name="issueLocation">Location where where the check roughly found an issue</param>
	/// <returns></returns>
	public delegate bool DoCheck(SubEntity a, SubEntity b, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation);

	private static bool CheckPointOnRasterToEntityType(RasterLayer raster, Vector2 point, EntityType entityType)
	{
		EntityType type = raster.GetEntityTypeForRasterAt(point);
		return type == entityType;
	}

	#region Raster
	private static bool PolyToRaster(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		issueLocation = Vector3.zero;
		
		// maybe make sure raster layer is always B (should then be enforced server side or when inputting the data!) To avoid that check
		if (subEntityB is PolygonSubEntity)
		{
			SubEntity tmpSubEntity = subEntityA;
			subEntityA = subEntityB;
			subEntityB = tmpSubEntity;
		}

		PolygonSubEntity poly = (PolygonSubEntity)subEntityA;
		RasterLayer rasterLayer = (RasterLayer)subEntityB.Entity.Layer;

		Vector3 polyCentre = subEntityA.BoundingBox.center;

		Rect rasterBounds = new Rect(rasterLayer.RasterBounds.position, rasterLayer.RasterBounds.size);
		PolygonRasterizer.Raster rasterizedPolygon = Rasterizer.CreateScanlinesForPolygon(rasterLayer.GetRasterImageWidth(), rasterLayer.GetRasterImageHeight(), poly.GetPoints(), rasterBounds);

		for (int y = rasterizedPolygon.m_scanlineMin; y < rasterizedPolygon.m_scanlineMax; ++y)
		{
			PolygonRasterizer.ScanlineMinMax scanline = rasterizedPolygon.m_scanlines[y];
			for (int x = (int)scanline.xMin; x < (int)scanline.xMax; ++x)
			{
				if (rasterLayer.GetEntityTypeForRasterAt(x, y) == target.entityType)
				{
					issueLocation = rasterLayer.GetWorldPositionForTextureLocation(x, y);
					return true;
				}
			}
		}

		return false;
	}

	private static bool LineToRaster(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		if (subEntityB is LineStringSubEntity)
		{
			SubEntity tmpSubentity = subEntityA;
			subEntityA = subEntityB;
			subEntityB = tmpSubentity;
		}

		EntityType targetEntityType = target.entityType;

		LineStringSubEntity line = (LineStringSubEntity)subEntityA;
		RasterLayer rasterLayer = (RasterLayer)subEntityB.Entity.Layer;

		List<Vector3> linePoints = line.GetPoints();

		foreach (Vector3 point in linePoints)
		{
			if (rasterLayer.GetEntityTypeForRasterAt(point) == targetEntityType)
			{
				issueLocation = point;
				return true;
			}
		}

		int count = linePoints.Count;

		for (int i = 0; i < count - 1; i += 2)
		{
			Vector3 pointA = linePoints[i];
			Vector3 pointB = linePoints[(i + 1) % count];
			float lengthOfLine = (pointB - pointA).magnitude;

			float increment = 5.0f;

			for (float j = 0; j < lengthOfLine; j += increment)
			{
				Vector3 point = Util.GetPointAlongLine(pointA, pointB, j);
				if (rasterLayer.GetEntityTypeForRasterAt(point) == target.entityType)
				{
					issueLocation = point;
					return true;
				}
			}
		}

		issueLocation = Vector3.zero;
		return false;
	}

	private static bool PointToRaster(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		if (subEntityB is PointSubEntity)
		{
			SubEntity tmpSubentity = subEntityA;
			subEntityA = subEntityB;
			subEntityB = tmpSubentity;
		}

		Vector3 pos = subEntityA.BoundingBox.center;

		RasterLayer rasterLayer = (RasterLayer)subEntityB.Entity.Layer;
		if (rasterLayer.GetEntityTypeForRasterAt(pos) == target.entityType)
		{
			issueLocation = pos;
			return true;
		}

		issueLocation = Vector3.zero;
		return false;
	}

	private static bool ExclusionCheckLineToPoly(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		PolygonSubEntity polyEntity = subEntityA as PolygonSubEntity;
		LineStringSubEntity lineEntity = subEntityB as LineStringSubEntity;
		if (polyEntity == null || lineEntity == null)
		{
			polyEntity = (PolygonSubEntity)subEntityB;
			lineEntity = (LineStringSubEntity)subEntityA;
		}

		List<Vector3> linePoints = lineEntity.GetPoints();
		for (int i = 0; i < linePoints.Count; ++i)
		{
			Vector3 linePoint = linePoints[i];
			if (!Util.PointCollidesWithPolygon(linePoint, polyEntity.GetPoints(), polyEntity.GetHoles(), 0.005f))
			{
				issueLocation = linePoint;
				return true;
			}
		}

		issueLocation = Vector3.zero;
		return false;
	}
	#endregion

	#region Inclusion Poly
	private static bool InclusionCheckPolyToPoly(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		Rect boundingBoxA = subEntityA.BoundingBox;
		Rect boundingBoxB = subEntityB.BoundingBox;

		// first check if the bounding boxes collide
		if (boundingBoxA.Overlaps(boundingBoxB))
		{
			List<Vector3> warningLocations;
			if (SetOperations.Overlap(subEntityA, subEntityB, out warningLocations))
			{
				issueLocation = warningLocations[0];
				return true;
			}
		}

		issueLocation = Vector3.zero;
		return false;
	}

	private static bool InclusionCheckPolyToLine(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		if (subEntityB is PolygonSubEntity)
		{
			SubEntity tmpSubentity = subEntityA;
			subEntityA = subEntityB;
			subEntityB = tmpSubentity;
		}
		Rect boundingBoxA = subEntityA.BoundingBox;
		Rect boundingBoxB = subEntityB.BoundingBox;

		if (boundingBoxA.Overlaps(boundingBoxB))
		{
			List<Vector3> warningLocations;
			if (SetOperations.OverlapPolygonLine(subEntityA, subEntityB, out warningLocations))
			{
				issueLocation = warningLocations[0];
				return true;
			}
		}

		issueLocation = Vector3.zero;
		return false;
	}

	private static bool InclusionCheckPolyToPoint(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		PointSubEntity pointSubEntity = subEntityA as PointSubEntity;
		PolygonSubEntity polygonSubEntity = subEntityB as PolygonSubEntity;

		if (pointSubEntity == null || polygonSubEntity == null)
		{
			pointSubEntity = (PointSubEntity)subEntityB;
			polygonSubEntity = (PolygonSubEntity)subEntityA;
		}

		Vector3 pointCenter = pointSubEntity.BoundingBox.center;

		if (Util.PointCollidesWithPolygon(pointCenter, polygonSubEntity.GetPoints(), polygonSubEntity.GetHoles(), ConstraintManager.ConstraintPointCollisionSize))
		{
			issueLocation = pointCenter;
			return true;
		}

		issueLocation = Vector3.zero;
		return false;
	}
	#endregion

	#region Inclusion Line
	private static bool InclusionCheckLineToLine(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		Rect boundingBoxA = subEntityA.BoundingBox;
		Rect boundingBoxB = subEntityB.BoundingBox;

		if (boundingBoxA.Overlaps(boundingBoxB))
		{
			LineStringSubEntity lineB = (LineStringSubEntity)subEntityB;
			List<Vector3> lineBPoints = lineB.GetPoints();

			if (Util.LineStringCollidesWithLineString(((LineStringSubEntity)subEntityA).GetPoints(), lineBPoints, out issueLocation))
			{
				return true;
			}
		}

		issueLocation = Vector3.zero;
		return false;
	}

	private static bool InclusionCheckLineToPoint(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		if (subEntityB is LineStringSubEntity)
		{
			SubEntity tmpSubentity = subEntityA;
			subEntityA = subEntityB;
			subEntityB = tmpSubentity;
		}

		Rect boundingBoxB = subEntityB.BoundingBox;

		if (Util.PointCollidesWithLineString(boundingBoxB.center, ((LineStringSubEntity)subEntityA).GetPoints(), ConstraintManager.ConstraintPointCollisionSize))
		{
			issueLocation = boundingBoxB.center;
			return true;
		}

		issueLocation = Vector3.zero;
		return false;
	}
	#endregion

	#region Inclusion Point
	private static bool InclusionCheckPointToPoint(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		Rect boundingBoxA = subEntityA.BoundingBox;
		Rect boundingBoxB = subEntityB.BoundingBox;

		if (Util.PointCollidesWithPoint(boundingBoxA.center, boundingBoxB.center, ConstraintManager.ConstraintPointCollisionSize))
		{
			issueLocation = boundingBoxA.center;
			return true;
		}

		issueLocation = Vector3.zero;
		return false;
	}
	#endregion

	#region Exclusion Poly (Incomplete)
	private static bool ExclusionCheckPolyToPoly(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		Rect boundingBoxA = subEntityA.BoundingBox;
		Rect boundingBoxB = subEntityB.BoundingBox;

		if (boundingBoxA.Overlaps(boundingBoxB))
		{
			PolygonSubEntity a = (PolygonSubEntity)subEntityA;
			PolygonSubEntity b = (PolygonSubEntity)subEntityB;

			List<Vector3> aPolygon = a.GetPoints();
			List<Vector3> bPolygon = b.GetPoints();

			foreach (Vector3 point in aPolygon)
			{
				if (!Util.PointInPolygon(point, bPolygon, null))
				{
					// point isnt in the other polygon
					issueLocation = point;
					return true;
				}
			}

			// overlapping
			issueLocation = Vector3.zero;
			return false;
		}

		// this is because the bounding boxes arent overlapping, so there is no inclusion
		issueLocation = boundingBoxA.center;
		return true;
	}
	#endregion

	#region Exclusion Line (TODO)
	#endregion

	#region Exclusion Point
	private static bool ExclusionCheckPointToPoint(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		Rect boundingBoxA = subEntityA.BoundingBox;
		Rect boundingBoxB = subEntityB.BoundingBox;

		issueLocation = boundingBoxA.center;

		if (Util.PointCollidesWithPoint(boundingBoxA.center, boundingBoxB.center, ConstraintManager.ConstraintPointCollisionSize))//if (boundingBoxA.Overlaps(subEntityB.BoundingBox))
		{
			return false;
		}

		return true;
	}

	private static bool ExclusionCheckPolyToPoint(SubEntity subEntityA, SubEntity subEntityB, ConstraintTarget target, PlanLayer targetPlanLayer, Plan checkForPlan, out Vector3 issueLocation)
	{
		PointSubEntity pointSubEntity = subEntityA as PointSubEntity;
		PolygonSubEntity polygonSubEntity = subEntityB as PolygonSubEntity;

		if (pointSubEntity == null || polygonSubEntity == null)
		{
			pointSubEntity = (PointSubEntity)subEntityB;
			polygonSubEntity = (PolygonSubEntity)subEntityA;
		}

		Vector3 pointCenter = pointSubEntity.BoundingBox.center;

		if (!Util.PointCollidesWithPolygon(pointCenter, polygonSubEntity.GetPoints(), polygonSubEntity.GetHoles(), ConstraintManager.ConstraintPointCollisionSize))
		{
			issueLocation = pointCenter;
			return true;
		}

		issueLocation = Vector3.zero;
		return false;
	}
	#endregion


	/// <summary>
	///  Takes the correct type of restriction check based on what the layers are
	/// </summary>
	/// <param name="layerA"></param>
	/// <param name="layerB"></param>
	/// <returns></returns>
	public static DoCheck PickCorrectInclusionCheckType(AbstractLayer layerA, AbstractLayer layerB)
	{
		if ((layerA is PolygonLayer && layerB is RasterLayer) || (layerA is RasterLayer && layerB is PolygonLayer))
		{
			return PolyToRaster;
		}
		else if ((layerA is LineStringLayer && layerB is RasterLayer) || (layerA is RasterLayer && layerB is LineStringLayer))
		{
			return LineToRaster;
		}
		else if ((layerA is PointLayer && layerB is RasterLayer) || (layerA is RasterLayer && layerB is PointLayer))
		{
			return PointToRaster;
		}
		else if (layerA is PolygonLayer && layerB is PolygonLayer)
		{
			return InclusionCheckPolyToPoly;
		}
		else if (layerA is PointLayer && layerB is PointLayer)
		{
			return InclusionCheckPointToPoint;
		}
		else if (layerA is LineStringLayer && layerB is LineStringLayer)
		{
			return InclusionCheckLineToLine;
		}
		else if ((layerA is PolygonLayer && layerB is PointLayer) || (layerA is PointLayer && layerB is PolygonLayer))
		{
			return InclusionCheckPolyToPoint;
		}
		else if ((layerA is PolygonLayer && layerB is LineStringLayer) || (layerA is LineStringLayer && layerB is PolygonLayer))
		{
			return InclusionCheckPolyToLine;
		}
		else if ((layerA is LineStringLayer && layerB is PointLayer) || (layerA is PointLayer && layerB is LineStringLayer))
		{
			return InclusionCheckLineToPoint;
		}
		return null;
	}

	public static DoCheck PickCorrectExclusionCheckType(AbstractLayer layerA, AbstractLayer layerB)
	{
		if ((layerA is PolygonLayer && layerB is RasterLayer) || (layerA is RasterLayer && layerB is PolygonLayer))
		{
			return PolyToRaster;
		}
		else if ((layerA is LineStringLayer && layerB is RasterLayer) || (layerA is RasterLayer && layerB is LineStringLayer))
		{
			return LineToRaster;
		}
		else if ((layerA is PointLayer && layerB is RasterLayer) || (layerA is RasterLayer && layerB is PointLayer))
		{
			return PointToRaster;
		}
		else if ((layerA is LineStringLayer && layerB is PolygonLayer) || (layerA is PolygonLayer && layerB is LineStringLayer))
		{
			return ExclusionCheckLineToPoly;
		}
		else if ((layerA is PointLayer && layerB is PolygonLayer) || (layerA is PolygonLayer && layerB is PointLayer))
		{
			return ExclusionCheckPolyToPoint;
		}
		else if (layerA is PolygonLayer && layerB is PolygonLayer)
		{
			return ExclusionCheckPolyToPoly;
		}
		else if (layerA is PointLayer && layerB is PointLayer)
		{
			return ExclusionCheckPointToPoint;
		}
		return null;
	}
}