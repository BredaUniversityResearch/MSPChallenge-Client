using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
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
			ClipperLib.ClipperOffset co = new ClipperLib.ClipperOffset();
			if (isPolygon)
				co.AddPath(GeometryOperations.VectorToIntPoint(newPoints), ClipperLib.JoinType.jtSquare, ClipperLib.EndType.etClosedPolygon);
			else
				co.AddPath(GeometryOperations.VectorToIntPoint(newPoints), ClipperLib.JoinType.jtSquare, ClipperLib.EndType.etOpenSquare);
			List<List<ClipperLib.IntPoint>> csolution = new List<List<ClipperLib.IntPoint>>();
			co.Execute(ref csolution, (double)restrictionSize * GeometryOperations.intConverstion * 10d);
			if (csolution.Count == 0)
				return;
			polygon = GeometryOperations.IntPointToVector(csolution[0]);

			//Set mesh to result
			Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();
			poly.outside = polygon;
			Mesh mesh = Poly2Mesh.CreateMesh(poly);
			GetComponent<MeshFilter>().mesh = mesh;
		}
	}
}

