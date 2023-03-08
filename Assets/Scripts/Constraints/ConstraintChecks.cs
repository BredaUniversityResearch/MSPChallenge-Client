using System.Collections.Generic;
using SoftwareRasterizerLib;
using UnityEngine;

namespace MSP2050.Scripts
{
	public static class ConstraintChecks
	{
		/// <summary>
		/// The generic check delegate.
		/// </summary>
		/// <param name="a_a">SubEntity A</param>
		/// <param name="a_b">Entity B</param>
		/// <param name="a_target">The constraint we are checking</param>
		/// <param name="a_targetPlanLayer">The plan layer we are checking issues for. Any issues will be submitted to this layer.</param>
		/// <param name="a_checkForPlan">The plan we are checking the issues for. The planLayer may or may not be contained within this plan. This is for tracking issues that are cross-plan.</param>
		/// <param name="a_issueLocation">Location where where the check roughly found an issue</param>
		/// <returns></returns>
		public delegate bool DoCheck(SubEntity a_a, SubEntity a_b, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation);

		private static bool CheckPointOnRasterToEntityType(RasterLayer a_raster, Vector2 a_point, EntityType a_entityType)
		{
			EntityType type = a_raster.GetEntityTypeForRasterAt(a_point);
			return type == a_entityType;
		}

		#region Raster
		private static bool PolyToRaster(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			a_issueLocation = Vector3.zero;
		
			// maybe make sure raster layer is always B (should then be enforced server side or when inputting the data!) To avoid that check
			if (a_subEntityB is PolygonSubEntity)
			{
				(a_subEntityA, a_subEntityB) = (a_subEntityB, a_subEntityA);
			}

			PolygonSubEntity poly = (PolygonSubEntity)a_subEntityA;
			RasterLayer rasterLayer = (RasterLayer)a_subEntityB.Entity.Layer;

			Vector3 polyCentre = a_subEntityA.BoundingBox.center;

			Rect rasterBounds = new Rect(rasterLayer.RasterBounds.position, rasterLayer.RasterBounds.size);
			PolygonRasterizer.Raster rasterizedPolygon = Rasterizer.CreateScanlinesForPolygon(rasterLayer.GetRasterImageWidth(), rasterLayer.GetRasterImageHeight(), poly.GetPoints(), rasterBounds);

			for (int y = rasterizedPolygon.m_scanlineMin; y < rasterizedPolygon.m_scanlineMax; ++y)
			{
				PolygonRasterizer.ScanlineMinMax scanline = rasterizedPolygon.m_scanlines[y];
				for (int x = (int)scanline.xMin; x < (int)scanline.xMax; ++x)
				{
					if (rasterLayer.GetEntityTypeForRasterAt(x, y) != a_target.entityType)
						continue;
					a_issueLocation = rasterLayer.GetWorldPositionForTextureLocation(x, y);
					return true;
				}
			}

			return false;
		}

		private static bool LineToRaster(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			if (a_subEntityB is LineStringSubEntity)
			{
				SubEntity tmpSubentity = a_subEntityA;
				a_subEntityA = a_subEntityB;
				a_subEntityB = tmpSubentity;
			}

			EntityType targetEntityType = a_target.entityType;

			LineStringSubEntity line = (LineStringSubEntity)a_subEntityA;
			RasterLayer rasterLayer = (RasterLayer)a_subEntityB.Entity.Layer;

			List<Vector3> linePoints = line.GetPoints();

			foreach (Vector3 point in linePoints)
			{
				if (rasterLayer.GetEntityTypeForRasterAt(point) != targetEntityType)
					continue;
				a_issueLocation = point;
				return true;
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
					if (rasterLayer.GetEntityTypeForRasterAt(point) != a_target.entityType)
						continue;
					a_issueLocation = point;
					return true;
				}
			}

			a_issueLocation = Vector3.zero;
			return false;
		}

		private static bool PointToRaster(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			if (a_subEntityB is PointSubEntity)
			{
				SubEntity tmpSubentity = a_subEntityA;
				a_subEntityA = a_subEntityB;
				a_subEntityB = tmpSubentity;
			}

			Vector3 pos = a_subEntityA.BoundingBox.center;

			RasterLayer rasterLayer = (RasterLayer)a_subEntityB.Entity.Layer;
			if (rasterLayer.GetEntityTypeForRasterAt(pos) == a_target.entityType)
			{
				a_issueLocation = pos;
				return true;
			}

			a_issueLocation = Vector3.zero;
			return false;
		}

