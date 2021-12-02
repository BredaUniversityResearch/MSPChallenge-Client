using System;
using System.Collections.Generic;
using UnityEngine;
using ClipperLib;
using DG.Tweening;
using SoftwareRasterizerLib;

public class Rasterizer
{
    private const float intConversion = 1000000.0f;

    ///// <summary>
    ///// Creates a raster of the weighted input layers. 
    ///// Polygon vertices should be ordered depending on the coordinate system:
    /////     Bottom left zero: counter clockwise
    /////     Top left zero: clockwise
    ///// </summary>
    //public static double[,] Rasterize(List<WeightedLayer> layers, int rasterSize, Rect rasterBounds)
    //{
    //    double[,] output = new double[rasterSize,rasterSize];

    //    //Offset to the first square (for both axes)
    //    long voffset = (long)rasterBounds.yMin * longintConversion;
    //    long hoffset = (long)rasterBounds.xMin * longintConversion;
    //    long vsquaresize = (long)(rasterBounds.yMax - rasterBounds.yMin) / rasterSize * longintConversion;
    //    long hsquaresize = (long)(rasterBounds.xMax - rasterBounds.xMin) / rasterSize * longintConversion;
    //    double squaresurface = Math.Sqrt(vsquaresize * vsquaresize + hsquaresize * hsquaresize);

    //    foreach (WeightedLayer layer in layers)
    //    {
    //        foreach (List<Vector2> polygon in layer.polygons)
    //        {
    //            //Convert to int poly
    //            List<IntPoint> intpoly = VectorToIntPoint(polygon);

    //            //Get bounding box
    //            long left, right, top, bot;
    //            GetBounds(intpoly, out left, out right, out top, out bot);

    //            //Determine squares within bounding box
    //            long xmin = (left - hoffset) / hsquaresize;
    //            long xmax = (right - hoffset) / hsquaresize;
    //            long ymin = (bot - voffset) / vsquaresize;
    //            long ymax = (top - voffset) / vsquaresize;

    //            //Foreach overlapping square: calculate intersecting area
    //            for (long x = xmin; x < xmax; x++)
    //                for (long y = ymin; y < ymax; y++)
    //                {
    //                    Clipper clipper = new Clipper();

    //                    //Construct polygon paths (of poly and grid square)
    //                    clipper.AddPaths(new List<List<IntPoint>>() { intpoly }, PolyType.ptSubject, true);
    //                    clipper.AddPaths(new List<List<IntPoint>>() { GetSquarePoly(x * hsquaresize, (x + 1) * hsquaresize, y * vsquaresize, (y + 1) * vsquaresize) }, PolyType.ptClip, true);

    //                    //Calculate intersection
    //                    List<List<IntPoint>> intersection = new List<List<IntPoint>>();
    //                    clipper.Execute(ClipType.ctIntersection, intersection);

    //                    //Calculate part of square that is covered by the intersection and add it to the result
    //                    if (intersection.Count > 0 && intersection[0].Count > 0)
    //                        output[x, y] += GetPolygonArea(intersection) / squaresurface * layer.weight;
    //                }
    //        }
    //    }
    //    return output;
    //}

	public static PolygonRasterizer.Raster CreateScanlinesForPolygon(int rasterSizeX, int rasterSizeY, List<Vector3> polygonPoints, Rect rasterBounds)
	{
		long shiftedXMin = (long)(rasterBounds.xMin * intConversion);
		long shiftedYMin = (long)(rasterBounds.yMin * intConversion);
		long shiftedXMax = (long)(rasterBounds.xMax * intConversion);
		long shiftedYMax = (long)(rasterBounds.yMax * intConversion);

		PolygonRasterizer.Raster raster = new PolygonRasterizer.Raster();
		raster.m_rasterWidth = rasterSizeX;
		raster.m_rasterHeight = rasterSizeY;

		List<IntPoint> convertedPolygonPoints = VectorToIntPoint(polygonPoints);

		PolygonRasterizer.CreateRasterScanlineValues(raster, convertedPolygonPoints, shiftedXMin, shiftedXMax, shiftedYMin, shiftedYMax);

		return raster;
	}

