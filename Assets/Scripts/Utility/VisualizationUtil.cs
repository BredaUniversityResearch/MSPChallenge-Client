using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class VisualizationUtil
{
	public static DataVisualizationSettings VisualizationSettings;

	private const float SELECT_MAX_DISTANCE = 10f;
	private const float MOUSE_MOVE_THRESHOLD = 2f;

	private static GameObject pointPrefab;
	private static GameObject pointRestrictionPrefab;
	private static GameObject areaRestrictionPrefab;
	private static GameObject linePrefab;
	private static GameObject lineIconPrefab;
	private static GameObject textPrefab;
	private static GameObject polygonPrefab;
	//private static GameObject rasterBathymetryPrefab;
	//private static GameObject rasterMELPrefab;
	private static Dictionary<string, GameObject> rasterPrefabs;

	public enum PointRenderMode { Default, Outline, AddWithPoint, AddWithoutPoint, RemoveWithPoint, RemoveWithoutPoint, MoveWithPoint, MoveWithoutPoint };
	private static Sprite pointOutline;
	private static Sprite pointAdd;
	private static Sprite pointRemove;
	private static Sprite pointMove;

	public static readonly Color DEFAULT_SELECTION_COLOR = new Color(255 / 255f, 147 / 255f, 30 / 255f); // orange
	public static readonly Color INVALID_SELECTION_COLOR = new Color(255 / 255f, 255 / 255f, 00 / 255f); // yellow
	public static Color SelectionColor = DEFAULT_SELECTION_COLOR;
	private static Color editColor = Color.white;

	public static float DisplayScale = 185f;
	public static float pointResolutionScale = 1f;
	public static float textResolutionScale = 1f;

    //private const float pointTextSize = 0.08f;

	static VisualizationUtil()
	{
		pointPrefab = Resources.Load<GameObject>("Point");
		pointRestrictionPrefab = Resources.Load<GameObject>("PointRestriction");
		linePrefab = Resources.Load<GameObject>("Line");
		lineIconPrefab = Resources.Load<GameObject>("LineIcon");
		textPrefab = Resources.Load<GameObject>("Text");
		polygonPrefab = Resources.Load<GameObject>("Polygon");
		areaRestrictionPrefab = (GameObject)Resources.Load("RestrictionPolygon");
		//areaRestrictionPrefab = Resources.Load<GameObject>("RestrictionPolygon");

		//rasterBathymetryPrefab = Resources.Load<GameObject>("RasterBathymetry");
		//rasterMELPrefab = Resources.Load<GameObject>("RasterMEL_ECO");

		rasterPrefabs = new Dictionary<string, GameObject>();
		Object[] rasterObjects = Resources.LoadAll("RasterPrefabs", typeof(GameObject));
		foreach (Object obj in rasterObjects)
			rasterPrefabs.Add(obj.name, obj as GameObject);

		pointOutline = Resources.Load<Sprite>("Point Outline");
		pointAdd = Resources.Load<Sprite>("Point Add");
		pointRemove = Resources.Load<Sprite>("Point Remove");
		pointMove = Resources.Load<Sprite>("Point Move");

		UpdateDisplayScale(Camera.main);
	}

	public static float GetSelectMaxDistance()
	{
		return SELECT_MAX_DISTANCE * Camera.main.orthographicSize * 2 / Screen.height;
	}

    public static float GetSelectMaxDistancePolygon()
    {
        return 0;
    }

    public static float GetMouseMoveThreshold()
	{
		return MOUSE_MOVE_THRESHOLD * Camera.main.orthographicSize * 2 / Screen.height;
	}

	public static void UpdateDisplayScale(Camera targetCamera)
	{
		DisplayScale = targetCamera.orthographicSize * 2.0f / Screen.height * 100.0f;
        pointResolutionScale = Screen.height / 1080f;
        textResolutionScale = Screen.height / (1080f * 20f);
        InterfaceCanvas.SetLineMaterialTiling(1f / (DisplayScale / 5f));
    }

	public static GameObject CreateText()
	{
		return GameObject.Instantiate<GameObject>(textPrefab);
	}

	//public static GameObject UpdateText(GameObject go, string text, Vector3 point, Color color, float size, float pointSize)
	//{
	//	TextMesh textMesh = go.GetComponent<TextMesh>();

	//	TextMesh[] outline = go.GetComponentsInChildren<TextMesh>();

	//	int fontSize = Mathf.Max(3, (int)size) + 20;
	//	textMesh.text = text;
	//	textMesh.color = color;
	//	textMesh.fontSize = fontSize;

	//	for (int i = 0; i < outline.Length; i++)
	//	{
	//		outline[i].text = text;
	//		outline[i].fontSize = fontSize;
	//	}

	//	point.z = -45;
	//	point.y += 0.1f * (go.transform.parent.localScale.y / pointSize);

	//	go.transform.localScale = (Vector3.one / pointSize) * pointTextSize;

	//	return go;
	//}

	public static GameObject CreateRasterGameObject(string prefabName)
	{
		return createRaster(prefabName);
	}

	public static GameObject CreatePointGameObject()
	{
		return CreatePoint();
	}

	public static void UpdatePointSubEntity(GameObject go, Vector3 position, SubEntityDrawSettings drawSettings, SubEntityPlanState planState, bool selected, bool hover)
	{
		if (go == null)
			return;

		Color c = drawSettings.PointColor;
		if (selected) { c = editColor; }
		else if (hover) { c = SelectionColor; }

		PointRenderMode pointRenderMode = PointRenderMode.Default;
		if (selected)
        {
            //Points with special sprites don't get outlines
            if(drawSettings.PointSprite == null)
                pointRenderMode = PointRenderMode.Outline;
        }
		else if (planState == SubEntityPlanState.Added) { pointRenderMode = PointRenderMode.AddWithPoint; }
		else if (planState == SubEntityPlanState.Removed) { pointRenderMode = PointRenderMode.RemoveWithPoint; }
		else if (planState == SubEntityPlanState.Moved) { pointRenderMode = PointRenderMode.MoveWithPoint; }

		UpdatePoint(go, position, c, drawSettings.PointSize, pointRenderMode, drawSettings.PointSprite);
		UpdatePointScale(go.transform.gameObject, drawSettings);
	}

	public static float UpdatePointScale(GameObject go, SubEntityDrawSettings drawSettings)
	{
		float newScale = drawSettings.PointSize * DisplayScale * pointResolutionScale;
		go.transform.localScale = new Vector3(newScale, newScale, 1f);
        return newScale;
	}

    public static GameObject CreatePoint()
	{
		return GameObject.Instantiate<GameObject>(pointPrefab);
	}

	public static GameObject CreateRestrictionPoint()
	{
		return GameObject.Instantiate<GameObject>(pointRestrictionPrefab);
	}

	public static RestrictionArea CreateRestrictionArea()
	{
		return GameObject.Instantiate<GameObject>(areaRestrictionPrefab).GetComponent<RestrictionArea>();
	}

	private static GameObject createRaster(string prefabName)
	{
		GameObject prefab;
		if (rasterPrefabs.TryGetValue(prefabName, out prefab))
			return GameObject.Instantiate<GameObject>(prefab);
		else
			Debug.LogError("No prefab with the name: \"" + prefabName + "\" exists in the RasterPrefabs folder. Using a default prefab.");
		return GameObject.Instantiate<GameObject>(rasterPrefabs.GetFirstValue());
	}

	public static void UpdatePoint(GameObject go, Vector3 position, Color color, float scale, PointRenderMode renderMode, Sprite pointSprite = null)
	{
		go.transform.gameObject.GetComponent<SpriteRenderer>().color = color;
		go.transform.localPosition = position + new Vector3(0, 0, -0.02f);
        float pointDisplayScale = scale * DisplayScale * pointResolutionScale;
        go.transform.localScale = new Vector3(pointDisplayScale, pointDisplayScale, 1f);


		go.transform.GetChild(0).gameObject.SetActive(renderMode != PointRenderMode.Default);
		SpriteRenderer childRenderer = go.transform.GetChild(0).GetComponent<SpriteRenderer>();

		SpriteRenderer pointRenderer = go.transform.gameObject.GetComponent<SpriteRenderer>();
		if (pointSprite != null)
		{
			pointRenderer.sprite = pointSprite;
        }

        if (renderMode != PointRenderMode.Outline)
        {
            //Constant childrenderer size
            float childScale = 1f / Mathf.Max(0.01f, scale);
            childRenderer.transform.localScale = new Vector3(childScale, childScale, 0);
            childRenderer.sortingOrder = 2;
			childRenderer.color = Color.white;
        }
        else
        {
            childRenderer.transform.localScale = new Vector3(1.2f, 1.2f, 0);
			childRenderer.sortingOrder = 0;
			childRenderer.color = SelectionColor;
        }

		switch (renderMode)
		{
		    case PointRenderMode.Outline:
			    pointRenderer.enabled = true;
			    childRenderer.sprite = pointOutline;
			    break;
		    case PointRenderMode.AddWithPoint:
			    pointRenderer.enabled = true;
			    childRenderer.sprite = pointAdd;
			    break;
		    case PointRenderMode.AddWithoutPoint:
			    pointRenderer.enabled = false;
			    childRenderer.sprite = pointAdd;
			    break;
		    case PointRenderMode.RemoveWithPoint:
			    pointRenderer.enabled = true;
			    childRenderer.sprite = pointRemove;
			    break;
		    case PointRenderMode.RemoveWithoutPoint:
			    pointRenderer.enabled = false;
			    childRenderer.sprite = pointRemove;
			    break;
		    case PointRenderMode.MoveWithPoint:
			    pointRenderer.enabled = true;
			    childRenderer.sprite = pointMove;
			    break;
		    case PointRenderMode.MoveWithoutPoint:
			    pointRenderer.enabled = false;
			    childRenderer.sprite = pointMove;
			    break;
		}
		
	}

	public static GameObject CreateLineStringGameObject()
	{
		return Object.Instantiate(linePrefab);
	}

	public static GameObject CreateLineStringIconObject(string iconName, Color iconColor)
	{
		GameObject go = Object.Instantiate(lineIconPrefab);
		SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
		renderer.sprite = Resources.Load<Sprite>(iconName);
		renderer.color = iconColor;
		if (renderer.sprite == null)
		{
			Debug.Log(string.Format("Could not load sprite with name \"{0}\"", iconName));
		}
		return go;
	}

	//public static void UpdateLineStringSubEntityScale(GameObject go, SubEntityDrawSettings drawSettings)
	//{
	//    int childCount = go.transform.childCount;

	//    for (int i = 0; i < childCount; ++i)
	//    {
	//        Transform child = go.transform.GetChild(i);
	//        if (child.tag == POINT_TAG) // point
	//        {
	//            float newScale = drawSettings.PointSize * DisplayScale;
	//            child.localScale = new Vector3(newScale, newScale, 1);
	//        }
	//        else // line segment
	//        {
	//            Vector3 localScale = child.localScale;
	//            child.localScale = new Vector3(localScale.x, DisplayScale, localScale.z);
	//            //linesToScale.Enqueue(child);
	//        }
	//    }
	//}

	//public static void updateLineStringPositionsAndColors(GameObject go, List<Vector3> positions, SubEntityDrawSettings drawSettings,
	//                                                       SubEntityPlanState planState, HashSet<int> selectedPoints, HashSet<int> hoverPoints)
	//{
	//    bool hasPointGameObjects = drawSettings.DisplayPoints || planState != SubEntityPlanState.NotInPlan;

	//    for (int i = 0; i < positions.Count; ++i)
	//    {
	//        bool hover = hoverPoints != null && hoverPoints.Contains(i);
	//        bool selected = selectedPoints != null && selectedPoints.Contains(i);

	//        if (hasPointGameObjects)
	//        {
	//            Color pointColor = hover ? SelectionColor : drawSettings.PointColor;
	//            pointColor = selected ? Color.white : pointColor;

	//            PointRenderMode pointRenderMode = PointRenderMode.Default;
	//            if (selected) { pointRenderMode = PointRenderMode.Outline; }
	//            else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Added) { pointRenderMode = PointRenderMode.AddWithPoint; }
	//            else if (planState == SubEntityPlanState.Added) { pointRenderMode = PointRenderMode.AddWithoutPoint; }
	//            else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Removed) { pointRenderMode = PointRenderMode.RemoveWithPoint; }
	//            else if (planState == SubEntityPlanState.Removed) { pointRenderMode = PointRenderMode.RemoveWithoutPoint; }
	//            else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Moved) { pointRenderMode = PointRenderMode.MoveWithPoint; }
	//            else if (planState == SubEntityPlanState.Moved) { pointRenderMode = PointRenderMode.MoveWithoutPoint; }

	//            int childIndex = i * 2;
	//            updatePoint(go.transform.GetChild(childIndex).gameObject, positions[i], pointColor, drawSettings.PointSize, pointRenderMode);
	//        }

	//        if (i < positions.Count - 1)
	//        {
	//            Color lineColor = (hover && hoverPoints.Contains(i + 1)) ? SelectionColor : drawSettings.LineColor;
	//            lineColor = (selected && selectedPoints.Contains(i + 1)) ? SelectionColor : lineColor;
	//            int childIndex = hasPointGameObjects ? i * 2 + 1 : i;
	//            updateLineSegment(go.transform.GetChild(childIndex).gameObject, positions[i], positions[i + 1], lineColor);
	//        }
	//    }
	//}

	public static GameObject CreateLineSegment()
	{
		GameObject go = GameObject.Instantiate<GameObject>(linePrefab);
		return go;
	}

	public static void updateLineSegment(GameObject go, Vector3 a, Vector3 b, Color color)
	{
		go.GetComponent<SpriteRenderer>().color = color;
		go.transform.position = (a + b) * 0.5f;
		go.transform.localPosition = new Vector3(go.transform.localPosition.x, go.transform.localPosition.y, -0.01f);
		go.transform.localScale = new Vector3((a - b).magnitude * 100, 1);
		go.transform.localRotation = Quaternion.identity;
		float angle = Mathf.Atan2((float)(a.y - b.y), (float)(a.x - b.x)) * 180 / Mathf.PI;
		go.transform.Rotate(Vector3.forward, angle, Space.Self);
	}

	public static GameObject CreatePolygonGameObject()
	{
		return GameObject.Instantiate<GameObject>(polygonPrefab);
	}

	public static IEnumerator UpdateScales()
	{
		//while (true)
		//{
		//    int lineCount = linesToScale.Count;
		//    if (lineCount > 0)
		//    {
		//        int max = (int)Mathf.Min(3000, lineCount );

		//        for (int i = 0; i < max; i++)
		//        {
		//            Transform trans = linesToScale.Dequeue();
		//            if (trans != null)
		//            {
		//                Vector3 localScale = trans.localScale;
		//                trans.localScale = new Vector3(localScale.x, DisplayScale, localScale.z);
		//            }
		//        }
		//    }
		//    yield return null;
		//}
		yield return null;
	}

	public class TriangulationException : System.Exception
	{
		public TriangulationException()
		{
		}

		public TriangulationException(string message)
		: base(message)
		{
		}

		public TriangulationException(string message, System.Exception inner)
		: base(message, inner)
		{
		}
	}

	public static Mesh CreatePolygon(List<Vector3> vertices, List<List<Vector3>> holes, Vector2 patternRandomOffset, bool innerGlow, Rect innerGlowTextureBounds)
	{
		if (vertices.Count < 3) { return null; }

		//vertices = Optimization.DouglasPeuckerReduction(vertices, 0.01f);

		Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();

		poly.outside = vertices;

		if (holes != null && holes.Count > 0)
		{
			poly.holes = holes;
		}

		Mesh mesh = Poly2Mesh.CreateMesh(poly);

		if (mesh != null && mesh.vertexCount > 0)
		{
			Vector2[] uvs = new Vector2[mesh.vertexCount];
			for (int i = 0; i < uvs.Length; i++)
			{
				uvs[i] = patternRandomOffset;
			}

			if (!innerGlow)
			{
				mesh.uv = uvs;
			}
			else
			{
				mesh.uv2 = uvs;

				uvs = mesh.uv;

				Vector2 offset = innerGlowTextureBounds.min;
				float xFactor = 1f / innerGlowTextureBounds.size.x;
				float yFactor = 1f / innerGlowTextureBounds.size.y;

				for (int i = 0; i < uvs.Length; i++)
				{
					uvs[i] -= offset;
					uvs[i] = new Vector2(uvs[i].x * xFactor, uvs[i].y * yFactor);
				}

				mesh.uv = uvs;
			}
		}

		return mesh;
	}

	public static PointRenderMode GetPointRenderMode(SubEntityDrawSettings drawSettings, SubEntityPlanState planState, bool selected)
	{
		PointRenderMode pointRenderMode = PointRenderMode.Default;
		if (selected)
		{
			pointRenderMode = PointRenderMode.Outline;
		}
		else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Added)
		{
			pointRenderMode = PointRenderMode.AddWithPoint;
		}
		else if (planState == SubEntityPlanState.Added)
		{
			pointRenderMode = PointRenderMode.AddWithPoint;
		}
		else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Removed)
		{
			pointRenderMode = PointRenderMode.RemoveWithPoint;
		}
		else if (planState == SubEntityPlanState.Removed)
		{
			pointRenderMode = PointRenderMode.RemoveWithoutPoint;
		}
		else if (drawSettings.DisplayPoints && planState == SubEntityPlanState.Moved)
		{
			pointRenderMode = PointRenderMode.MoveWithPoint;
		}
		else if (planState == SubEntityPlanState.Moved)
		{
			pointRenderMode = PointRenderMode.MoveWithoutPoint;
		}
		return pointRenderMode;
	}

	public static void DestroyChildren(GameObject subject)
	{
		foreach (Transform child in subject.transform)
		{
			GameObject.Destroy(child.gameObject);
		}
		subject.transform.DetachChildren();
	}
}
