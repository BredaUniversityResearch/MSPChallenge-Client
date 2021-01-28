using ClipperLib;
using Poly2Tri;
using System.Collections.Generic;
using UnityEngine;

namespace SoftwareRasterizerLib
{
	public static class PolygonRasterizer
	{
		public class Raster
		{
			public int m_rasterWidth;
			public int m_rasterHeight;
			public int m_scanlineMin;
			public int m_scanlineMax;
			public ScanlineMinMax[] m_scanlines;

			public void SetupScanlines()
			{
				if (m_scanlines == null || m_scanlines.Length != m_rasterHeight)
				{
					m_scanlines = new ScanlineMinMax[m_rasterHeight];
					for (int i = 0; i < m_scanlines.Length; ++i)
					{
						m_scanlines[i] = new ScanlineMinMax();
					}
					//Setup min & max so it flushes the entire scanline array on the first pass.
					m_scanlineMin = 0;
					m_scanlineMax = m_scanlines.Length - 1;
				}
			}
		}

		public class ScanlineMinMax
		{
			public float xMin;
			public float xMax;
		}

		//This will create a convex outline of the lineMesh given. Convex shapes will be filled up.
		public static void CreateRasterScanlineValues(Raster rasterResult, List<IntPoint> lineMesh, long clippingBoundsMinX, long clippingBoundsMaxX, long clippingBoundsMinY, long clippingBoundsMaxY)
		{
			rasterResult.SetupScanlines();

			ResetScanlines(rasterResult);
			List<PolygonPoint> screenSpaceVertices = TransformToRasterSpace(lineMesh, clippingBoundsMinX, clippingBoundsMaxX, clippingBoundsMinY, clippingBoundsMaxY, new Vector2(rasterResult.m_rasterWidth, rasterResult.m_rasterHeight));

			Polygon polygon = new Polygon(screenSpaceVertices);
			DTSweepContext tcx = new DTSweepContext();
			tcx.PrepareTriangulation(polygon);
			DTSweep.Triangulate(tcx);

			BuildScanlines(rasterResult, polygon.Triangles);
		}
		public static void DrawPolygons(RasterizerSurface targetSurface, int valueToSet, List<List<IntPoint>> meshes, long clippingBoundsMinX, long clippingBoundsMaxX, long clippingBoundsMinY, long clippingBoundsMaxY)
		{
			Raster raster = new Raster();
			raster.m_rasterWidth = targetSurface.Width;
			raster.m_rasterHeight = targetSurface.Height;
			raster.SetupScanlines();

			foreach (List<IntPoint> linePoints in meshes)
			{
				List<PolygonPoint> screenSpaceVertices = TransformToRasterSpace(linePoints, clippingBoundsMinX, clippingBoundsMaxX, clippingBoundsMinY, clippingBoundsMaxY, new Vector2(raster.m_rasterWidth, raster.m_rasterHeight));

				Polygon polygon = new Polygon(screenSpaceVertices);
				DTSweepContext tcx = new DTSweepContext();
				tcx.PrepareTriangulation(polygon);
				DTSweep.Triangulate(tcx);

				foreach (DelaunayTriangle triangle in polygon.Triangles)
				{
					ResetScanlines(raster);
					BuildScanlinesForTriangle(raster, triangle);
					RenderScanlines(raster, targetSurface.Values, valueToSet);
				}
			}
		}

		private static void BuildScanlines(Raster raster, IEnumerable<Poly2Tri.DelaunayTriangle> triangles)
		{
			foreach (Poly2Tri.DelaunayTriangle triangle in triangles)
			{
				BuildScanlinesForTriangle(raster, triangle);
			}
		}

