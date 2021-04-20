using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Object = UnityEngine.Object;

public class PolygonSubEntity : SubEntity
{
	private const float LINE_FORWARD_EXTRUDE_AMOUNT = -0.01f;

	protected List<Vector3> polygon;
	protected List<List<Vector3>> holes;

	private bool surfaceAreaNeeded; //Was the surfacearea requested before?
	private float surfaceAreaSqrKm; 
	public float SurfaceAreaSqrKm
	{
		get
		{
			if (!surfaceAreaNeeded)
			{
				surfaceAreaNeeded = true;
				surfaceAreaSqrKm = InterfaceCanvas.Instance.mapScale.GetRealWorldPolygonAreaInSquareKm(polygon, holes);
			}
			return surfaceAreaSqrKm;
		}
	}//Surface size in KM2

	public HashSet<int> InvalidPoints = null;
	public bool firstToLastInvalid = false;//is the first to last point valid
	public RestrictionArea restrictionArea;

	private GameObject outline = null;

	protected class PolygonLOD
	{
		public List<Vector3> Polygon { get; private set; }
		public List<List<Vector3>> Holes { get; private set; }
		public LODSettings Settings { get; private set; }
		public GameObject GameObject { get; private set; }
		public GameObject PointContainerObject { get; private set; }

        public PolygonLOD(List<Vector3> polygon, List<List<Vector3>> holes, Transform targetParent, LODSettings settings)
        {

            GameObject = VisualizationUtil.CreatePolygonGameObject();
            GameObject.transform.SetParent(targetParent, false);
            GameObject.SetActive(false);
            PointContainerObject = new GameObject("PointContainer");
            PointContainerObject.transform.SetParent(GameObject.transform, false);

            Settings = settings;
            SetMeshData(polygon, holes);
        }

		public void SetMeshData(List<Vector3> polygonVertices, List<List<Vector3>> holeVertices)
		{
			Polygon = polygonVertices;
			Holes = holeVertices;
		}

		public Vector3 GetPointPosition(int point)
		{
			if (point < Polygon.Count)
			{
				return Polygon[point];
			}
			else
			{
				point -= Polygon.Count;
				for (int i = 0; i < Holes.Count; ++i)
				{
					if (point < Holes[i].Count)
					{
						return Holes[i][point];
					}
					point -= Holes[i].Count;
				}
			}
			throw new System.Exception("Error in GetPointPosition(): point " + point + " not found");
		}
	}

    protected List<PolygonLOD> lods = new List<PolygonLOD>();
	protected int displayedLOD = -1;
	private bool lodLockedAtZero = false;
	private bool meshIsDirty = false;

	public PolygonSubEntity(Entity entity, int persistentID = -1) : base(entity, -1, persistentID)
	{
		polygon = new List<Vector3>();
		holes = null;
	}

	public PolygonSubEntity(Entity entity, SubEntityObject geometry, int databaseID)
		: base(entity, databaseID, geometry.persistent)
	{
		polygon = GetPolygonFromGeometryObject(geometry);
		holes = null;

		if (geometry.subtractive != null)
		{
			holes = new List<List<Vector3>>();
			foreach (GeometryObject subtractiveGeo in geometry.subtractive)
			{
				holes.Add(GetPolygonFromGeometryObject(subtractiveGeo));
			}
		}

		mspID = geometry.mspid;
		restrictionNeedsUpdate = true;

		Initialise();
	}

	public virtual void Initialise()
	{
		UpdateBoundingBox();
	}

	public override void RemoveGameObject()
	{
		base.RemoveGameObject();
		Object.Destroy(outline);
		outline = null;

		lods.Clear();
		displayedLOD = -1;
	}

	public override void SetDataToObject(SubEntityObject subEntityObject)
	{
		polygon = GetPolygonFromGeometryObject(subEntityObject);
		holes = null;

		if (subEntityObject.subtractive != null)
		{
			holes = new List<List<Vector3>>();
			foreach (GeometryObject subtractiveGeo in subEntityObject.subtractive)
				holes.Add(GetPolygonFromGeometryObject(subtractiveGeo));
		}

		UpdateBoundingBox();
	}

	protected override void UpdateBoundingBox()
	{
		updateBoundingBox();
	}

	private void updateBoundingBox()
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = Vector3.one * float.MinValue;
		foreach (Vector3 point in polygon)
		{
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}

