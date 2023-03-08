using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MSP2050.Scripts
{
	public class RasterSubentity : SubEntity
	{
		private const int GRADIENT_RESOLUTION = 256;

		private Texture2D raster;
		private Texture2D colorGradient;
		private SpriteRenderer spriteRenderer;
		private float materialPatternOffset;

		private Vector2 offset;
		private Vector2 scale;

		private bool hasDrawnNewRaster = false;

		public RasterSubentity(Entity entity, Texture2D raster, Vector3 offset, Vector2 scale, int databaseID = -1) 
			: base(entity, databaseID)
		{
			this.raster = raster;
			this.offset = offset;
			this.scale = scale;
		}

		public void SetScale(Vector2 scale)
		{
			this.scale = scale;

			if (m_gameObject != null)
			{
				m_gameObject.transform.localScale = this.scale;
			}
			else
			{
				Debug.Log(m_entity.Layer.GetShortName() + " doesn't have a gameobject yet!");
			}

			UpdateBoundingBox();
		}

		public override void DrawGameObject(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null)
		{
			if (m_gameObject == null)
			{
				RasterLayer sourceLayer = (RasterLayer) m_entity.Layer;
				m_gameObject = VisualizationUtil.Instance.CreateRasterGameObject(sourceLayer.rasterObject.layer_raster_material);
				m_gameObject.transform.SetParent(parent);

				m_gameObject.transform.localScale = scale;
				m_gameObject.transform.localPosition = offset;

				spriteRenderer = m_gameObject.GetComponent<SpriteRenderer>();

				spriteRenderer.material.SetFloat("_ValueCutoff",
					sourceLayer.rasterObject.layer_raster_minimum_value_cutoff /
					sourceLayer.rasterValueToEntityValueMultiplier
				);
				spriteRenderer.material.SetTexture("_Dither", MaterialManager.Instance.GetPatternOrDefault(sourceLayer.rasterObject.layer_raster_pattern));
				SetRenderPatternOffset(materialPatternOffset);
				SetupColorGradient(
					spriteRenderer.material,
					sourceLayer.GetEntityTypesSortedByValue(),
					sourceLayer.rasterObject.layer_raster_color_interpolation,
					sourceLayer.rasterValueToEntityValueMultiplier,
					sourceLayer.rasterObject.layer_raster_minimum_value_cutoff
				);
			}
	   
			RedrawGameObject(drawMode, selectedPoints, hoverPoints);
			SetOrderBasedOnType();
		}

		public override void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
		{
			base.RedrawGameObject(drawMode, selectedPoints, hoverPoints, updatePlanState);

			if (m_gameObject == null)
			{
				return;
			}

			if (drawMode == SubEntityDrawMode.Default && LayerManager.Instance.IsReferenceLayer(m_entity.Layer))
				drawMode = SubEntityDrawMode.PlanReference;

			// PdG 2017-10-03: Disabled snapping entirely for Raster Subentities otherwise everything would snap to the bathymetry layer at the mouse position.
			SnappingToThisEnabled = false; // snapToDrawMode(drawMode);

			m_drawSettings = m_entity.EntityTypes[0].DrawSettings;
			if (drawMode != SubEntityDrawMode.Default) { m_drawSettings = VisualizationUtil.Instance.VisualizationSettings.GetDrawModeSettings(drawMode).GetSubEntityDrawSettings(m_drawSettings); }

			if (raster != null)
			{
				if (hasDrawnNewRaster == false) // to not redraw if you dont have a new raster
				{
					spriteRenderer.sprite = Sprite.Create(raster, new Rect(0, 0, raster.width, raster.height), Vector2.zero, 100.0f, 0, SpriteMeshType.FullRect);
					//spriteRenderer.material.SetTexture("_MainTex", raster);
					hasDrawnNewRaster = true;
				}
			}
			else
			{
				Debug.Log("Texture is null");
			}
		}

		public override void RemoveGameObject()
		{
			base.RemoveGameObject();
			Object.Destroy(colorGradient);
		}

		public void SetNewRaster(Texture2D newRaster)
		{
			this.raster = newRaster;

			hasDrawnNewRaster = false;

			if(m_gameObject != null)
				RedrawGameObject();
		}

		public Material GetMaterial()
		{
			if (spriteRenderer == null)
				return null;

			return spriteRenderer.material;
		}

		protected override SubEntityObject GetLayerObject()
		{
			throw new NotImplementedException();
		}

		public override Vector3 GetPointClosestTo(Vector3 position)
		{
			return position;
		}

		public override void SetOrderBasedOnType()
		{
			// no sorting needed on a raster layer
		}

		public override void UpdateGameObjectForEveryLOD()
		{
			//   throw new NotImplementedException();
		}
	
		public override void UpdateGeometry(GeometryObject geo)
		{
			throw new NotImplementedException();
		}

		protected override void UpdateBoundingBox()
		{
			m_boundingBox = ((RasterLayer)m_entity.Layer).RasterBounds;
		}

		public override void SetDataToObject(SubEntityObject subEntityObject)
		{
		}

		protected override void UpdatePlanState()
		{
			//Do nothing, except for forcing the planstate to set to not in plan.
			PlanState = SubEntityPlanState.NotInPlan;
		}

		public override Feature GetGeoJsonFeature(int idToUse)
		{
			throw new NotImplementedException();
		}

		public void SetRenderPatternOffset(float patternOffset)
		{
			materialPatternOffset = patternOffset;
			if (spriteRenderer != null)
			{
				spriteRenderer.material.SetFloat("_OffsetX", patternOffset);
			}
		}

		private void SetupColorGradient(Material targetMaterial, List<EntityType> layerEntityTypesSortedByValue, ERasterColorInterpolationMode interpolationMode, float rasterValueToEntityValueMultiplier, float rasterMinimumValueCutoff)
		{
			if (colorGradient == null)
			{
				colorGradient = new Texture2D(GRADIENT_RESOLUTION, 1, TextureFormat.ARGB32, false, false)
				{
					filterMode = FilterMode.Point,
					wrapMode = TextureWrapMode.Clamp
				};
			}
		
			for (int i = 0; i < GRADIENT_RESOLUTION; ++i)
			{
				float value = (i / (float) (GRADIENT_RESOLUTION - 1)) * rasterValueToEntityValueMultiplier;
				value = Mathf.Clamp(value, 0.0f, rasterValueToEntityValueMultiplier);
				// note MH: below cutoff value should not be mapped to a entity type
				if (value < rasterMinimumValueCutoff)
				{
					continue;
				}

				EntityType lowBound = layerEntityTypesSortedByValue[0];
				EntityType highBound = layerEntityTypesSortedByValue[0];
				for (int entityTypeId = 1; entityTypeId < layerEntityTypesSortedByValue.Count; ++entityTypeId)
				{
					EntityType type = layerEntityTypesSortedByValue[entityTypeId];
					if (type.value >= value)
					{
						highBound = type;
						break;
					}
					else
					{
						lowBound = type;
					}
				}

				float lerp = Mathf.Clamp01((float)(value - lowBound.value) / (float)(highBound.value - lowBound.value));

				Color gradientColor;
				switch(interpolationMode)
				{
					case ERasterColorInterpolationMode.Linear:
						gradientColor = Color.Lerp(lowBound.DrawSettings.PolygonColor, highBound.DrawSettings.PolygonColor, lerp);
						break;
					case ERasterColorInterpolationMode.Point:
						gradientColor = ((lerp > 0.5f)? highBound : lowBound).DrawSettings.PolygonColor;
						break;
					default:
						Debug.LogError("Unsupported color interpolation mode " + interpolationMode);
						gradientColor = lowBound.DrawSettings.PolygonColor;
						break;
				}
				colorGradient.SetPixel(i, 0, gradientColor);
			}

			colorGradient.Apply();

			targetMaterial.SetTexture("_ColorGradient", colorGradient);
		}
	}
}