		private static void BuildScanlinesForTriangle(Raster raster, Poly2Tri.DelaunayTriangle triangle)
		{
			for (int i = 0; i < 3; ++i)
			{
				Poly2Tri.TriangulationPoint point0 = triangle.Points[i];
				Poly2Tri.TriangulationPoint point1 = triangle.Points[(i + 1) % 3];

				Vector2 vertex0 = new Vector2((float)point0.X, (float)point0.Y);
				Vector2 vertex1 = new Vector2((float)point1.X, (float)point1.Y);

				//Swap vertices so we always go from top to bottom with vertex0 to vertex1
				if (vertex0.y > vertex1.y)
				{
					Vector2 temp = vertex0;
					vertex0 = vertex1;
					vertex1 = temp;
				}

				float deltaY = vertex1.y - vertex0.y;
				if (deltaY == 0.0f)
				{
					//No difference in Y so the line is perfectly horizontal, we can't render that so continue :)
					continue;
				}

				float rcpDeltaY = 1.0f / deltaY; //Multiplication is faster than divide, so precalculate the reciprocal so we can just multiply
				float deltaX = (vertex1.x - vertex0.x) * rcpDeltaY;

				float currentX = vertex0.x;

				//Apply subpixel correction.
				int intY0 = (int)vertex0.y + 1;
				int intY1 = (int)vertex1.y;

				float yCorrection = ((float)intY0 - vertex0.y);
				if (intY0 < 0)
				{
					yCorrection -= vertex1.y;
					intY0 = 0;
				}

				if (intY1 >= raster.m_rasterHeight)
				{
					intY1 = raster.m_rasterHeight - 1;
				}

				currentX += yCorrection * deltaX;

				//Update the touched scanline bounds.
				if (raster.m_scanlineMin > intY0)
				{
					raster.m_scanlineMin = intY0;
				}
				if (raster.m_scanlineMax < intY1)
				{
					raster.m_scanlineMax = intY1;
				}

				for (int y = intY0; y <= intY1; ++y)
				{
					if (raster.m_scanlines[y].xMin > currentX)
					{
						raster.m_scanlines[y].xMin = currentX;
					}
					if (raster.m_scanlines[y].xMax < currentX)
					{
						raster.m_scanlines[y].xMax = currentX;
					}
					currentX += deltaX;
				}
			}
		}

		/// <summary>
		/// Transforms all vertices to X and Y values from 0 .. width/height within the clipping bounds.
		/// </summary>
		/// <param name="vertices"></param>
		/// <param name="clippingMinX"></param>
		/// <param name="clippingMaxX"></param>
		/// <param name="clippingMinY"></param>
		/// <param name="clippingMaxY"></param>
		/// <returns></returns>
		private static List<Poly2Tri.PolygonPoint> TransformToRasterSpace(List<IntPoint> vertices, long clippingMinX, long clippingMaxX, long clippingMinY, long clippingMaxY, Vector2 rasterSize)
		{
			double deltaX = (double)(clippingMaxX - clippingMinX);
			double deltaY = (double)(clippingMaxY - clippingMinY);
			List<Poly2Tri.PolygonPoint> result = new List<Poly2Tri.PolygonPoint>();
			foreach (IntPoint vertex in vertices)
			{
				double transformedX = ((double)(vertex.X - clippingMinX) / deltaX);
				double transformedY = ((double)(vertex.Y - clippingMinY) / deltaY);
				result.Add(new Poly2Tri.PolygonPoint(transformedX * rasterSize.x, transformedY * rasterSize.y));
			}
			return result;
		}

		private static void ResetScanlines(Raster raster)
		{
			for (int i = raster.m_scanlineMin; i <= raster.m_scanlineMax; ++i)
			{
				raster.m_scanlines[i].xMin = 1e25f;
				raster.m_scanlines[i].xMax = -1.0f;
			}
			raster.m_scanlineMax = -1;
			raster.m_scanlineMin = raster.m_rasterHeight + 1;
		}

		private static void RenderScanlines(Raster raster, int[] outputPixels, int valueToSet)
		{
			for (int y = raster.m_scanlineMin; y <= raster.m_scanlineMax; ++y)
			{
				int outputY = y * raster.m_rasterWidth;//(raster.m_rasterHeight - y) * raster.m_rasterWidth;
				ScanlineMinMax scanline = raster.m_scanlines[y];

				for (int x = (int)scanline.xMin; x < (int)scanline.xMax; x++)
				{
					outputPixels[x + outputY] = valueToSet;
				}
			}
		}
	}
}