		private static bool ExclusionCheckLineToPoly(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			PolygonSubEntity polyEntity = a_subEntityA as PolygonSubEntity;
			LineStringSubEntity lineEntity = a_subEntityB as LineStringSubEntity;
			if (polyEntity == null || lineEntity == null)
			{
				polyEntity = (PolygonSubEntity)a_subEntityB;
				lineEntity = (LineStringSubEntity)a_subEntityA;
			}

			List<Vector3> linePoints = lineEntity.GetPoints();
			for (int i = 0; i < linePoints.Count; ++i)
			{
				Vector3 linePoint = linePoints[i];
				if (Util.PointCollidesWithPolygon(linePoint, polyEntity.GetPoints(), polyEntity.GetHoles(), 0.005f))
					continue;
				a_issueLocation = linePoint;
				return true;
			}

			a_issueLocation = Vector3.zero;
			return false;
		}
		#endregion

		#region Inclusion Poly
		private static bool InclusionCheckPolyToPoly(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			PolygonSubEntity poly1 = a_subEntityA as PolygonSubEntity;
			PolygonSubEntity poly2 = a_subEntityB as PolygonSubEntity;
			if(poly1 == null || poly2 == null)
			{
				Debug.LogError($"Trying to perform a poly to poly check, but one of the arguments is not a polygon. Subent1: {FormatSubentityAndLayer(a_subEntityA, a_target.layer)}, subent2: {FormatSubentityAndLayer(a_subEntityB, a_targetPlanLayer.BaseLayer)}");
				a_issueLocation = Vector3.zero;
				return false;
			}

			// first check if the bounding boxes collide
			if (poly1.BoundingBox.Overlaps(poly2.BoundingBox))
			{
				List<Vector3> warningLocations;
				if (GeometryOperations.Overlap(poly1, poly2, out warningLocations))
				{
					a_issueLocation = warningLocations[0];
					return true;
				}
			}

			a_issueLocation = Vector3.zero;
			return false;
		}

		private static bool InclusionCheckPolyToLine(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			PolygonSubEntity poly = a_subEntityA as PolygonSubEntity;
			LineStringSubEntity line;
			if (poly == null)
			{
				if (a_subEntityB is PolygonSubEntity)
				{
					poly = (PolygonSubEntity)a_subEntityB;
					line = a_subEntityA as LineStringSubEntity;
				}
				else
				{
					Debug.LogError($"Trying to perform a poly to line check, but none of the arguments are polygons. Subent1: {FormatSubentityAndLayer(a_subEntityA, a_target.layer)}, subent2: {FormatSubentityAndLayer(a_subEntityB, a_targetPlanLayer.BaseLayer)}");
					a_issueLocation = Vector3.zero;
					return false;
				}
			}
			else
			{
				line = a_subEntityB as LineStringSubEntity;
			}
			if(line == null)
			{
				Debug.LogError($"Trying to perform a poly to line check, but none of the arguments are lines. Subent1: {FormatSubentityAndLayer(a_subEntityA, a_target.layer)}, subent2: {FormatSubentityAndLayer(a_subEntityB, a_targetPlanLayer.BaseLayer)}");
				a_issueLocation = Vector3.zero;
				return false;
			}

			Rect boundingBoxA = poly.BoundingBox;
			Rect boundingBoxB = line.BoundingBox;

			if (boundingBoxA.Overlaps(boundingBoxB))
			{
				List<Vector3> warningLocations;
				if (GeometryOperations.OverlapPolygonLine(poly, line, out warningLocations))
				{
					a_issueLocation = warningLocations[0];
					return true;
				}
			}

			a_issueLocation = Vector3.zero;
			return false;
		}

		private static bool InclusionCheckPolyToPoint(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			PolygonSubEntity poly = a_subEntityA as PolygonSubEntity;
			PointSubEntity point;
			if (poly == null)
			{
				if (a_subEntityB is PolygonSubEntity)
				{
					poly = (PolygonSubEntity)a_subEntityB;
					point = a_subEntityA as PointSubEntity;
				}
				else
				{
					Debug.LogError($"Trying to perform a poly to point check, but none of the arguments are polygons. Subent1: {FormatSubentityAndLayer(a_subEntityA, a_target.layer)}, subent2: {FormatSubentityAndLayer(a_subEntityB, a_targetPlanLayer.BaseLayer)}");
					a_issueLocation = Vector3.zero;
					return false;
				}
			}
			else
			{
				point = a_subEntityB as PointSubEntity;
			}
			if (point == null)
			{
				Debug.LogError($"Trying to perform a poly to point check, but none of the arguments are point. Subent1: {FormatSubentityAndLayer(a_subEntityA, a_target.layer)}, subent2: {FormatSubentityAndLayer(a_subEntityB, a_targetPlanLayer.BaseLayer)}");
				a_issueLocation = Vector3.zero;
				return false;
			}

			Vector3 pointCenter = point.BoundingBox.center;

			if (Util.PointCollidesWithPolygon(point.GetPosition(), poly.GetPoints(), poly.GetHoles(), ConstraintManager.Instance.ConstraintPointCollisionSize))
			{
				a_issueLocation = pointCenter;
				return true;
			}

			a_issueLocation = Vector3.zero;
			return false;
		}
		#endregion

