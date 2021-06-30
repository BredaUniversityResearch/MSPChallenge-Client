using System;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

public class LineStringSubEntity : SubEntity
{
	//private const float LINE_FORWARD_EXTRUDE_AMOUNT = 0.05f;

	private class LineStringLOD
	{
		public List<Vector3> Points { get; private set; }
		public GameObject PointsObject { get; private set; }
		public GameObject IconsObject { get; private set; }
		public LineRenderer LineRenderer { get; private set; }

		public LineStringLOD(List<Vector3> points, Transform parentTransform)
		{
			LineRenderer = new GameObject("LineRenderer").AddComponent<LineRenderer>();
			LineRenderer.transform.SetParent(parentTransform);
			LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			LineRenderer.receiveShadows = false;
            LineRenderer.useWorldSpace = false;

			PointsObject = new GameObject("PointsContainer");
			PointsObject.transform.SetParent(parentTransform);

			IconsObject = new GameObject("IconsContainer");
			IconsObject.transform.SetParent(parentTransform);
			IconsObject.transform.localPosition = new Vector3(0.0f, 0.0f, -1.0f);

			SetGeometryData(points);
		}

		public void Destroy()
		{
			GameObject.Destroy(LineRenderer);
			GameObject.Destroy(PointsObject);
			GameObject.Destroy(IconsObject);
			LineRenderer = null;
			PointsObject = null;
			IconsObject = null;
			Points = null;
		}

		public void SetGeometryData(List<Vector3> points)
		{
			Points = points;
			//Set the linerenderer to the given points
			LineRenderer.positionCount = points.Count;
			Vector3[] lineVertices = new Vector3[points.Count + 1];
			for (int i = 0; i < points.Count; i++)
			{
                lineVertices[i] = new Vector3(points[i].x, points[i].y, 0);

			}
			LineRenderer.SetPositions(lineVertices);
		}
	}

	protected List<Vector3> m_points;
	public RestrictionArea m_restrictionArea;

	private bool lineLengthNeeded; //Was the length requested before?
	private float lineLengthKm;
	public float LineLengthKm
	{
		get
		{
			if (!lineLengthNeeded)
			{
				lineLengthNeeded = true;
				lineLengthKm = InterfaceCanvas.Instance.mapScale.GetRealWorldLineLength(m_points);
			}
			return lineLengthKm;
		}
	}//line length in KM

	private bool m_meshDirty = false;
	private LineStringLOD m_displayLod = null;

	public LineStringSubEntity(Entity entity, int persistentID = -1) : base(entity, -1, persistentID)
	{
		m_points = new List<Vector3>();
	}

	public LineStringSubEntity(Entity entity, SubEntityObject geometry, int databaseID) : base(entity, databaseID, geometry.persistent)
	{
		m_points = new List<Vector3>();

		for (int i = 0; i < geometry.geometry.Count; i++)
		{
			m_points.Add(new Vector3(geometry.geometry[i][0] / Main.SCALE, geometry.geometry[i][1] / Main.SCALE));
		}

		mspID = geometry.mspid;
		restrictionNeedsUpdate = true;
		OnPointsDataChanged();
	}

	public override void RemoveGameObject()
	{
		m_displayLod.Destroy();
		base.RemoveGameObject();
	}

	public override void SetDataToObject(SubEntityObject subEntityObject)
	{
		m_points = new List<Vector3>();

		for (int i = 0; i < subEntityObject.geometry.Count; i++)
		{
			m_points.Add(new Vector3(subEntityObject.geometry[i][0] / Main.SCALE, subEntityObject.geometry[i][1] / Main.SCALE));
		}

		OnPointsDataChanged();
	}

	protected override void UpdateBoundingBox()
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = Vector3.one * float.MinValue;
		foreach (Vector3 point in m_points)
		{
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}

