using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActiveLayerEntityType : MonoBehaviour
	{
		[HideInInspector] TextMeshProUGUI m_label;
		[HideInInspector] Image m_pointKey;
		[HideInInspector] Image m_lineKey;
		[HideInInspector] RawImage m_areaKey;
		[HideInInspector] Image m_outlineKey;

		public void SetContent(AbstractLayer a_layer, EntityType a_entityType)
		{
			Texture2D pattern = MaterialManager.Instance.GetPatternOrDefault(a_entityType.DrawSettings.PolygonPatternName);

			Color color = a_entityType.DrawSettings.PolygonColor;

			if (a_layer.GetGeoType() == LayerManager.GeoType.line)
			{
				color = a_entityType.DrawSettings.LineColor;
			}
			else if (a_layer.GetGeoType() == LayerManager.GeoType.point)
			{
				color = a_entityType.DrawSettings.PointColor;
			}
			else if (a_layer.GetGeoType() == LayerManager.GeoType.raster)
			{
				pattern = MaterialManager.Instance.GetPatternOrDefault(((RasterLayer)a_layer).rasterObject.layer_raster_pattern);
			}
			int key = a_layer.GetEntityTypeKey(a_entityType);

			string mapKeyName = a_entityType.Name;
			if (SessionManager.Instance.AreWeGameMaster)
				mapKeyName += " (" + key + ")";
			m_label.text = mapKeyName;

			if (a_layer.GetGeoType() == LayerManager.GeoType.polygon)
			{
				m_areaKey.texture = pattern;
				m_areaKey.color = color;
				m_outlineKey.transform.gameObject.SetActive(true);
				Color outlineColor = a_entityType.DrawSettings.LineColor;
				outlineColor.a = 1.0f; //Force Alpha to 1 as it is with the outline line rendering.
				m_outlineKey.color = outlineColor;
			}
			else if (a_layer.GetGeoType() == LayerManager.GeoType.line)
			{
				m_areaKey.transform.parent.parent.gameObject.SetActive(false);
				m_lineKey.transform.parent.gameObject.SetActive(true);
				m_pointKey.transform.parent.gameObject.SetActive(false);
				m_outlineKey.transform.gameObject.SetActive(false);
				m_lineKey.color = color;
				m_lineKey.sprite = InterfaceCanvas.Instance.activeLayerLineSprites[(int)a_entityType.DrawSettings.LinePatternType];
			}
			else if (a_layer.GetGeoType() == LayerManager.GeoType.point)
			{
				m_areaKey.transform.parent.parent.gameObject.SetActive(false);
				m_lineKey.transform.parent.gameObject.SetActive(false);
				m_pointKey.transform.parent.gameObject.SetActive(true);
				m_outlineKey.transform.gameObject.SetActive(false);
				m_pointKey.sprite = a_entityType.DrawSettings.PointSprite;
				m_pointKey.color = color;
			}
			else
			{
				m_outlineKey.transform.gameObject.SetActive(false);
				m_areaKey.color = color;
				m_areaKey.texture = pattern;
			}
		}
	}
}