		#region Inclusion Line
		private static bool InclusionCheckLineToLine(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			Rect boundingBoxA = a_subEntityA.BoundingBox;
			Rect boundingBoxB = a_subEntityB.BoundingBox;

			if (boundingBoxA.Overlaps(boundingBoxB))
			{
				LineStringSubEntity lineB = (LineStringSubEntity)a_subEntityB;
				List<Vector3> lineBPoints = lineB.GetPoints();

				if (Util.LineStringCollidesWithLineString(((LineStringSubEntity)a_subEntityA).GetPoints(), lineBPoints, out a_issueLocation))
				{
					return true;
				}
			}

			a_issueLocation = Vector3.zero;
			return false;
		}

		private static bool InclusionCheckLineToPoint(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			if (a_subEntityB is LineStringSubEntity)
			{
				SubEntity tmpSubentity = a_subEntityA;
				a_subEntityA = a_subEntityB;
				a_subEntityB = tmpSubentity;
			}

			Rect boundingBoxB = a_subEntityB.BoundingBox;

			if (Util.PointCollidesWithLineString(boundingBoxB.center, ((LineStringSubEntity)a_subEntityA).GetPoints(), ConstraintManager.Instance.ConstraintPointCollisionSize))
			{
				a_issueLocation = boundingBoxB.center;
				return true;
			}

			a_issueLocation = Vector3.zero;
			return false;
		}
		#endregion

		#region Inclusion Point
		private static bool InclusionCheckPointToPoint(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			Rect boundingBoxA = a_subEntityA.BoundingBox;
			Rect boundingBoxB = a_subEntityB.BoundingBox;

			if (Util.PointCollidesWithPoint(boundingBoxA.center, boundingBoxB.center, ConstraintManager.Instance.ConstraintPointCollisionSize))
			{
				a_issueLocation = boundingBoxA.center;
				return true;
			}

			a_issueLocation = Vector3.zero;
			return false;
		}
		#endregion

		#region Exclusion Poly (Incomplete)
		private static bool ExclusionCheckPolyToPoly(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			Rect boundingBoxA = a_subEntityA.BoundingBox;
			Rect boundingBoxB = a_subEntityB.BoundingBox;

			if (boundingBoxA.Overlaps(boundingBoxB))
			{
				PolygonSubEntity a = (PolygonSubEntity)a_subEntityA;
				PolygonSubEntity b = (PolygonSubEntity)a_subEntityB;

				List<Vector3> aPolygon = a.GetPoints();
				List<Vector3> bPolygon = b.GetPoints();

				foreach (Vector3 point in aPolygon)
				{
					if (!Util.PointInPolygon(point, bPolygon, null))
					{
						// point isnt in the other polygon
						a_issueLocation = point;
						return true;
					}
				}

				// overlapping
				a_issueLocation = Vector3.zero;
				return false;
			}

			// this is because the bounding boxes arent overlapping, so there is no inclusion
			a_issueLocation = boundingBoxA.center;
			return true;
		}
		#endregion

		#region Exclusion Line (TODO)
		#endregion

		#region Exclusion Point
		private static bool ExclusionCheckPointToPoint(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			Rect boundingBoxA = a_subEntityA.BoundingBox;
			Rect boundingBoxB = a_subEntityB.BoundingBox;

			a_issueLocation = boundingBoxA.center;

			if (Util.PointCollidesWithPoint(boundingBoxA.center, boundingBoxB.center, ConstraintManager.Instance.ConstraintPointCollisionSize))//if (boundingBoxA.Overlaps(subEntityB.BoundingBox))
			{
				return false;
			}

			return true;
		}

