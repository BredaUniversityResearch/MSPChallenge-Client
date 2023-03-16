using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PointSubEntity : SubEntity
	{
		private Vector3 m_position;
		private GameObject m_restrictionAreaSprite;
		private const float SIZE_OFFSET_TO_MATCH_POLY_RESTRICTION = Mathf.PI;
		private const float CHARACTER_SIZE = 0.04f;

		public PointSubEntity(Entity a_entity, Vector3 a_position, int a_persistentID = -1) : base(a_entity, -1, a_persistentID)
		{
			m_position = a_position;
			UpdateBoundingBox();
		}

		public PointSubEntity(Entity a_entity, SubEntityObject a_geometry, int a_databaseID) : base(a_entity, a_databaseID, a_geometry.persistent)
		{
			m_position = new Vector3(a_geometry.geometry[0][0] / Main.SCALE, a_geometry.geometry[0][1] / Main.SCALE);
			m_mspID = a_geometry.mspid;
			m_restrictionNeedsUpdate = true;
			UpdateBoundingBox();

		}

		protected override void UpdateBoundingBox()
		{
			m_boundingBox = new Rect(m_position, Vector3.zero);
		}

		public Vector3 GetPosition()
		{
			return m_position;
		}

		public void SetPosition(Vector3 a_position)
		{
			m_position = a_position;
			UpdateBoundingBox();
		}

		public override void SetDataToObject(SubEntityObject a_subEntityObject)
		{
			m_position = new Vector3(a_subEntityObject.geometry[0][0] / Main.SCALE, a_subEntityObject.geometry[0][1] / Main.SCALE);
			UpdateBoundingBox();
		}

		public override void SetOrderBasedOnType()
		{
			calculateOrderBasedOnType();

			Vector3 currentPos = m_gameObject.transform.position;
			m_gameObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, m_order);

		}

		public override void DrawGameObject(Transform a_parent, SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, HashSet<int> a_selectedPoints = null, HashSet<int> a_hoverPoints = null)
		{
			if (m_gameObject != null)
			{
				//Debug.LogError("Attempting to draw entity with an existing GameObject.");
				return;
			}

			m_gameObject = VisualizationUtil.Instance.CreatePointGameObject();
			m_gameObject.transform.SetParent(a_parent);

			if(m_entity.Layer.m_textInfo != null)
			{
				//Points need inverse scale...
				m_entity.Layer.m_textInfo.UseInverseScale = true;

				CreateTextMesh(m_gameObject.transform, m_entity.Layer.m_textInfo.textOffset);
				ScaleTextMesh(VisualizationUtil.Instance.UpdatePointScale(m_gameObject, m_entity.EntityTypes[0].DrawSettings));
			}

			RedrawGameObject(a_drawMode, a_selectedPoints, a_hoverPoints);
			UpdateRestrictionArea(m_entity.GetCurrentRestrictionSize());

			SetOrderBasedOnType();
		}

		public override void RedrawGameObject(SubEntityDrawMode a_drawMode = SubEntityDrawMode.Default, HashSet<int> a_selectedPoints = null, HashSet<int> a_hoverPoints = null, bool a_updatePlanState = true)
		{
			base.RedrawGameObject(a_drawMode, a_selectedPoints, a_hoverPoints, a_updatePlanState);

			if (a_drawMode == SubEntityDrawMode.Default && LayerManager.Instance.IsReferenceLayer(m_entity.Layer))
				a_drawMode = SubEntityDrawMode.PlanReference;

			SnappingToThisEnabled = IsSnapToDrawMode(a_drawMode);
		
			m_drawSettings = m_entity.EntityTypes[0].DrawSettings;
			if (a_drawMode != SubEntityDrawMode.Default) { m_drawSettings = VisualizationUtil.Instance.VisualizationSettings.GetDrawModeSettings(a_drawMode).GetSubEntityDrawSettings(m_drawSettings); }

			VisualizationUtil.Instance.UpdatePointSubEntity(m_gameObject, m_position, m_drawSettings, PlanState, a_selectedPoints != null, a_hoverPoints != null);
		}

		public override void UpdateGameObjectForEveryLOD()
		{
			VisualizationUtil.Instance.UpdatePointSubEntity(m_gameObject, m_position, m_drawSettings, PlanState, false, false);
		}

		public override void UpdateScale(Camera a_targetCamera)
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
				ObjectVisibility(m_gameObject, (float)GetImportance("natlscale", 10), a_targetCamera);
		}

		public override void UpdateGeometry(GeometryObject a_geo)
		{
			m_position = new Vector3(a_geo.geometry[0][0] / Main.SCALE, a_geo.geometry[0][1] / Main.SCALE);
			UpdateBoundingBox();
		}

		protected override SubEntityObject GetLayerObject()
		{
			SubEntityObject obj = new SubEntityObject();

			obj.subtractive = null;

			List<List<float>> listOfPoints = new List<List<float>>();
			List<float> points = new List<float>();
			points.Add(m_position.x * Main.SCALE);
			points.Add(m_position.y * Main.SCALE);
			listOfPoints.Add(points);

			obj.geometry = listOfPoints;

			return obj;
		}

		public override Vector3 GetPointClosestTo(Vector3 a_position)
		{
			return a_position;
		}

		protected override void UpdateRestrictionArea(float a_newRestrictionSize)
		{
			base.UpdateRestrictionArea(a_newRestrictionSize);
			if (m_restrictionAreaSprite == null && a_newRestrictionSize > 0.0f && !m_restrictionHidden)
			{
				CreateRestrictionAreaSprite();
			}

			if (m_restrictionAreaSprite != null && !m_restrictionHidden)
			{
				m_restrictionAreaSprite.transform.localScale = new Vector3(a_newRestrictionSize / m_gameObject.transform.localScale.x * SIZE_OFFSET_TO_MATCH_POLY_RESTRICTION, a_newRestrictionSize / m_gameObject.transform.localScale.y * SIZE_OFFSET_TO_MATCH_POLY_RESTRICTION, 1f);
				if (!m_restrictionAreaSprite.gameObject.activeInHierarchy)
					m_restrictionAreaSprite.gameObject.SetActive(true);
			}
		}

		public override void HideRestrictionArea()
		{
			base.HideRestrictionArea();
			if(m_restrictionAreaSprite != null)
				m_restrictionAreaSprite.SetActive(false);
		}

		private void CreateRestrictionAreaSprite()
		{
			float restrictionSize = m_entity.GetCurrentRestrictionSize();
			m_restrictionAreaSprite = VisualizationUtil.Instance.CreateRestrictionPoint();
			Transform transform = m_restrictionAreaSprite.transform;
			transform.SetParent(m_gameObject.transform, false);
			transform.localPosition = new Vector3(0, 0, 1f);
			transform.localScale = new Vector3(restrictionSize / m_gameObject.transform.localScale.x * SIZE_OFFSET_TO_MATCH_POLY_RESTRICTION, restrictionSize / m_gameObject.transform.localScale.y * SIZE_OFFSET_TO_MATCH_POLY_RESTRICTION, 1f);
		}

		public override List<Vector3> GetPoints()
		{
			return new List<Vector3>() { m_position };
		}

		public override Feature GetGeoJsonFeature(int a_idToUse)
		{
			double[] coordinates = new double[] {(double) m_position.x * 1000, (double) m_position.y * 1000};
			Main.Instance.ConvertToGeoJSONCoordinate(coordinates);
			return new Feature(new Point(new Position(coordinates[0], coordinates[1])), GetGeoJsonProperties(), a_idToUse.ToString());
		}

		public virtual Dictionary<string, object> GetGeoJsonProperties()
		{
			return new Dictionary<string, object>();
		}

		protected override void SetPoints(List<Vector3> a_points)
		{
			SetPosition(a_points[0]);
		}
	}
}
