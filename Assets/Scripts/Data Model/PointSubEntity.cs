using System;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

public class PointSubEntity : SubEntity
{
	private Vector3 position;
	private GameObject restrictionAreaSprite;
	const float sizeOffsetToMatchPolyRestriction = Mathf.PI;
	private const float characterSize = 0.04f;

	public PointSubEntity(Entity entity, Vector3 position, int persistentID = -1) : base(entity, -1, persistentID)
	{
		this.position = position;
		UpdateBoundingBox();
	}

	public PointSubEntity(Entity entity, SubEntityObject geometry, int databaseID) : base(entity, databaseID, geometry.persistent)
	{
		position = new Vector3(geometry.geometry[0][0] / Main.SCALE, geometry.geometry[0][1] / Main.SCALE);
		mspID = geometry.mspid;
		restrictionNeedsUpdate = true;
		UpdateBoundingBox();

	}

	protected override void UpdateBoundingBox()
	{
		BoundingBox = new Rect(position, Vector3.zero);
	}

	public Vector3 GetPosition()
	{
		return position;
	}

	public void SetPosition(Vector3 position)
	{
		this.position = position;
		UpdateBoundingBox();
	}

	public override void SetDataToObject(SubEntityObject subEntityObject)
	{
		this.position = new Vector3(subEntityObject.geometry[0][0] / Main.SCALE, subEntityObject.geometry[0][1] / Main.SCALE);
		UpdateBoundingBox();
	}

	public override void SetOrderBasedOnType()
	{
		calculateOrderBasedOnType();

		Vector3 currentPos = gameObject.transform.position;
		gameObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, Order);

	}

	public override void DrawGameObject(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null)
	{
		if (gameObject != null)
		{
			//Debug.LogError("Attempting to draw entity with an existing GameObject.");
			return;
		}

		gameObject = VisualizationUtil.CreatePointGameObject();
		gameObject.transform.SetParent(parent);

		if(Entity.Layer.textInfo != null)
		{
			//Points need inverse scale...
			Entity.Layer.textInfo.UseInverseScale = true;

            CreateTextMesh(gameObject.transform, Entity.Layer.textInfo.textOffset);
            ScaleTextMesh(VisualizationUtil.UpdatePointScale(gameObject, Entity.EntityTypes[0].DrawSettings));
        }

		RedrawGameObject(drawMode, selectedPoints, hoverPoints);
		UpdateRestrictionArea(Entity.GetCurrentRestrictionSize());

		SetOrderBasedOnType();
	}

	public override void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
	{
		base.RedrawGameObject(drawMode, selectedPoints, hoverPoints, updatePlanState);

		if (drawMode == SubEntityDrawMode.Default && LayerManager.IsReferenceLayer(Entity.Layer))
			drawMode = SubEntityDrawMode.PlanReference;

		SnappingToThisEnabled = IsSnapToDrawMode(drawMode);
		
		drawSettings = Entity.EntityTypes[0].DrawSettings;
		if (drawMode != SubEntityDrawMode.Default) { drawSettings = VisualizationUtil.VisualizationSettings.GetDrawModeSettings(drawMode).GetSubEntityDrawSettings(drawSettings); }

		VisualizationUtil.UpdatePointSubEntity(gameObject, position, drawSettings, planState, selectedPoints != null, hoverPoints != null);
	}

	public override void UpdateGameObjectForEveryLOD()
	{
		VisualizationUtil.UpdatePointSubEntity(gameObject, position, drawSettings, planState, false, false);
	}

	public override void UpdateScale(Camera targetCamera)
	{
		if (gameObject == null)
			return;


		if (drawSettings == null)
		{
			drawSettings = Entity.EntityTypes[0].DrawSettings;
			return;
			//Debug.LogError("Trying to draw point without drawsettings. GO name: " + gameObject.name + ". Parent name: " + gameObject.transform.parent.name);
		}

		float pointScale = VisualizationUtil.UpdatePointScale(gameObject, drawSettings);
		UpdateRestrictionArea(Entity.GetCurrentRestrictionSize());

        ScaleTextMesh(pointScale);

		if(planState != SubEntityPlanState.NotShown)
			objectVisibility(gameObject, (float)getImportance("natlscale", 10), targetCamera);
	}

    public override void UpdateGeometry(GeometryObject geo)
	{
		position = new Vector3(geo.geometry[0][0] / Main.SCALE, geo.geometry[0][1] / Main.SCALE);
		UpdateBoundingBox();
	}

	public override SubEntityObject GetLayerObject()
	{
		SubEntityObject obj = new SubEntityObject();

		obj.subtractive = null;

		List<List<float>> listOfPoints = new List<List<float>>();
		List<float> points = new List<float>();
		points.Add(position.x * Main.SCALE);
		points.Add(position.y * Main.SCALE);
		listOfPoints.Add(points);

		obj.geometry = listOfPoints;

		return obj;
	}

	public override Vector3 GetPointClosestTo(Vector3 position)
	{
		return position;
	}

	protected override void UpdateRestrictionArea(float newRestrictionSize)
	{
		base.UpdateRestrictionArea(newRestrictionSize);
		if (restrictionAreaSprite == null && newRestrictionSize > 0.0f && !restrictionHidden)
		{
			CreateRestrictionAreaSprite();
		}

		if (restrictionAreaSprite != null && !restrictionHidden)
		{
			restrictionAreaSprite.transform.localScale = new Vector3(newRestrictionSize / gameObject.transform.localScale.x * sizeOffsetToMatchPolyRestriction, newRestrictionSize / gameObject.transform.localScale.y * sizeOffsetToMatchPolyRestriction, 1f);
			if (!restrictionAreaSprite.gameObject.activeInHierarchy)
				restrictionAreaSprite.gameObject.SetActive(true);
		}
	}

	public override void HideRestrictionArea()
	{
		base.HideRestrictionArea();
		if(restrictionAreaSprite != null)
			restrictionAreaSprite.SetActive(false);
	}

	private void CreateRestrictionAreaSprite()
	{
		float restrictionSize = Entity.GetCurrentRestrictionSize();
		restrictionAreaSprite = VisualizationUtil.CreateRestrictionPoint();
		Transform transform = restrictionAreaSprite.transform;
		transform.SetParent(gameObject.transform, false);
		transform.localPosition = new Vector3(0, 0, 1f);
		transform.localScale = new Vector3(restrictionSize / gameObject.transform.localScale.x * sizeOffsetToMatchPolyRestriction, restrictionSize / gameObject.transform.localScale.y * sizeOffsetToMatchPolyRestriction, 1f);
	}

	public override List<Vector3> GetPoints()
	{
		return new List<Vector3>() { position };
	}

    public override Feature GetGeoJSONFeature(int idToUse)
    {
        double[] coordinates = new double[] {(double) position.x * 1000, (double) position.y * 1000};
        Main.ConvertToGeoJSONCoordinate(coordinates);
        return new Feature(new Point(new Position(coordinates[0], coordinates[1])), GetGeoJSONProperties(), idToUse.ToString());
    }

    public virtual Dictionary<string, object> GetGeoJSONProperties()
    {
        return new Dictionary<string, object>();
    }

    public override void SetPoints(List<Vector3> points)
	{
		SetPosition(points[0]);
	}
}