		private static bool ExclusionCheckPolyToPoint(SubEntity a_subEntityA, SubEntity a_subEntityB, ConstraintTarget a_target, PlanLayer a_targetPlanLayer, Plan a_checkForPlan, out Vector3 a_issueLocation)
		{
			PointSubEntity pointSubEntity = a_subEntityA as PointSubEntity;
			PolygonSubEntity polygonSubEntity = a_subEntityB as PolygonSubEntity;

			if (pointSubEntity == null || polygonSubEntity == null)
			{
				pointSubEntity = (PointSubEntity)a_subEntityB;
				polygonSubEntity = (PolygonSubEntity)a_subEntityA;
			}

			Vector3 pointCenter = pointSubEntity.BoundingBox.center;

			if (!Util.PointCollidesWithPolygon(pointCenter, polygonSubEntity.GetPoints(), polygonSubEntity.GetHoles(), ConstraintManager.Instance.ConstraintPointCollisionSize))
			{
				a_issueLocation = pointCenter;
				return true;
			}

			a_issueLocation = Vector3.zero;
			return false;
		}
		#endregion


		/// <summary>
		///  Takes the correct type of restriction check based on what the layers are
		/// </summary>
		/// <param name="a_layerA"></param>
		/// <param name="a_layerB"></param>
		/// <returns></returns>
		public static DoCheck PickCorrectInclusionCheckType(AbstractLayer a_layerA, AbstractLayer a_layerB)
		{
			if ((a_layerA is PolygonLayer && a_layerB is RasterLayer) || (a_layerA is RasterLayer && a_layerB is PolygonLayer))
			{
				return PolyToRaster;
			}
			if ((a_layerA is LineStringLayer && a_layerB is RasterLayer) || (a_layerA is RasterLayer && a_layerB is LineStringLayer))
			{
				return LineToRaster;
			}
			if ((a_layerA is PointLayer && a_layerB is RasterLayer) || (a_layerA is RasterLayer && a_layerB is PointLayer))
			{
				return PointToRaster;
			}
			if (a_layerA is PolygonLayer && a_layerB is PolygonLayer)
			{
				return InclusionCheckPolyToPoly;
			}
			if (a_layerA is PointLayer && a_layerB is PointLayer)
			{
				return InclusionCheckPointToPoint;
			}
			if (a_layerA is LineStringLayer && a_layerB is LineStringLayer)
			{
				return InclusionCheckLineToLine;
			}
			if ((a_layerA is PolygonLayer && a_layerB is PointLayer) || (a_layerA is PointLayer && a_layerB is PolygonLayer))
			{
				return InclusionCheckPolyToPoint;
			}
			if ((a_layerA is PolygonLayer && a_layerB is LineStringLayer) || (a_layerA is LineStringLayer && a_layerB is PolygonLayer))
			{
				return InclusionCheckPolyToLine;
			}
			if ((a_layerA is LineStringLayer && a_layerB is PointLayer) || (a_layerA is PointLayer && a_layerB is LineStringLayer))
			{
				return InclusionCheckLineToPoint;
			}
			return null;
		}

		public static DoCheck PickCorrectExclusionCheckType(AbstractLayer a_layerA, AbstractLayer a_layerB)
		{
			if ((a_layerA is PolygonLayer && a_layerB is RasterLayer) || (a_layerA is RasterLayer && a_layerB is PolygonLayer))
			{
				return PolyToRaster;
			}
			if ((a_layerA is LineStringLayer && a_layerB is RasterLayer) || (a_layerA is RasterLayer && a_layerB is LineStringLayer))
			{
				return LineToRaster;
			}
			if ((a_layerA is PointLayer && a_layerB is RasterLayer) || (a_layerA is RasterLayer && a_layerB is PointLayer))
			{
				return PointToRaster;
			}
			if ((a_layerA is LineStringLayer && a_layerB is PolygonLayer) || (a_layerA is PolygonLayer && a_layerB is LineStringLayer))
			{
				return ExclusionCheckLineToPoly;
			}
			if ((a_layerA is PointLayer && a_layerB is PolygonLayer) || (a_layerA is PolygonLayer && a_layerB is PointLayer))
			{
				return ExclusionCheckPolyToPoint;
			}
			if (a_layerA is PolygonLayer && a_layerB is PolygonLayer)
			{
				return ExclusionCheckPolyToPoly;
			}
			if (a_layerA is PointLayer && a_layerB is PointLayer)
			{
				return ExclusionCheckPointToPoint;
			}
			return null;
		}

		private static string FormatSubentityAndLayer(SubEntity a_subent, AbstractLayer a_layer)
		{
			if (a_subent == null)
				return $"null ({a_layer.ShortName})";
			return $"{a_subent.GetDatabaseID()} ({a_layer.ShortName})";
		}
	}
}
