using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using UnityEngine;

namespace MSP2050.Scripts
{
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
			m_mspID = geometry.mspid;
			m_restrictionNeedsUpdate = true;
			UpdateBoundingBox();

		}

		protected override void UpdateBoundingBox()
		{
			m_boundingBox = new Rect(position, Vector3.zero);
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

			Vector3 currentPos = m_gameObject.transform.position;
			m_gameObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, m_order);

		}

		public override void DrawGameObject(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null)
		{
			if (m_gameObject != null)
			{
				//Debug.LogError("Attempting to draw entity with an existing GameObject.");
				return;
			}

			m_gameObject = VisualizationUtil.Instance.CreatePointGameObject();
			m_gameObject.transform.SetParent(parent);

			if(m_entity.Layer.m_textInfo != null)
			{
				//Points need inverse scale...
				m_entity.Layer.m_textInfo.UseInverseScale = true;

				CreateTextMesh(m_gameObject.transform, m_entity.Layer.m_textInfo.textOffset);
				ScaleTextMesh(VisualizationUtil.Instance.UpdatePointScale(m_gameObject, m_entity.EntityTypes[0].DrawSettings));
			}

			RedrawGameObject(drawMode, selectedPoints, hoverPoints);
			UpdateRestrictionArea(m_entity.GetCurrentRestrictionSize());

			SetOrderBasedOnType();
		}

		public override void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
		{
			base.RedrawGameObject(drawMode, selectedPoints, hoverPoints, updatePlanState);

			if (drawMode == SubEntityDrawMode.Default && LayerManager.Instance.IsReferenceLayer(m_entity.Layer))
				drawMode = SubEntityDrawMode.PlanReference;

			SnappingToThisEnabled = IsSnapToDrawMode(drawMode);
		
			m_drawSettings = m_entity.EntityTypes[0].DrawSettings;
			if (drawMode != SubEntityDrawMode.Default) { m_drawSettings = VisualizationUtil.Instance.VisualizationSettings.GetDrawModeSettings(drawMode).GetSubEntityDrawSettings(m_drawSettings); }

			VisualizationUtil.Instance.UpdatePointSubEntity(m_gameObject, position, m_drawSettings, PlanState, selectedPoints != null, hoverPoints != null);
		}

		public override void UpdateGameObjectForEveryLOD()
		{
			VisualizationUtil.Instance.UpdatePointSubEntity(m_gameObject, position, m_drawSettings, PlanState, false, false);
		}

		public override void UpdateScale(Camera targetCamera)
		{
			if (m_gameObject == null)
				return;


			if (m_drawSettings == null)
			{
				m_drawSettings = m_entity.EntityTypes[0].DrawSettings;
				return;
				//Debug.LogError("Trying to draw point without drawsettings. GO name: " + gameObject.name + ". Parent name: " + gameObject.transform.parent.name);
			}

			float pointScale = VisualizationUtil.Instance.UpdatePointScale(m_gameObject, m_drawSettings);
			UpdateRestrictionArea(m_entity.GetCurrentRestrictionSize());

			ScaleTextMesh(pointScale);

			if(PlanState != SubEntityPlanState.NotShown)
				ObjectVisibility(m_gameObject, (float)GetImportance("natlscale", 10), targetCamera);
		}

		public override void UpdateGeometry(GeometryObject geo)
		{
			position = new Vector3(geo.geometry[0][0] / Main.SCALE, geo.geometry[0][1] / Main.SCALE);
			UpdateBoundingBox();
		}

		protected override SubEntityObject GetLayerObject()
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
			if (restrictionAreaSprite == null && newRestrictionSize > 0.0f && !m_restrictionHidden)
			{
				CreateRestrictionAreaSprite();
			}

			if (restrictionAreaSprite != null && !m_restrictionHidden)
			{
				restrictionAreaSprite.transform.localScale = new Vector3(newRestrictionSize / m_gameObject.transform.localScale.x * sizeOffsetToMatchPolyRestriction, newRestrictionSize / m_gameObject.transform.localScale.y * sizeOffsetToMatchPolyRestriction, 1f);
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
			float restrictionSize = m_entity.GetCurrentRestrictionSize();
			restrictionAreaSprite = VisualizationUtil.Instance.CreateRestrictionPoint();
			Transform transform = restrictionAreaSprite.transform;
			transform.SetParent(m_gameObject.transform, false);
			transform.localPosition = new Vector3(0, 0, 1f);
			transform.localScale = new Vector3(restrictionSize / m_gameObject.transform.localScale.x * sizeOffsetToMatchPolyRestriction, restrictionSize / m_gameObject.transform.localScale.y * sizeOffsetToMatchPolyRestriction, 1f);
		}

		public override List<Vector3> GetPoints()
		{
			return new List<Vector3>() { position };
		}

		public override Feature GetGeoJsonFeature(int idToUse)
		{
			double[] coordinates = new double[] {(double) position.x * 1000, (double) position.y * 1000};
			Main.Instance.ConvertToGeoJSONCoordinate(coordinates);
			return new Feature(new Point(new Position(coordinates[0], coordinates[1])), GetGeoJSONProperties(), idToUse.ToString());
		}

		public virtual Dictionary<string, object> GetGeoJSONProperties()
		{
			return new Dictionary<string, object>();
		}

		protected override void SetPoints(List<Vector3> points)
		{
			SetPosition(points[0]);
		}
	}
}