	/// <summary>
	/// Rasterizes polygons to a flat 0 or valueToSet value. No subpixel correction.
	/// </summary>
	/// <param name="outputValues"></param>
	/// <param name="mesh"></param>
	/// <param name="valueToSet"></param>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <param name="rasterBounds"></param>
	public static void RasterizePolygonsFlat(RasterizerSurface targetSurface, List<Vector3> meshVertices, int valueToSet, Rect rasterBounds)
	{
		//Offset to the first square (for both axes)
		long shiftedXMin = (long)(rasterBounds.xMin * intConversion);
		long shiftedYMin = (long)(rasterBounds.yMin * intConversion);
		long shiftedXMax = (long)(rasterBounds.xMax * intConversion);
		long shiftedYMax = (long)(rasterBounds.yMax * intConversion);

		//Convert to int poly
		List<IntPoint> intpoly = VectorToIntPoint(meshVertices);

		Clipper clipper = new Clipper();

		//Construct polygon paths (of poly and grid square)
		clipper.AddPaths(new List<List<IntPoint>>() { intpoly }, PolyType.ptSubject, true);
		List<List<IntPoint>> clipPoly =
			new List<List<IntPoint>>() {GetSquarePoly(shiftedXMin, shiftedXMax, shiftedYMin, shiftedYMax)};
		clipper.AddPaths(clipPoly, PolyType.ptClip, true);

		//Calculate intersection
		List<List<IntPoint>> intersection = new List<List<IntPoint>>();

		clipper.Execute(ClipType.ctIntersection, intersection, PolyFillType.pftNonZero);

		//Calculate part of square that is covered by the intersection and add it to the result
		//if (intersection.Count > 0 && intersection[0].Count > 0)
		//	outputValues[x + ((height - 1 - y) * width)] = valueToSet;
		//Now rasterize the triangles. 
		PolygonRasterizer.DrawPolygons(targetSurface, valueToSet, intersection, shiftedXMin, shiftedXMax, shiftedYMin, shiftedYMax);
	}

	/// <summary>
	/// Gets the bounds of a polygon. Returned rect is non-rotated.
	/// </summary>
	public static void GetBounds(List<IntPoint> poly, out long left, out long right, out long top, out long bot)
    {
        left = 0; right = 0; top = 0; bot = 0;
        foreach (IntPoint v in poly)
        {
            if (v.X > right)
                right = v.X;
            else if (v.X < left)
                left = v.X;
            if (v.Y > top)
                top = v.Y;
            else if (v.Y < bot)
                bot = v.Y;
        }
    }

    private static List<IntPoint> VectorToIntPoint(List<Vector2> points)
    {
        List<IntPoint> verts = new List<IntPoint>();

        for (int i = 0; i < points.Count; i++)
        {
            verts.Add(new IntPoint(points[i].x * intConversion, points[i].y * intConversion));
        }

        return verts;
    }

	private static List<IntPoint> VectorToIntPoint(List<Vector3> points)
	{
		List<IntPoint> verts = new List<IntPoint>();

		for (int i = 0; i < points.Count; i++)
		{
			verts.Add(new IntPoint(points[i].x * intConversion, points[i].y * intConversion));
		}

		return verts;
	}

	private static List<IntPoint> GetSquarePoly(long xmin, long xmax, long ymin, long ymax)
    {
		return new List<IntPoint>()
		{
			new IntPoint(xmin, ymin),
			new IntPoint(xmin, ymax),
			new IntPoint(xmax, ymax),
			new IntPoint(xmax, ymin)
		};
    }

    private static double GetPolygonArea(List<List<IntPoint>> polygons)
    {
        double area = 0;
        foreach (List<IntPoint> polygon in polygons)
        {
            for (int i = 0; i < polygon.Count; ++i)
            {
                int j = (i + 1) % polygon.Count;
                area += polygon[i].Y * polygon[j].X - polygon[i].X * polygon[j].Y;
            }
        }
        return Math.Abs(area * 0.5);
    }
}

///// <summary>
///// A simple list of polygons with a weight.
///// Polygon vertices should be ordered depending on the coordinate system:
/////     Bottom left zero: counter clockwise
/////     Top left zero: clockwise
///// </summary>
//struct WeightedLayer
//{
//    public List<List<Vector2>> polygons;//@Elwin: you probably need to not use the unity vector here
//    public double weight;
//}