		BoundingBox = new Rect(min, max - min);
		//Update line's length if it was needed before (avoid calculating on load)
		if(lineLengthNeeded)
			lineLengthKm = InterfaceCanvas.Instance.mapScale.GetRealWorldLineLength(m_points);
	}

	protected void OnPointsDataChanged()
	{
		m_meshDirty = true;
        restrictionNeedsUpdate = true;
		UpdateBoundingBox();
    }

	private void RebuildLODs()
	{
		m_displayLod = new LineStringLOD(m_points, gameObject.transform);
		UpdateLineStringSubEntity(m_displayLod, Entity.EntityTypes[0].DrawSettings, planState, null, null);
		m_meshDirty = false;
	}

	public void AddPoint(Vector3 point)
	{
		m_points.Add(point);
		OnPointsDataChanged();
	}

	public int AddPointBetween(Vector3 newPoint, int pointA, int pointB)
	{
		m_points.Insert(pointB, newPoint);
		OnPointsDataChanged();
		return pointB;
	}

    private void UpdateTextMeshPosition(LineStringLOD targetLod)
    {
        if (textMesh != null)
        {
            textMesh.SetPosition(Util.GetLineCenter(targetLod.Points) + Entity.Layer.textInfo.textOffset, false);
        }
    }

    public int GetPointAt(Vector3 position, out float closestDistanceSquared, int ignorePointId = -1)
	{
		float threshold = VisualizationUtil.GetSelectMaxDistance();
		threshold *= threshold;

		int closestPoint = -1;
		closestDistanceSquared = float.MaxValue;

		for (int i = 0; i < m_points.Count; ++i)
		{
			if (ignorePointId == i)
			{
				continue;
			}

			float distanceSquared = (m_points[i] - position).sqrMagnitude;
			if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
			{
				closestPoint = i;
				closestDistanceSquared = distanceSquared;
			}
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

		for (int i = 0; i < m_points.Count - 1; ++i)
		{
			float distanceSquared = Util.GetSquaredDistanceToLine(position, m_points[i], m_points[(i + 1) % m_points.Count]);
			if (distanceSquared < threshold && distanceSquared < closestDistanceSquared)
			{
				lineA = i;
				lineB = i + 1;
				closestDistanceSquared = distanceSquared;
			}
		}
	}

	public virtual HashSet<int> GetPointsInBox(Vector3 min, Vector3 max)
	{
		HashSet<int> result = new HashSet<int>();

		for (int i = 0; i < m_points.Count; ++i)
		{
			Vector3 position = m_points[i];
			if (position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y)
			{
				result.Add(i);
			}
		}

		return result.Count > 0 ? result : null;
	}

	public int GetPointCount()
	{
		return m_points.Count;
	}

	public Vector3 GetPointPosition(int pointIndex)
	{
		return m_points[pointIndex];
	}

	public void SetPointPosition(int pointIndex, Vector3 position)
	{
		m_points[pointIndex] = position;
		OnPointsDataChanged();
	}

	public bool RemovePoints(HashSet<int> indices)
	{
		bool[] remove = new bool[m_points.Count]; // all items are automatically initialized to false
		foreach (int index in indices)
		{
			if (index >= m_points.Count) { return false; }
			remove[index] = true;
		}

		for (int i = m_points.Count - 1; i >= 0; --i)
		{
			if (remove[i]) { m_points.RemoveAt(i); }
		}

		OnPointsDataChanged();
		return true;
	}

	public void Simplify(float tolerance)
	{
		m_points = Optimization.DouglasPeuckerReduction(m_points, tolerance);
		OnPointsDataChanged();
	}

	public override void UpdateGeometry(GeometryObject geo)
	{
		m_points = new List<Vector3>();

		for (int i = 0; i < geo.geometry.Count; i++)
		{
			m_points.Add(new Vector3(geo.geometry[i][0] / Main.SCALE, geo.geometry[i][1] / Main.SCALE));
		}

		OnPointsDataChanged();
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
		gameObject = new GameObject("Line String LODs");
		gameObject.transform.SetParent(parent);

		if (Entity.EntityTypes[0] == null)
		{
			//Mark Layer Dirty because it loaded in wrongly
			Entity.Layer.Dirty = true;
			return;
		}
		drawSettings = Entity.EntityTypes[0].DrawSettings;

        RebuildLODs();
        if (Entity.Layer.textInfo != null)
        {
            CreateTextMesh(gameObject.transform, Vector3.zero);
            m_meshDirty = true;
        }
		RedrawGameObject(drawMode, selectedPoints, hoverPoints);

		SetOrderBasedOnType();
	}

	public override void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
	{
		base.RedrawGameObject(drawMode, selectedPoints, hoverPoints, updatePlanState);

		if (gameObject == null)
			return;

        if (drawMode == SubEntityDrawMode.Default && LayerManager.IsReferenceLayer(Entity.Layer))
            drawMode = SubEntityDrawMode.PlanReference;

		SnappingToThisEnabled = IsSnapToDrawMode(drawMode);

		SubEntityDrawSettings previousDrawSettings = drawSettings;
		drawSettings = Entity.EntityTypes[0].DrawSettings;

		if (drawMode != SubEntityDrawMode.Default)
		{
			drawSettings = VisualizationUtil.VisualizationSettings.GetDrawModeSettings(drawMode).GetSubEntityDrawSettings(drawSettings);
		}

		bool meshDirtyFromOverride = false;
		Entity.OverrideDrawSettings(drawMode, ref drawSettings, ref meshDirtyFromOverride);

		//if (drawSettings != previousDrawSettings || planState != previousPlanState)
		//{
		//	UpdateGameObjectForEveryLOD();
		//}

        if (m_meshDirty || meshDirtyFromOverride)
		{
            m_displayLod.SetGeometryData(m_points);
            UpdateTextMeshPosition(m_displayLod);
		    CreateLineStringIconsForLod(m_displayLod, drawSettings);
            m_meshDirty = false;
		}

        if(m_displayLod.Points != null)
		    UpdateLineStringSubEntity(m_displayLod, drawSettings, planState, selectedPoints, hoverPoints);
    }

	public override void UpdateGameObjectForEveryLOD()
	{
		UpdateLineStringSubEntity(m_displayLod, drawSettings, planState, null, null);
	}

	private void UpdateLineStringSubEntity(LineStringLOD lod, SubEntityDrawSettings drawSettings, SubEntityPlanState planState, HashSet<int> selectedPoints, HashSet<int> hoverPoints)
	{
		UpdateLineStringPositionsAndColors(lod, drawSettings, planState, selectedPoints, hoverPoints);
		UpdateLineStringSubEntityScale(lod, drawSettings);
		UpdateLineStringIconsForLod(lod, drawSettings);
		lod.LineRenderer.material = InterfaceCanvas.Instance.lineMaterials[(int)drawSettings.LinePatternType];
        if (drawSettings.LinePatternType != ELinePatternType.Solid)
            lod.LineRenderer.textureMode = LineTextureMode.Tile;
    }

	private void CreateLineStringIconsForLod(LineStringLOD targetLod, SubEntityDrawSettings drawSettings)
	{
		bool shouldDrawIcons = !string.IsNullOrEmpty(drawSettings.LineIcon);
		int expectedChildCount = 0;
		if (shouldDrawIcons)
		{
			expectedChildCount = drawSettings.LineIconCount > 0 ? drawSettings.LineIconCount : targetLod.Points.Count - 1;
		}

		if (targetLod.IconsObject.transform.childCount != expectedChildCount)
		{
			// redraw everything
			VisualizationUtil.DestroyChildren(targetLod.IconsObject);

			if (shouldDrawIcons)
			{
				for (int i = 0; i < expectedChildCount; ++i)
				{
					GameObject segmentIcon = VisualizationUtil.CreateLineStringIconObject(drawSettings.LineIcon, drawSettings.LineIconColor);
					segmentIcon.transform.SetParent(targetLod.IconsObject.transform, false);
				}
			}
		}
	}

	private void UpdateLineStringIconsForLod(LineStringLOD targetLod, SubEntityDrawSettings drawSettings)
	{
		Sprite newSprite = Resources.Load<Sprite>(drawSettings.LineIcon);
		Transform iconsTransform = targetLod.IconsObject.transform;
		for (int i = 0; i < iconsTransform.childCount; ++i)
		{
			SpriteRenderer renderer = iconsTransform.GetChild(i).GetComponent<SpriteRenderer>();
			renderer.sprite = newSprite;
			renderer.color = drawSettings.LineIconColor;
		}
	}

	protected override void UpdateRestrictionArea(float newRestrictionSize)
	{
		base.UpdateRestrictionArea(newRestrictionSize);
		if (m_restrictionArea == null && newRestrictionSize > 0.0f && !restrictionHidden)
		{
			m_restrictionArea = VisualizationUtil.CreateRestrictionArea();
			m_restrictionArea.SetParent(gameObject.transform);
		}

		if (m_restrictionArea != null && !restrictionHidden)
		{
			m_restrictionArea.SetPoints(m_points, newRestrictionSize, false);
			if (!m_restrictionArea.gameObject.activeInHierarchy)
				m_restrictionArea.gameObject.SetActive(true);
		}
	}

	public override void HideRestrictionArea()
	{
		base.HideRestrictionArea();
		if (m_restrictionArea != null)
			m_restrictionArea.gameObject.SetActive(false);
	}

	public override void UpdateScale(Camera targetCamera)
	{
		UpdateLineStringSubEntityScale(m_displayLod, drawSettings);
        if (textMesh != null)
            ScaleTextMesh();
    }

    public Vector3 GetMiddlePoint()
	{
		int id = (int)(m_points.Count / 2);
		return m_points[id];
	}

	public override SubEntityObject GetLayerObject()
	{
		SubEntityObject obj = new SubEntityObject();
		List<List<float>> geometry = new List<List<float>>();

		for (int i = 0; i < m_points.Count; i++)
		{
			List<float> vertices = new List<float>();
			vertices.Add(m_points[i].x * Main.SCALE);
			vertices.Add(m_points[i].y * Main.SCALE);
			geometry.Add(vertices);
		}

		obj.geometry = geometry;

		return obj;
	}

	public bool CollidesWithPoint(Vector2 point, float maxDistance)
	{
		return Util.PointCollidesWithLineString(point, m_points, maxDistance);
	}

    public float DistanceToPoint(Vector2 point)
    {
        return Util.PointDistanceFromLineString(point, m_points);
    }

    public bool CollidesWithRect(Rect rect)
	{
		return Util.BoundingBoxCollidesWithLineString(rect, m_points);
	}

	public override Vector3 GetPointClosestTo(Vector3 position)
	{
		Vector3 closestPoint = Vector3.zero;
		float closestSqrMagnitude = float.MaxValue;

		foreach (Vector3 p in m_points)
		{
			float sqrMagnitude = (position - p).sqrMagnitude;
			if (sqrMagnitude < closestSqrMagnitude)
			{
				closestPoint = p;
				closestSqrMagnitude = sqrMagnitude;
			}
		}

		return closestPoint;
	}

	public bool AreFirstOrLastPoints(HashSet<int> checkPoints)
	{
		foreach (int i in checkPoints)
			if (i == 0 || i == m_points.Count - 1)
				return true;
		return false;
	}

    public bool AreOnlyFirstOrLastPoint(HashSet<int> checkPoints)
    {
        foreach (int i in checkPoints)
        {
            if (i == 0 || i == m_points.Count - 1)
                return true;
            else
                return false;
        }
        return false;
    }

    private void UpdateLineStringPositionsAndColors(LineStringLOD lod, SubEntityDrawSettings drawSettings, SubEntityPlanState planState, HashSet<int> selectedPoints, HashSet<int> hoverPoints)
	{
		bool displayPoints = drawSettings.DisplayPoints || planState != SubEntityPlanState.NotInPlan;

		//Set color for entire line
		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(drawSettings.LineColor, 0.0f), new GradientColorKey(drawSettings.LineColor, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
		);
		lod.LineRenderer.colorGradient = gradient;

        //Points
        if (displayPoints)
        {
            UpdateNumberPointObjectsForLod(lod, displayPoints);
            lod.PointsObject.SetActive(true);
            for (int i = 0; i < lod.Points.Count; ++i)
            {
                GameObject point = lod.PointsObject.transform.GetChild(i).gameObject;

                bool hover = hoverPoints != null && hoverPoints.Contains(i);
                bool selected = selectedPoints != null && selectedPoints.Contains(i);

                Color pointColor = hover ? VisualizationUtil.SelectionColor : drawSettings.PointColor;
                pointColor = selected ? Color.white : pointColor;

                VisualizationUtil.PointRenderMode pointRenderMode = VisualizationUtil.GetPointRenderMode(drawSettings, planState, selected);
                VisualizationUtil.UpdatePoint(point, lod.Points[i], pointColor, drawSettings.PointSize, pointRenderMode);
            }
        }
        else
        {
            lod.PointsObject.SetActive(false);
        }

		//Icons
		Transform iconsContainerTransform = lod.IconsObject.transform;
		int iconCount = iconsContainerTransform.childCount;
		if (iconCount > 0 && lod.Points.Count >= 2) //Cannot put an icon on a point, we need a line and thus at least 2 points
		{
			if (iconCount >= lod.Points.Count)
			{
				throw new System.ArgumentOutOfRangeException("Well this is going to fail. iconCount cannot approach the actual number of icons that we have.");
			}
			int iconPointSpacing = Mathf.RoundToInt((float)lod.Points.Count / (float)(iconCount + 1)); // We add 1 to skip over the starting point so we end up in the middle on the first entry.
																									   //Since we're interpolating from point to point + 1 we need to substract 1 from the initial spacing to ensure we don't go out of bounds when we only have 2 entries. 
																									   //initialSpacing = (2 / (1 + 1)) - 1 = 0; initialIconSpacing = (6 / (1 + 1)) - 2 = 2;
			int initialIconSpacing = iconPointSpacing - 1;
			for (int i = 0; i < iconCount; ++i)
			{
				Transform targetTransform = lod.IconsObject.transform.GetChild(i).transform;
				int pointIndex = (i * iconPointSpacing) + initialIconSpacing;
				Vector3 lineFrom = lod.Points[pointIndex];
				Vector3 deltaPoint = lod.Points[pointIndex + 1] - lineFrom;
				targetTransform.localPosition = lineFrom + (deltaPoint * 0.5f);
				targetTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, (Mathf.Atan2((float)deltaPoint.y, (float)deltaPoint.x) * 180.0f) / Mathf.PI);
			}
		}
	}

    private void UpdateNumberPointObjectsForLod(LineStringLOD lod, bool drawPoints)
    {
        //Recreate points if necessary.
        int expectedChildCount = lod.Points.Count;
        if (lod.PointsObject.transform.childCount != expectedChildCount)
        {
            // redraw everything
            VisualizationUtil.DestroyChildren(lod.PointsObject);

            for (int i = 0; i < lod.Points.Count; ++i)
            {
                GameObject point = VisualizationUtil.CreatePointGameObject();
                point.transform.SetParent(lod.PointsObject.transform);
            }

        }
    }

    private void UpdateLineStringSubEntityScale(LineStringLOD lod, SubEntityDrawSettings drawSettings)
	{
		//Update line game objects. 
		if (drawSettings.FixedWidth)
		{
			lod.LineRenderer.widthMultiplier = drawSettings.LineWidth / InterfaceCanvas.Instance.mapScale.GameToRealWorldScale;
		}
		else
		{
			lod.LineRenderer.widthMultiplier = VisualizationUtil.DisplayScale * drawSettings.LineWidth / 50f;
		}

		//Update Points 
		int pointCount = lod.PointsObject.transform.childCount;
		float newScale = drawSettings.PointSize * VisualizationUtil.DisplayScale * VisualizationUtil.pointResolutionScale;
		for (int i = 0; i < pointCount; ++i)
		{
			lod.PointsObject.transform.GetChild(i).localScale = new Vector3(newScale, newScale, 1);
		}

		//Update Icons 
		int iconsCount = lod.IconsObject.transform.childCount;
		for (int i = 0; i < iconsCount; ++i)
		{
			Transform child = lod.IconsObject.transform.GetChild(i);
			child.localScale = new Vector3(VisualizationUtil.DisplayScale, VisualizationUtil.DisplayScale, 1.0f);
		}
	}

	public override List<Vector3> GetPoints()
	{
		return m_points;
	}

	public override void SetPoints(List<Vector3> points)
	{
		m_points = points;
		OnPointsDataChanged();
	}

    public override Feature GetGeoJSONFeature(int idToUse)
    {
        //Convert line
        double[][] linePoints = new double[m_points.Count][];
        for (int i = 0; i < m_points.Count; i++)
            linePoints[i] = Main.ConvertToGeoJSONCoordinate(new double[] { (double)m_points[i].x * 1000, (double)m_points[i].y * 1000 });

        return new Feature(new LineString(linePoints), GetGeoJSONProperties(), idToUse.ToString());
    }

    public virtual Dictionary<string, object> GetGeoJSONProperties()
    {
        return new Dictionary<string, object>();
    }
}