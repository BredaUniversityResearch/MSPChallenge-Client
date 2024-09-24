using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using UnityEngine;

namespace MSP2050.Scripts
{
	public static class LayerInfo
	{
		public static List<LayerMeta> Load(List<LayerMeta> a_layerMeta)
		{
			for (int i = 0; i < a_layerMeta.Count; i++)
			{
				AbstractLayer layer;

				// This means its a plan, dont load those here
				if (!string.IsNullOrEmpty(a_layerMeta[i].layer_original_id))
				{
					continue;
				}

				if (a_layerMeta[i].layer_geotype == "polygon")
				{
					if (a_layerMeta[i].layer_editing_type == "sourcepolygon")
						layer = new EnergyPolygonLayer(a_layerMeta[i], new List<SubEntityObject>());
					else
						layer = new PolygonLayer(a_layerMeta[i], new List<SubEntityObject>());
				}
				else if (a_layerMeta[i].layer_geotype == "line")
				{
					if (a_layerMeta[i].layer_special_entity_type == ELayerSpecialEntityType.ShippingLine)
					{
						layer = new ShippingLineStringLayer(a_layerMeta[i], new List<SubEntityObject>());
					}
					else
					{
						layer = new LineStringLayer(a_layerMeta[i], new List<SubEntityObject>());
					}
				}
				else if (a_layerMeta[i].layer_geotype == "point")
				{
					layer = new PointLayer(a_layerMeta[i], new List<SubEntityObject>());
				}
				else if (a_layerMeta[i].layer_geotype == "raster")
				{
					layer = new RasterLayer(a_layerMeta[i]);
				}
				else
				{
					layer = new PolygonLayer(a_layerMeta[i], new List<SubEntityObject>());
					Debug.LogError("Layer has invalid geotype: " + a_layerMeta[i].layer_geotype + " in layer " + layer.FileName);
				}
				layer.m_versionNr = a_layerMeta[i].layer_filecreationtime;
				LayerManager.Instance.AddLayer(layer);
			}
			//Load dependencies after all layers have been added to the layer manager
			foreach(LayerMeta meta in a_layerMeta)
			{
				LayerManager.Instance.GetLayerByID(meta.layer_id).LoadDependencies(meta);
			}

			return a_layerMeta;
		}
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class EntityTypeValues
	{
		public string displayName { get; set; }
		public bool displayPolygon { get; set; }
		public string polygonColor { get; set; }
		public string polygonPatternName { get; set; }
		public bool innerGlowEnabled { get; set; }
		public int innerGlowRadius { get; set; }
		public int innerGlowIterations { get; set; }
		public float innerGlowMultiplier { get; set; }
		public float innerGlowPixelSize { get; set; }
		public bool displayLines { get; set; }
		public string lineColor { get; set; }
		public float lineWidth { get; set; }
		public string lineIcon { get; set; }
		public ELinePatternType linePatternType { get; set; }
		public bool displayPoints { get; set; }
		public string pointColor { get; set; }
		public float pointSize { get; set; }
		public string pointSpriteName { get; set; }

		public string description { get; set; }
		public long capacity { get; set; }
		public float investmentCost { get; set; }
		public int availability { get; set; }
		public int value { get; set; }
		public string media { get; set; }
		public EApprovalType approval { get; set; } //required approval

		public EntityTypeValues()
		{
			lineWidth = 1.0f;
		}
	}
	
	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class LayerMeta
	{
		public LayerMeta()
		{
			layer_id = 0;
			layer_original_id = null;
			layer_depth = "0";
			layer_name = "test_layer";
			layer_geotype = "polygon";
			layer_short = "Test Layer";
			layer_media = null;
			layer_group = "";
			layer_tooltip = "";
			layer_sub = "";
			layer_icon = "";
			layer_info_properties = new LayerInfoPropertiesObject[0];
			layer_text_info = null;
			layer_type = new Dictionary<int, EntityTypeValues>();
			layer_category = "Test Category";
			layer_subcategory = "aquaculture";
			layer_active = "1";
			layer_selectable = true;
			layer_kpi_category = ELayerKPICategory.Miscellaneous;
			layer_editable = true;
			layer_toggleable = true;
			layer_active_on_start = false;
			layer_states = "";
			layer_editing_type = "";
			layer_special_entity_type = ELayerSpecialEntityType.Default;
			layer_filecreationtime = -1;
			layer_entity_value_max = null;
		}

		public int layer_id { get; set; }
		public string layer_original_id { get; set; }
		public string layer_depth { get; set; }
		public string layer_tags { get; set; }
		public string layer_name { get; set; }
		public string layer_geotype { get; set; }
		public string layer_short { get; set; }
		public string layer_media { get; set; }
		public string layer_group { get; set; }
		public string layer_tooltip { get; set; }
		public string layer_sub { get; set; }
		public string layer_icon { get; set; }
		public LayerInfoPropertiesObject[] layer_info_properties { get; set; } 
		public LayerTextInfoObject layer_text_info { get; set; }
		[JsonConverter(typeof(JsonConverterLayerType))]
		public Dictionary<int, EntityTypeValues> layer_type { get; set; }
		public int[] layer_dependencies { get; set; }
		public string layer_category { get; set; }
		public string layer_subcategory { get; set; }
		public ELayerKPICategory layer_kpi_category { get; set; }
		public string layer_active { get; set; }
		public bool layer_selectable { get; set; }
		public bool layer_editable { get; set; }
		public bool layer_toggleable { get; set; }
		public bool layer_active_on_start { get; set; }
		public string layer_states { get; set; }
		public GeometryParameterObject[] layer_geometry_parameters { get; set; }	
		public string layer_raster { get; set; }
		public string layer_editing_type { get; set; }
		public ELayerSpecialEntityType layer_special_entity_type { get; set; }
		public int layer_green { get; set; }
		public int layer_filecreationtime { get; set; }
		public float? layer_entity_value_max { get; set; }
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class LayerStateObject
	{
		public string state { get; set; }
		public int time { get; set; }
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class LayerInfoPropertiesObject
	{
		public enum ContentValidation { None, ShippingWidth, NumberCables, PitExtractionDepth }

		public string property_name { get; set; }
		public bool enabled { get; set; }
		public bool editable { get; set; }
		public string display_name { get; set; }
		public string sprite_name { get; set; }
		public string default_value { get; set; }
		public string policy_type { get; set; }
		public bool update_visuals { get; set; }
		public bool update_text { get; set; }
		public bool update_calculation { get; set; }
		public TMPro.TMP_InputField.ContentType content_type { get; set; }
		public ContentValidation content_validation { get; set; }
		public string unit { get; set; }
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class GeometryParameterObject
	{
		public string meta_name { get; set; }
		public string display_name { get; set; }
		public string sprite_name { get; set; }
		public int update_visuals { get; set; }
	}
}