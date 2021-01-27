using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelToPolygons : MonoBehaviour
{
    [Tooltip("The ID used to get the data from this model in the game")]
    public string ModelID;

    public Vector3 Translate = Vector3.zero;
    public Vector3 Scale = Vector3.one;

    public static Dictionary<string, List<PolygonData>> ModelPolygons = new Dictionary<string, List<PolygonData>>();

    public class PolygonData
    {
        public List<Vector3> Polygon;
        public List<List<Vector3>> Holes;

        public PolygonData(List<Vector3> polygon, List<List<Vector3>> holes)
        {
            Polygon = polygon;
            Holes = holes;
        }
    }

    void Start()
    {
        if (ModelID == "")
        {
            Debug.LogError("Please enter a Model ID in the Model To Polygons script", this);
            return;
        }

        if (ModelPolygons.ContainsKey(ModelID))
        {
            Debug.LogError("Duplicate Model ID: '" + ModelID + "'", this);
            return;
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        List<List<List<int>>> contourIndices = getContoursFromTriangles(mesh.triangles);

        Vector3[] vertices = mesh.vertices;
        float[] extents = new float[] { float.MaxValue, float.MinValue, float.MaxValue, float.MinValue };
        foreach (Vector3 vertex in vertices)
        {
            if (vertex.x < extents[0]) { extents[0] = vertex.x; }
            if (vertex.x > extents[1]) { extents[1] = vertex.x; }
            if (vertex.y < extents[2]) { extents[2] = vertex.y; }
            if (vertex.y > extents[3]) { extents[3] = vertex.y; }
        }
        Debug.Log("Extents for '" + ModelID + "': min = " + extents[0] + ", " + extents[2] + "; max = " + extents[1] + ", " + extents[3]);

        List<List<List<Vector3>>> contours = new List<List<List<Vector3>>>();
        foreach (List<List<int>> contourIndicesSet in contourIndices)
        {
            List<List<Vector3>> contourSet = new List<List<Vector3>>();
            foreach (List<int> singleContourIndices in contourIndicesSet)
            {
                List<Vector3> singleContour = new List<Vector3>();
                foreach (int index in singleContourIndices)
                {
                    singleContour.Add(Translate + Vector3.Scale(vertices[index], Scale));
                }
                contourSet.Add(singleContour);
            }
            contours.Add(contourSet);
        }

        List<PolygonData> polygons = new List<PolygonData>();
        foreach (List<List<Vector3>> contourSet in contours)
        {
            float largestArea = float.MinValue;
            List<Vector3> largestContour = null;

            foreach (List<Vector3> contour in contourSet)
            {
                float area = Util.GetPolygonArea(contour);
                if (area > largestArea)
                {
                    largestArea = area;
                    largestContour = contour;
                }
            }

            List<List<Vector3>> holes = new List<List<Vector3>>();
            foreach (List<Vector3> contour in contourSet)
            {
                if (contour != largestContour)
                {
                    holes.Add(contour);
                }
            }

            polygons.Add(new PolygonData(largestContour, holes));
        }

        ModelPolygons[ModelID] = polygons;

        Debug.Log("Loaded " + polygons.Count + " polygons for Model ID '" + ModelID + "'");
    }

    private List<List<List<int>>> getContoursFromTriangles(int[] triangles)
    {
        // find all edges

        Dictionary<int, List<int>> allEdges = new Dictionary<int, List<int>>();
        for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
        {
            for (int i = 0; i < 3; ++i)
            {
                int i0 = triangles[triangleIndex + i];
                int i1 = triangles[triangleIndex + (i + 1) % 3];

                if (!allEdges.ContainsKey(i0))
                {
                    allEdges.Add(i0, new List<int>());
                }
                allEdges[i0].Add(i1);
            }
        }

        // find contour edges

        Dictionary<int, List<int>> contourEdges = new Dictionary<int, List<int>>();
        foreach (var kvp in allEdges)
        {
            foreach (int neighbor in kvp.Value)
            {
                if (!allEdges[neighbor].Contains(kvp.Key))
                {
                    if (!contourEdges.ContainsKey(kvp.Key))
                    {
                        contourEdges.Add(kvp.Key, new List<int>());
                    }
                    contourEdges[kvp.Key].Add(neighbor);
                }
            }
        }

        // create contours

        HashSet<int> processedVertices = new HashSet<int>();
        List<List<int>> contours = new List<List<int>>();
        foreach (var kvp in contourEdges)
        {
            if (!processedVertices.Contains(kvp.Key))
            {
                contours.AddRange(createContours(kvp.Key, contourEdges, processedVertices));
            }
        }

        // create and return contour sets

        return getContourSets(contours, allEdges);
    }

    private List<List<List<int>>> getContourSets(List<List<int>> contours, Dictionary<int, List<int>> allEdges)
    {
        List<List<List<int>>> contourSets = new List<List<List<int>>>();
        Dictionary<List<int>, List<List<int>>> contourToContourSet = new Dictionary<List<int>, List<List<int>>>();
        Dictionary<int, List<int>> pointToContour = new Dictionary<int, List<int>>();

        foreach (List<int> contour in contours)
        {
            foreach (int point in contour)
            {
                pointToContour.Add(point, contour);
            }

            List<List<int>> newContourSet = new List<List<int>> { contour };
            contourToContourSet[contour] = newContourSet;
            contourSets.Add(newContourSet);
        }

        foreach (var kvp in allEdges)
        {
            int p0 = kvp.Key;
            List<List<int>> set0 = contourToContourSet[pointToContour[p0]];
            foreach (int p1 in kvp.Value)
            {
                List<List<int>> set1 = contourToContourSet[pointToContour[p1]];

                if (set0 != set1)
                {
                    set0.AddRange(set1);
                    foreach(List<int> contour in set1)
                    {
                        contourToContourSet[contour] = set0;
                    }
                    contourSets.Remove(set1);
                }
            }
        }

        return contourSets;
    }

    private List<List<int>> createContours(int start, Dictionary<int, List<int>> contourEdges, HashSet<int> processedVertices)
    {
        List<List<int>> finishedContours = new List<List<int>>();
        List<List<int>> unfinishedContours = new List<List<int>>();
        List<List<int>> currentContours = new List<List<int>>();

        currentContours.Add(new List<int> { start });
        int safetyCounter = 100000;
        while (safetyCounter-- > 0 && currentContours.Count > 0)
        {
            for (int i = currentContours.Count - 1; i >= 0; --i)
            {
                List<int> currentContour = currentContours[i];
                int lastContourPoint = currentContour[currentContour.Count - 1];
                if (processedVertices.Contains(lastContourPoint))
                {
                    if (lastContourPoint == currentContour[0])
                    {
                        currentContour.RemoveAt(currentContour.Count - 1);
                        finishedContours.Add(currentContour);
                    }
                    else
                    {
                        unfinishedContours.Add(currentContour);
                    }
                    currentContours.RemoveAt(i);
                }
                else
                {
                    List<int> neighbors = contourEdges[lastContourPoint];
                    if (neighbors.Count == 1)
                    {
                        currentContour.Add(neighbors[0]);
                    }
                    else
                    {
                        unfinishedContours.Add(currentContour);
                        currentContours.RemoveAt(i);

                        foreach (int neighbor in neighbors)
                        {
                            List<int> newContour = new List<int> { lastContourPoint, neighbor };
                            currentContours.Add(newContour);
                        }
                    }

                    processedVertices.Add(lastContourPoint);
                }
            }
        }

        if (safetyCounter == 0) { Debug.LogError("createContours: first while loop iteration limit exceeded!"); }

        if (unfinishedContours.Count > 0)
        {
            Debug.LogError("createContours: unfinished contours encountered! handling these isn't implemented because they never seemed to appear (until now)");
        }

        return finishedContours;
    }
}
