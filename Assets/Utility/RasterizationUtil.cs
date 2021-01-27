using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class RasterizationUtil
{
    public static void RasterizePolygon(List<Vector3> vertices, List<List<Vector3>> holes, int drawValue, int[,] raster, Rect rasterBounds)
    {
        Vector3 offset = rasterBounds.min;
        float xFactor = raster.GetLength(0) / rasterBounds.size.x;
        float yFactor = raster.GetLength(1) / rasterBounds.size.y;

        rasterizePolygonOutlines(vertices, holes, drawValue, raster, offset, xFactor, yFactor);
        //fillPolygon(vertices, holes, drawValue, raster, offset, xFactor, yFactor);
    }

    private static void rasterizePolygonOutlines(List<Vector3> vertices, List<List<Vector3>> holes, int drawValue, int[,] raster, Vector3 offset, float xFactor, float yFactor)
    {
        rasterizeLineLoop(vertices, drawValue, raster, offset, xFactor, yFactor);
        if (holes != null)
        {
            foreach (List<Vector3> hole in holes)
            {
                rasterizeLineLoop(hole, drawValue, raster, offset, xFactor, yFactor);
            }
        }
    }

    private static void rasterizeLineLoop(List<Vector3> vertices, int drawValue, int[,] raster, Vector3 offset, float xFactor, float yFactor)
    {
        for (int i = 0; i < vertices.Count; ++i)
        {
            int j = (i + 1) % vertices.Count;
            rasterizeLineSegment(vertices[i], vertices[j], drawValue, raster, offset, xFactor, yFactor);
        }
    }

    private static void rasterizeLineSegment(Vector3 a, Vector3 b, int drawValue, int[,] raster, Vector3 offset, float xFactor, float yFactor)
    {
        int w = raster.GetLength(0);
        int h = raster.GetLength(1);

        int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs((b.x - a.x) * xFactor), Mathf.Abs((b.y - a.y) * yFactor)));
        Vector3 increment = (b - a) / steps;
        for (int i = 0; i < steps; ++i)
        {
            Vector3 rasterPosition = (a + i * increment) - offset;
            rasterPosition.x *= xFactor;
            rasterPosition.y *= yFactor;

            raster[Mathf.Clamp(Mathf.RoundToInt(rasterPosition.x), 0, w - 1), Mathf.Clamp(Mathf.RoundToInt(rasterPosition.y), 0, h - 1)] = drawValue;
        }
    }

    private static void fillPolygon(List<Vector3> vertices, List<List<Vector3>> holes, int drawValue, int[,] raster, Vector3 offset, float xFactor, float yFactor)
    {
        int w = raster.GetLength(0);
        int h = raster.GetLength(1);

        float reverseXFactor = 1 / xFactor;
        float reverseYFactor = 1 / yFactor;

        bool[,] processed = new bool[w, h];
        for (int x = 0; x < w; ++x)
        {
            for (int y = 0; y < h; ++y)
            {
                if (!processed[x, y] && raster[x, y] != drawValue)
                {
                    Vector3 point = new Vector3(x * reverseXFactor, y * reverseYFactor, 0) + offset;
                    bool modifyRaster = Util.PointInPolygon(point, vertices, holes);
                    fill(x, y, drawValue, raster, processed, modifyRaster); 
                }
            }
        }
    }

    private struct int2 { public int x, y; public int2(int x, int y) { this.x = x; this.y = y; } };
    private static void fill(int x, int y, int drawValue, int[,] raster, bool[,] processed, bool modifyRaster = true)
    {
        int w = raster.GetLength(0);
        int h = raster.GetLength(1);

        List<int2> current = new List<int2> { new int2(x, y) };
        while (current.Count > 0)
        {
            List<int2> next = new List<int2>();

            foreach (int2 p in current)
            {
                if (modifyRaster) { raster[p.x, p.y] = drawValue; }

                if (p.x > 0 && !processed[p.x - 1, p.y] && raster[p.x - 1, p.y] != drawValue) { next.Add(new int2(p.x - 1, p.y)); processed[p.x - 1, p.y] = true; }
                if (p.y > 0 && !processed[p.x, p.y - 1] && raster[p.x, p.y - 1] != drawValue) { next.Add(new int2(p.x, p.y - 1)); processed[p.x, p.y - 1] = true; }

                if (p.x < w - 1 && !processed[p.x + 1, p.y] && raster[p.x + 1, p.y] != drawValue) { next.Add(new int2(p.x + 1, p.y)); processed[p.x + 1, p.y] = true; }
                if (p.y < h - 1 && !processed[p.x, p.y + 1] && raster[p.x, p.y + 1] != drawValue) { next.Add(new int2(p.x, p.y + 1)); processed[p.x, p.y + 1] = true; }
            }

            current = next;
        }
    }
}
