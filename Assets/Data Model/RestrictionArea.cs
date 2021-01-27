using ClipperLib;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RestrictionArea : MonoBehaviour
{
    public List<Vector3> polygon;

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
        transform.localPosition = new Vector3(0, 0, 0.1f);
        transform.localScale = Vector3.one;
    }

    public void SetPoints(List<Vector3> newPoints, float restrictionSize, bool isPolygon)
    {
        //Calculate offset
        ClipperOffset co = new ClipperOffset();
        if (isPolygon)
            co.AddPath(SetOperations.VectorToIntPoint(newPoints), JoinType.jtSquare, EndType.etClosedPolygon);
        else
            co.AddPath(SetOperations.VectorToIntPoint(newPoints), JoinType.jtSquare, EndType.etOpenSquare);
        List<List<IntPoint>> csolution = new List<List<IntPoint>>();
        co.Execute(ref csolution, (double)restrictionSize * SetOperations.intConverstion * 10d);
        if (csolution.Count == 0)
            return;
        polygon = SetOperations.IntPointToVector(csolution[0]);

        //Set mesh to result
        Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();
        poly.outside = polygon;
        Mesh mesh = Poly2Mesh.CreateMesh(poly);
        GetComponent<MeshFilter>().mesh = mesh;
    }
}