		BoundingBox = new Rect(min, max - min);
		//Update surfface area if it was required before (avoids calculating on load)
		if(surfaceAreaNeeded)
			surfaceAreaSqrKm = InterfaceCanvas.Instance.mapScale.GetRealWorldPolygonAreaInSquareKm(polygon, holes);
	}

	private void RebuildLods()
	{
		lods.Clear();

		List<LODSettings> lodSettingsList = VisualizationUtil.VisualizationSettings.LODs;
		for (int i = 0; i < lodSettingsList.Count; ++i)
		{
            lods.Add(new PolygonLOD(null, null, gameObject.transform, lodSettingsList[i]));
		}
		UpdateLods();
	}

	private void UpdateLods()
	{
		for (int i = 0; i < lods.Count; ++i)
		{
			UpdateLod(lods[i]);
		}

		UpdateGameObjectForEveryLOD();
	}

	private void UpdateLod(PolygonLOD targetLod)
	{
		List<Vector3> lodPolygon = Optimization.DouglasPeuckerReduction(polygon, targetLod.Settings.SimplificationTolerance);

		//if (lodPolygon.Count < 3 || Util.GetPolygonArea(lodPolygon) < targetLod.Settings.MinPolygonArea)
		//{
		//	targetLod.SetMeshData(null, null);
		//}
		//else
		//{
			List<List<Vector3>> lodHoles = null;
			if (holes != null)
			{
				lodHoles = new List<List<Vector3>>();
				foreach (List<Vector3> hole in holes)
				{
					List<Vector3> lodHole = Optimization.DouglasPeuckerReduction(hole, targetLod.Settings.SimplificationTolerance);
					if (lodHole.Count >= 3)
					{
						lodHoles.Add(lodHole);
					}
				}
			}
			targetLod.SetMeshData(lodPolygon, lodHoles);
		//}
	}

	public override void UpdateGameObjectForEveryLOD()
	{
		//foreach (PolygonLOD lod in lods)
		//{
		//	if (lod.GameObject != null)
		//	{
		//		//PolygonLayer layer = Entity.Layer as PolygonLayer;
		//		RebuildPolygon(lod);
		//		//UpdatePolygonSubEntity(lod.GameObject, lod.Polygon, lod.Holes, Entity.patternRandomOffset, drawSettings, planState, layer.InnerGlowTexture, layer.InnerGlowBounds, null, null);
		//	}
		//}
	}

	public void PerformValidityCheck(bool polygonIsBeingCreated, bool ignoreLastPoint = false)
	{
		Vector3 tmp = Vector3.zero;
		if (ignoreLastPoint)
		{
			tmp = polygon[polygon.Count - 1];
			polygon.RemoveAt(polygon.Count - 1);
		}

		if (polygonIsBeingCreated)
		{
			if (polygon.Count < 4)
			{
				InvalidPoints = null;
				firstToLastInvalid = false;
			}
			else
			{
				InvalidPoints = Util.LineStringSelfIntersects(polygon);
				firstToLastInvalid = Util.LineSegmentAndLineIntersect(polygon, polygon.Count-1, GetPointPosition(0), GetPointPosition(polygon.Count - 1));
			}
		}
		else
		{
			InvalidPoints = Util.PolygonSelfIntersects(polygon, holes);
		}

		if (ignoreLastPoint)
		{
			polygon.Add(tmp);
		}
	}

	//Validity check used during polygon creation
	public void PerformNewSegmentValidityCheck(bool completing)
	{
		InvalidPoints = null;
		firstToLastInvalid = false;
		if (polygon.Count < 4)		
			return;
		
		int points = polygon.Count;
		if (completing)
		{
			if (Util.LineSegmentAndLineIntersect(polygon, points - 2, polygon[points - 2], polygon[0]))
				InvalidPoints = new HashSet<int>() { 0, points - 1, points - 2 };
		}
		else
		{
			if (Util.LineSegmentAndLineIntersect(polygon, points - 2, polygon[points - 2], polygon[points - 1]))
				InvalidPoints = new HashSet<int>() { points - 1, points - 2 };
			firstToLastInvalid = Util.LineSegmentAndLineIntersect(polygon, points - 2, polygon[points - 1], polygon[0]);
		}
	}

	public void TryFixingSelfIntersectionsWithIncreasingOffsets(float initialFixOffset, float maxFixOffset)
	{
		float fixOffset = initialFixOffset;

		if (fixOffset <= 0) { Debug.LogError("invalid initial fix offset in PolygonSubEntity.TryFixingSelfIntersectionsWithIncreasingOffsets()!"); return; }
		while (fixOffset < maxFixOffset && InvalidPoints != null)
		{
			TryFixingSelfIntersections(fixOffset);
			fixOffset *= 2;
		}
		TryFixingSelfIntersections(maxFixOffset);
	}

	public void TryFixingSelfIntersections(float fixOffset)
	{
		if (InvalidPoints != null)
		{
			InvalidPoints = Util.TryFixingSelfIntersections(polygon, holes, InvalidPoints, fixOffset);

			// if there are any invalid points left, run the algorithm again (the second time should be faster and it may solve some remaining problems)
			if (InvalidPoints != null)
			{
				InvalidPoints = Util.TryFixingSelfIntersections(polygon, holes, InvalidPoints, fixOffset);
			}

			UpdateBoundingBox();
		}
	}

	public void AddPoint(Vector3 point)
	{
		polygon.Add(point);
		UpdateBoundingBox();
	}

	public void AddHole(List<Vector3> vertices)
	{
		if (holes == null)
		{
			holes = new List<List<Vector3>>();
		}
		holes.Add(vertices);

		meshIsDirty = true;
	}

	public int AddPointBetween(Vector3 newPoint, int pointA, int pointB)
	{
		if (pointB < polygon.Count)
		{
			polygon.Insert(pointB, newPoint);
		}
		else
		{
			int offset = polygon.Count;
			foreach (List<Vector3> hole in holes)
			{
				if (pointB < hole.Count + offset)
				{
					hole.Insert(pointB - offset, newPoint);
					break;
				}
				offset += hole.Count;
			}
		}

		UpdateBoundingBox();
		meshIsDirty = true;
		return pointB;
	}

	public string HolesToJSON(int index)
	{
		return JsonConvert.SerializeObject(GetLayerObject().subtractive[index].geometry);
	}

	public int GetPointAt(Vector3 position, out float closestDistanceSquared, bool ignoreLastPoint = false)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		int closestPoint = -1;
		closestDistanceSquared = float.MaxValue;

		int end = ignoreLastPoint ? polygon.Count - 1 : polygon.Count;
		for (int i = 0; i < end; ++i)
		{
			float distanceSquared = (polygon[i] - position).sqrMagnitude;
			if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
			{
				closestPoint = i;
				closestDistanceSquared = distanceSquared;
			}
		}

		int pointIndexOffset = polygon.Count;
		int holeCount = GetHoleCount();
		for (int j = 0; j < holeCount; ++j)
		{
			for (int i = 0; i < holes[j].Count; ++i)
			{
				float distanceSquared = (holes[j][i] - position).sqrMagnitude;
				if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
				{
					closestPoint = i + pointIndexOffset;
					closestDistanceSquared = distanceSquared;
				}
			}
			pointIndexOffset += holes[j].Count;
		}

		return closestPoint;
	}

	public void GetLineAt(Vector3 position, out int lineA, out int lineB, out float closestDistanceSquared)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		lineA = -1;
		lineB = -1;
		closestDistanceSquared = float.MaxValue;

		for (int i = 0; i < polygon.Count; ++i)
		{
			float distanceSquared = Util.GetSquaredDistanceToLine(position, polygon[i], polygon[(i + 1) % polygon.Count]);
			if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
			{
				lineA = i;
				lineB = (i + 1) % polygon.Count;
				closestDistanceSquared = distanceSquared;
			}
		}

		int pointIndexOffset = polygon.Count;
		int holeCount = GetHoleCount();
		for (int j = 0; j < holeCount; ++j)
		{
			for (int i = 0; i < holes[j].Count; ++i)
			{
				float distanceSquared = Util.GetSquaredDistanceToLine(position, holes[j][i], holes[j][(i + 1) % holes[j].Count]);
				//(holes[j][i] - position).sqrMagnitude;
				if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
				{
					lineA = i + pointIndexOffset;
					lineB = (i + 1) % holes[j].Count + pointIndexOffset;
					closestDistanceSquared = distanceSquared;
				}
			}
			pointIndexOffset += holes[j].Count;
		}
	}

	public HashSet<int> GetPointsInBox(Vector3 min, Vector3 max)
	{
		HashSet<int> result = new HashSet<int>();

		for (int i = 0; i < polygon.Count; ++i)
		{
			Vector3 position = polygon[i];
			if (position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y)
			{
				result.Add(i);
			}
		}

		int pointIndexOffset = polygon.Count;
		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				for (int i = 0; i < hole.Count; ++i)
				{
					Vector3 position = hole[i];
					if (position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y)
					{
						result.Add(i + pointIndexOffset);
					}
				}
				pointIndexOffset += hole.Count;
			}
		}

		return result.Count > 0 ? result : null;
	}

	public int GetPolygonPointCount()
	{
		return polygon.Count;
	}

	public int GetHoleCount()
	{
		return (holes == null) ? 0 : holes.Count;
	}

	public int GetHolePointCount(int holeIndex)
	{
		return holes[holeIndex].Count;
	}

	public int GetTotalPointCount()
	{
		int result = polygon.Count;
		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				result += hole.Count;
			}
		}
		return result;
	}

	public Vector3 GetPointPosition(int point)
	{
		if (point < polygon.Count)
		{
			return polygon[point];
		}
		else
		{
			point -= polygon.Count;
			for (int i = 0; i < holes.Count; ++i)
			{
				if (point < holes[i].Count)
				{
					return holes[i][point];
				}
				point -= holes[i].Count;
			}
		}
		throw new System.Exception("Error in GetPointPosition(): point " + point + " not found");
	}

	public void SetPointPosition(int point, Vector3 position, bool UpdateBoundingBoxAndLODs = true)
	{
		if (point < polygon.Count)
		{
			polygon[point] = position;
			if (UpdateBoundingBoxAndLODs)
			{
				UpdateBoundingBox();
			}
			meshIsDirty = true;
			return;
		}
		else
		{
			point -= polygon.Count;
			for (int i = 0; i < holes.Count; ++i)
			{
				if (point < holes[i].Count)
				{
					// bounding box doesn't change when a hole is updated
					holes[i][point] = position;
					meshIsDirty = true;
					return;
				}
				point -= holes[i].Count;
			}
		}
		throw new System.Exception("Error in SetPointPosition(): point " + point + " not found");
	}

	public void RemovePoints(HashSet<int> pointIndices)
	{
		List<int> pointList = new List<int>(pointIndices);
		pointList.Sort();
		pointList.Reverse();

		foreach (int pointIndex in pointList)
		{
			removePoint(pointIndex);
		}

		UpdateBoundingBox();
	}

	private void removePoint(int point)
	{
		if (point < polygon.Count)
		{
			polygon.RemoveAt(point);
			meshIsDirty = true;
			return;
		}
		else
		{
			point -= polygon.Count;
			for (int i = 0; i < holes.Count; ++i)
			{
				if (point < holes[i].Count)
				{
					holes[i].RemoveAt(point);
					meshIsDirty = true;
					return;
				}
				point -= holes[i].Count;
			}
		}
		throw new System.Exception("Error in removePoint(): point " + point + " not found");
	}

	public void RemoveHole(int holeIndex)
	{
		holes.RemoveAt(holeIndex);
		meshIsDirty = true;
	}

	public void RemoveAllHoles()
	{
		holes = null;
		meshIsDirty = true;
	}

	private List<Vector3> GetPolygonFromGeometryObject(SubEntityObject geo)
	{
		int total = geo.geometry.Count;
		List<Vector3> polygon = new List<Vector3>(total);

		for (int i = 0; i < total; i++)
		{
			float x0 = geo.geometry[i][0] / Main.SCALE;
			float y0 = geo.geometry[i][1] / Main.SCALE;
			float x1 = geo.geometry[(i + 1) % total][0] / Main.SCALE;
			float y1 = geo.geometry[(i + 1) % total][1] / Main.SCALE;

			//Calculate "reasonable" epsilon. Considering we have 7 digits of precision, we take a '1' value on the sixth digit as the maximum distance.
			//1000 = 0.001 (log10(1000) = 3, 6-3 = 3, 10^3 = 1000, 1/1000 = 0.001)
			//10000 = 0.01 (log10(10000) = 4, 6-2 = 2, 10^2 = 100, 1/100 = 0.01)
			float epsilon = 1.0f / Mathf.Pow(10, 6 - Mathf.Floor(Mathf.Log10(x0)));

			Vector2 v1 = new Vector2(x1, y1);

			if (Math.Abs(x0 - x1) > epsilon || Math.Abs(y0 - y1) > epsilon)
			{
				polygon.Add(v1);
			}
		}

		return polygon;
	}

	private List<Vector3> GetPolygonFromGeometryObject(GeometryObject geo)
	{
		int total = geo.geometry.Count;
		List<Vector3> polygon = new List<Vector3>(total);

		for (int i = 0; i < total; i++)
		{
			float x0 = geo.geometry[i][0] / Main.SCALE;
			float y0 = geo.geometry[i][1] / Main.SCALE;
			//float x1 = geo.geometry[(i + 1) % total][0] / Main.SCALE;
			//float y1 = geo.geometry[(i + 1) % total][1] / Main.SCALE;

			Vector2 v1 = new Vector2(x0, y0);

			//if (x0 != x1 || y0 != y1)
			{
				polygon.Add(v1);
			}
		}

		return polygon;
	}

	private GeometryObject GetGeometryObjectFromPolygon(List<Vector3> poly)
	{
		GeometryObject geo = new GeometryObject();
		geo.geometry = new List<List<float>>();

		for (int i = 0; i < poly.Count; i++)
		{
			List<float> vertex = new List<float>();

			vertex.Add(poly[i].x * Main.SCALE);
			vertex.Add(poly[i].y * Main.SCALE);

			geo.geometry.Add(vertex);
		}

		return geo;
	}

	public void Simplify(float tolerance)
	{
		List<Vector3> simplePoly = Optimization.DouglasPeuckerReduction(polygon, tolerance);

		if (simplePoly.Count >= 3)
		{
			polygon = simplePoly;
		}

		if (holes != null)
		{
			for (int i = 0; i < holes.Count; ++i)
			{
				List<Vector3> simpleHole = Optimization.DouglasPeuckerReduction(holes[i], tolerance);
				if (simpleHole.Count >= 3)
				{
					holes[i] = simpleHole;
				}
			}
		}

		PerformValidityCheck(false);
		//TryFixingSelfIntersectionsWithIncreasingOffsets(0.01f, tolerance);

		UpdateBoundingBox();
	}

	public override void UpdateGeometry(GeometryObject geo)
	{
		polygon = GetPolygonFromGeometryObject(geo);
		UpdateBoundingBox();
	}

	public override void SetOrderBasedOnType()
	{
		calculateOrderBasedOnType();
		gameObject.transform.localPosition = new Vector3(0, 0, Order);
	}

	public override void DrawGameObject(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null)
	{
		if (gameObject != null)
		{
			//Debug.LogError("Attempting to draw entity with an existing GameObject.");
			return;
		}
		if (GetTotalPointCount() >= 65000)
		{
			Debug.LogError(" GetTotalPointCount() >= 65000 for " + Entity.Layer.FileName + " - " + GetDatabaseID());
		}
		drawSettings = Entity.EntityTypes[0].DrawSettings;

        if (!Entity.Layer.Optimized)
        {
            gameObject = new GameObject(databaseID != -1 ? "" + databaseID : "<undefined database ID>");
            gameObject.transform.SetParent(parent);

            PolygonLayer layer = (PolygonLayer)Entity.Layer;
            if (drawSettings.InnerGlowEnabled)
            {
                layer.UpdateInnerGlow(drawSettings.InnerGlowRadius, drawSettings.InnerGlowIterations, drawSettings.InnerGlowMultiplier, drawSettings.InnerGlowPixelSize);
            }

            if (Entity.Layer.textInfo != null)
            {
                CreateTextMesh(gameObject.transform, Vector3.zero);
            }

            RebuildLods();

            RedrawGameObject(drawMode, selectedPoints, hoverPoints);

            SetOrderBasedOnType();
        }
	}

	public override void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
	{
		base.RedrawGameObject(drawMode, selectedPoints, hoverPoints, updatePlanState);

		if (gameObject == null)
			return;

		// Bathymetry and Countries/Councils are not selectable, and will not change drawmode
        if (drawMode == SubEntityDrawMode.Default && LayerManager.IsReferenceLayer(Entity.Layer) && Entity.Layer.Selectable)
        {
			drawMode = SubEntityDrawMode.PlanReference;			
		}

		if (InvalidPoints != null /*&& Main.InEditMode && Main.CurrentlyEditingBaseLayer == Entity.Layer*/)
		{
			if (drawMode == SubEntityDrawMode.BeingCreated)
			{
				drawMode = SubEntityDrawMode.BeingCreatedInvalid;
			}
			else if (drawMode == SubEntityDrawMode.Selected)
			{
				drawMode = SubEntityDrawMode.Invalid;
			}
			if (hoverPoints != null)
			{
				hoverPoints.UnionWith(InvalidPoints);
			}
			else
			{
				hoverPoints = InvalidPoints;
			}

			VisualizationUtil.SelectionColor = VisualizationUtil.INVALID_SELECTION_COLOR;
		}

		SnappingToThisEnabled = IsSnapToDrawMode(drawMode);

		SubEntityDrawSettings previousDrawSettings = drawSettings;
		drawSettings = Entity.EntityTypes[0].DrawSettings;
		if (drawMode != SubEntityDrawMode.Default)
		{
			drawSettings = VisualizationUtil.VisualizationSettings.GetDrawModeSettings(drawMode).GetSubEntityDrawSettings(drawSettings);
		}

		lodLockedAtZero = drawMode != SubEntityDrawMode.Default && drawMode != SubEntityDrawMode.Hover;

		PolygonLayer layer = (PolygonLayer)Entity.Layer;
		if (drawSettings.InnerGlowEnabled)
		{
			layer.UpdateInnerGlow(drawSettings.InnerGlowRadius, drawSettings.InnerGlowIterations, drawSettings.InnerGlowMultiplier, drawSettings.InnerGlowPixelSize);
		}

		if (drawMode != SubEntityDrawMode.Hover)
		{
			UpdateDisplayedLOD();

			if (drawSettings != previousDrawSettings || planState != previousPlanState)
			{
				UpdateGameObjectForEveryLOD();
			}

			UpdatePolygonMaterialForDrawSettings(lods[displayedLOD], drawSettings);
		}

		PolygonLOD currentLod = lods[displayedLOD];
		if (meshIsDirty)
		{
			UpdateLod(currentLod);
			RebuildPolygon(currentLod);
			meshIsDirty = false;
		}

		if (currentLod.Polygon != null)
		{
			UpdatePolygonSubEntity(currentLod, drawSettings, selectedPoints, hoverPoints);
		}

		if (InvalidPoints != null)
		{
			VisualizationUtil.SelectionColor = VisualizationUtil.DEFAULT_SELECTION_COLOR;
		}	
	}

	protected override void UpdateRestrictionArea(float newRestrictionSize)
	{
		base.UpdateRestrictionArea(newRestrictionSize);
		if (restrictionArea == null && newRestrictionSize > 0.0f && !restrictionHidden)
		{
			restrictionArea = VisualizationUtil.CreateRestrictionArea();
			restrictionArea.SetParent(gameObject.transform);
		}

		if (restrictionArea != null && !restrictionHidden)
		{
			restrictionArea.SetPoints(GetPoints(), newRestrictionSize, true);
			if (!restrictionArea.gameObject.activeInHierarchy)
				restrictionArea.gameObject.SetActive(true);
		}

		restrictionNeedsUpdate = false;
	}

	public override void HideRestrictionArea()
	{
		base.HideRestrictionArea();
		if (restrictionArea != null)
			restrictionArea.gameObject.SetActive(false);
	}

	private void UpdateDisplayedLOD()
	{
		int newLod = 0;
		if (!lodLockedAtZero)
		{
			List<LODSettings> lodSettingsList = VisualizationUtil.VisualizationSettings.LODs;
			for (int i = 0; i < lodSettingsList.Count; ++i)
			{
				if (VisualizationUtil.DisplayScale >= lodSettingsList[i].MinScale)
				{
					newLod = i;
				}
			}
		}
		if (newLod != displayedLOD)
		{
			displayedLOD = newLod;
			for (int i = 0; i < lods.Count; ++i)
			{
				if (lods[i].GameObject != null)
				{
					lods[i].GameObject.SetActive(i == displayedLOD);
				}
			}
			UpdateLod(lods[displayedLOD]);
			RebuildPolygon(lods[displayedLOD]); //Maybe we should change this to keep a dirty flag on the lod? But since we only have a single lod ¯\(o.o)/¯
		}
	}

	public override void UpdateScale(Camera targetCamera)
	{
		if (gameObject == null)
		{
			return;
		}

        UpdateDisplayedLOD();
		UpdatePolygonPointScale(lods[displayedLOD], drawSettings);
		UpdatePolygonOutlineScale(drawSettings);
        if(textMesh != null)
            ScaleTextMesh();
    }

	public override SubEntityObject GetLayerObject()
	{
		SubEntityObject obj = new SubEntityObject();
		//obj.FID = this.Entity.FID;

		//List<GeometryObject> geoList = new List<GeometryObject>();

		//GeometryObject geo = new GeometryObject();
		obj.geometry = new List<List<float>>();

		obj.geometry = GetGeometryObjectFromPolygon(polygon).geometry;

		if (holes != null)
		{
			obj.subtractive = new List<GeometryObject>();
			for (int j = 0; j < holes.Count; j++)
			{
				obj.subtractive.Add(GetGeometryObjectFromPolygon(holes[j]));
			}
		}

		//geoList.Add(geo);

		//obj.geometry = geoList;

		return obj;
	}

	public bool CollidesWithPoint(Vector2 point, float maxDistance)
	{
		return Util.PointCollidesWithPolygon(point, polygon, holes, maxDistance);
	}

	public bool CollidesWithRect(Rect rect)
	{
		return Util.BoundingBoxCollidesWithPolygon(rect, polygon, holes);
	}

	public void Rasterize(int drawValue, int[,] raster, Rect rasterBounds)
	{
		RasterizationUtil.RasterizePolygon(polygon, holes, drawValue, raster, rasterBounds);
	}

	// make sure the winding order of the polygon is clockwise and the winding order of the holes are counter-clockwise
	public void ValidateWindingOrders()
	{
		if (!Util.PolygonIsClockwise(polygon))
		{
			polygon.Reverse();
		}

		if (holes != null)
		{
			for (int i = 0; i < holes.Count; ++i)
			{
				if (Util.PolygonIsClockwise(holes[i]))
				{
					holes[i].Reverse();
				}
			}
		}
	}

	public override Vector3 GetPointClosestTo(Vector3 position)
	{
		Vector3 closestPoint = Vector3.zero;
		float closestSqrMagnitude = float.MaxValue;

		foreach (Vector3 p in polygon)
		{
			float sqrMagnitude = (position - p).sqrMagnitude;
			if (sqrMagnitude < closestSqrMagnitude)
			{
				closestPoint = p;
				closestSqrMagnitude = sqrMagnitude;
			}
		}

		if (holes != null)
		{
			foreach (List<Vector3> hole in holes)
			{
				foreach (Vector3 p in hole)
				{
					float sqrMagnitude = (position - p).sqrMagnitude;
					if (sqrMagnitude < closestSqrMagnitude)
					{
						closestPoint = p;
						closestSqrMagnitude = sqrMagnitude;
					}
				}
			}
		}

		return closestPoint;
	}

	private void UpdateTextMeshPosition(PolygonLOD targetLod)
	{
		if (textMesh != null)
		{
			textMesh.SetPosition(new Vector3(BoundingBox.center.x, BoundingBox.center.y) + Entity.Layer.textInfo.textOffset, false);
		}
	}

	private void RebuildPolygon(PolygonLOD targetLod)
	{
        //if (Entity.Layer.Optimized)
        //    return;

        MeshFilter filter = targetLod.GameObject.GetComponent<MeshFilter>();
		Object.Destroy(filter.mesh);

		if (targetLod.Polygon != null)
		{
			PolygonLayer layer = (PolygonLayer)Entity.Layer;

			try
			{
				filter.mesh = VisualizationUtil.CreatePolygon(targetLod.Polygon, targetLod.Holes, Entity.patternRandomOffset, drawSettings.InnerGlowEnabled, layer.InnerGlowBounds);
			}
			catch (Exception ex)
			{
				Debug.LogError(string.Format("Triangulation error occurred in layer: {0}, database ID: {1}. Exception: {2}", layer.ID, databaseID, ex.Message));
				throw;
			}

			if (targetLod.Polygon.Count > 1) //Changed from 3, Kevin
			{
				RebuildOutline(targetLod);
				UpdateTextMeshPosition(targetLod);
			}
		}
		else
		{
			filter.mesh = null;
		}

	}

	private void RebuildOutline(PolygonLOD lod)
	{
		//Create outline
		if (outline == null)
		{
			outline = new GameObject("Outline");
			outline.transform.parent = gameObject.transform;
		}
		if (lod.Polygon.Count > 0)
		{
            outline.transform.localPosition = new Vector3(0, 0, LINE_FORWARD_EXTRUDE_AMOUNT);
		}

		LineRenderer lineRenderer = outline.GetComponent<LineRenderer>();
		if (lineRenderer == null)
		{
			lineRenderer = outline.AddComponent<LineRenderer>();
			lineRenderer.material = InterfaceCanvas.Instance.lineMaterials[0];
            lineRenderer.useWorldSpace = false;
			lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lineRenderer.receiveShadows = false;
		}

		lineRenderer.positionCount = lod.Polygon.Count;
		Vector3[] lineVertices = new Vector3[lod.Polygon.Count + 1];
		for (int i = 0; i < lod.Polygon.Count; i++)
		{
			lineVertices[i] = new Vector3(lod.Polygon[i].x, lod.Polygon[i].y, 0);
		}
		
		lineRenderer.SetPositions(lineVertices);
	}

	private void UpdatePolygonSubEntity(PolygonLOD targetLod, SubEntityDrawSettings targetDrawSettings, HashSet<int> selectedPoints, HashSet<int> hoverPoints)
	{
        //if (Entity.Layer.Optimized)
        //    return;

        int totalVertexCount = targetLod.Polygon.Count;
		if (targetLod.Holes != null)
		{
			foreach (List<Vector3> hole in targetLod.Holes)
			{
				totalVertexCount += hole.Count;
			}
		}
		UpdatePolygonVisualPoints(targetLod, targetDrawSettings, totalVertexCount, selectedPoints, hoverPoints);
		UpdatePolygonVisualOutline(targetDrawSettings, targetLod);
	}

	private void UpdatePolygonVisualPoints(PolygonLOD targetLod, SubEntityDrawSettings targetDrawSettings, int totalVertexCount, HashSet<int> selectedPoints, HashSet<int> hoverPoints)
	{
		bool displayPoints = targetDrawSettings.DisplayPoints || planState != SubEntityPlanState.NotInPlan;

		if (displayPoints)
		{
			int expectedChildCount = totalVertexCount;
			if (targetLod.PointContainerObject.transform.childCount != expectedChildCount)
			{
				VisualizationUtil.DestroyChildren(targetLod.PointContainerObject);
				for (int i = 0; i < expectedChildCount; ++i)
				{
					GameObject point = VisualizationUtil.CreatePoint();
					point.transform.SetParent(targetLod.PointContainerObject.transform, false);
				}
			}

			targetLod.PointContainerObject.SetActive(true);
			for (int i = 0; i < targetLod.PointContainerObject.transform.childCount; ++i)
			{
				GameObject point = targetLod.PointContainerObject.transform.GetChild(i).gameObject;

				bool hover = hoverPoints != null && hoverPoints.Contains(i);
				bool selected = selectedPoints != null && selectedPoints.Contains(i);

				Color pointColor = hover ? VisualizationUtil.SelectionColor : targetDrawSettings.PointColor;
				pointColor = selected ? Color.white : pointColor;

				VisualizationUtil.PointRenderMode pointRenderMode = VisualizationUtil.GetPointRenderMode(targetDrawSettings, planState, selected);
				
				VisualizationUtil.UpdatePoint(point, targetLod.GetPointPosition(i), pointColor, targetDrawSettings.PointSize, pointRenderMode);
			}

			UpdatePolygonPointScale(targetLod, targetDrawSettings);
		}
		else
		{
			targetLod.PointContainerObject.SetActive(false);
		}
	}

	private void UpdatePolygonVisualOutline(SubEntityDrawSettings drawSettings, PolygonLOD lod)
	{
		if (drawSettings.DisplayLines)
		{
			if (outline != null)
			{
				LineRenderer line = outline.GetComponent<LineRenderer>();
				Gradient gradient = new Gradient();
				gradient.SetKeys(
					new GradientColorKey[] { new GradientColorKey(drawSettings.LineColor, 0.0f), new GradientColorKey(drawSettings.LineColor, 1.0f) },
					new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
				);
				line.colorGradient = gradient;
				line.loop = drawSettings.DrawLineFromEndToStart;
				outline.SetActive(true);
				UpdatePolygonOutlineScale(drawSettings);
			}
		}
		else
		{
			if (outline != null)
			{
				if (outline.activeSelf)
				{
					outline.SetActive(false);
				}
			}
		}
	}

	private void UpdatePolygonMaterialForDrawSettings(PolygonLOD targetLod, SubEntityDrawSettings targetDrawSettings)
	{
		MeshRenderer renderer = targetLod.GameObject.GetComponent<MeshRenderer>();
		if (renderer != null)
		{
			if (targetDrawSettings.DisplayPolygon)
			{
				PolygonLayer layer = (PolygonLayer)Entity.Layer;
				if (targetDrawSettings.InnerGlowEnabled && layer.InnerGlowTexture != null)
				{
					renderer.material = MaterialManager.GetInnerGlowPolygonMaterial(layer.InnerGlowTexture, targetDrawSettings.PolygonPatternName, targetDrawSettings.PolygonColor);
				}
				else
				{
					renderer.material = MaterialManager.GetDefaultPolygonMaterial(targetDrawSettings.PolygonPatternName, targetDrawSettings.PolygonColor);
				}
				renderer.enabled = !firstToLastInvalid;
			}
			else
				renderer.enabled = false;
		}
	}

	private void UpdatePolygonPointScale(PolygonLOD targetLod, SubEntityDrawSettings targetDrawSettings)
	{
		float newPointScale = targetDrawSettings.PointSize * VisualizationUtil.DisplayScale * VisualizationUtil.pointResolutionScale;

		Transform pointTransform = targetLod.PointContainerObject.transform;
		for (int i = 0; i < pointTransform.childCount; ++i)
		{
			pointTransform.GetChild(i).localScale = new Vector3(newPointScale, newPointScale, 1);
		}
	}

	private void UpdatePolygonOutlineScale(SubEntityDrawSettings targetDrawSettings)
	{
		if (outline == null)
			return;
		LineRenderer lineRenderer = outline.GetComponent<LineRenderer>();
		float lineWidth = (targetDrawSettings.LineWidth * VisualizationUtil.DisplayScale) / 50.0f;
		//float lineWidth = (targetDrawSettings.LineWidth * CameraManager.Instance.gameCamera.orthographicSize) / 200.0f;
		lineRenderer.widthMultiplier = lineWidth;
	}

	public override List<Vector3> GetPoints()
	{
		return polygon;
	}

	public override void SetPoints(List<Vector3> points)
	{
		this.polygon = points;
		UpdateBoundingBox();
	}

	public override List<List<Vector3>> GetHoles(bool copy = false)
	{
		if (copy && holes != null)
		{
			List<List<Vector3>>  holesCopy = new List<List<Vector3>>(holes.Count);
			foreach (List<Vector3> hole in holes)
			{
				holesCopy.Add(new List<Vector3>(hole));
			}
			return holesCopy;
		}
		return holes;
	}

	public override void SetDataToCopy(SubEntityDataCopy copy)
	{
		base.SetDataToCopy(copy);
		meshIsDirty = true;
	}

    public override Feature GetGeoJSONFeature(int idToUse)
    {
        //Convert polygon
        double[][][] polygonPoints = new double[1][][];
        polygonPoints[0] = new double[polygon.Count+1][];
        if (!HasClockwiseWindingOrder())
        {
            for (int i = 0; i < polygon.Count; i++)
                polygonPoints[0][i] = Main.ConvertToGeoJSONCoordinate(new double[] {(double) polygon[i].x * 1000, (double) polygon[i].y * 1000});
        }
        else
        {
            int j = 0;
            for (int i = polygon.Count - 1; i >= 0; i--)
            {
                polygonPoints[0][j] = Main.ConvertToGeoJSONCoordinate(new double[] {(double) polygon[i].x * 1000, (double) polygon[i].y * 1000});
                j++;
            }
        }
        //Add the last point again to match GeoJSON specifications
        polygonPoints[0][polygon.Count] = polygonPoints[0][0];
        return new Feature(new Polygon(polygonPoints), GetGeoJSONProperties(), idToUse.ToString());
    }

    private bool HasClockwiseWindingOrder()
    {
        float edgeSum = 0;
        for (int i = 0; i < polygon.Count-1; i++)
        {
            edgeSum += (polygon[i + 1].x - polygon[i].x) * (polygon[i + 1].y + polygon[i].y);
        }
        //Last to first point edge
        edgeSum += (polygon[0].x - polygon[polygon.Count - 1].x) * (polygon[0].y + polygon[polygon.Count - 1].y);

        return edgeSum >= 0;
    }

    public virtual Dictionary<string, object> GetGeoJSONProperties()
    {
        return new Dictionary<string, object>();
    }
}

