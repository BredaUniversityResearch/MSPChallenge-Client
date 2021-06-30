using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Optimization
{
    //http://www.codeproject.com/Articles/18936/A-Csharp-Implementation-of-Douglas-Peucker-Line-Ap

    public static List<Vector3> DouglasPeuckerReduction(List<Vector3> points, float tolerance)
    {
        if (points == null || points.Count < 3) { return points; }

        int firstPoint = 0;
        int lastPoint = points.Count - 1;
        List<int> pointIndicesToKeep = new List<int>();

        //The first and the last point cannot be the same
        while (lastPoint >= 0 && points[firstPoint].Equals(points[lastPoint]))
        {
            lastPoint--;
        }

        if (lastPoint < 2) { return points; }

        //Add the first and last index to the keepers
        pointIndicesToKeep.Add(firstPoint);
        pointIndicesToKeep.Add(lastPoint);

        douglasPeuckerReduction(points, firstPoint, lastPoint, tolerance, ref pointIndicesToKeep);

        List<Vector3> returnPoints = new List<Vector3>();
        pointIndicesToKeep.Sort();
        foreach (int index in pointIndicesToKeep)
        {
            returnPoints.Add(points[index]);
        }

        return returnPoints;
    }

    private static void douglasPeuckerReduction(List<Vector3> points, int firstPoint, int lastPoint, float tolerance, ref List<int> pointIndicesToKeep)
    {
        float maxDistance = 0;
        int indexFarthest = 0;

        for (int index = firstPoint; index < lastPoint; index++)
        {
            float distance = perpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                indexFarthest = index;
            }
        }

        if (maxDistance > tolerance && indexFarthest != 0)
        {
            //Add the largest point that exceeds the tolerance
            pointIndicesToKeep.Add(indexFarthest);

            douglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndicesToKeep);
            douglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, ref pointIndicesToKeep);
        }
    }

    private static float perpendicularDistance(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        float area = Mathf.Abs(0.5f * (point1.x * point2.y + point2.x * point3.y + point3.x * point1.y -
                                       point2.x * point1.y - point3.x * point2.y - point1.x * point3.y));
        float bottom = Mathf.Sqrt(Mathf.Pow(point1.x - point2.x, 2) + Mathf.Pow(point1.y - point2.y, 2));
        float height = area / bottom * 2;

        return height;
    }
}
