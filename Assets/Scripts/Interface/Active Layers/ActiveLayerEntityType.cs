using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActiveLayerEntityType : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_label;
		[SerializeField] Image m_background;
		[SerializeField] Image m_pointKey;
		[SerializeField] Image m_lineKey;
		[SerializeField] RawImage m_areaKey;
		[SerializeField] Image m_outlineKey;

		public void SetContent(AbstractLayer a_layer, EntityType a_entityType)
		{
			Texture2D pattern = MaterialManager.Instance.GetPatternOrDefault(a_entityType.DrawSettings.PolygonPatternName);

			Color color = a_entityType.DrawSettings.PolygonColor;

			if (a_layer.GetGeoType() == LayerManager.EGeoType.line)
			{
				color = a_entityType.DrawSettings.LineColor;
			}
			else if (a_layer.GetGeoType() == LayerManager.EGeoType.point)
			{
				color = a_entityType.DrawSettings.PointColor;
			}
			else if (a_layer.GetGeoType() == LayerManager.EGeoType.raster)
			{
				pattern = MaterialManager.Instance.GetPatternOrDefault(((RasterLayer)a_layer).rasterObject.layer_raster_pattern);
			}
			int key = a_layer.GetEntityTypeKey(a_entityType);

			string mapKeyName = a_entityType.Name;
			if (SessionManager.Instance.AreWeGameMaster)
				mapKeyName += " (" + key + ")";
			m_label.text = mapKeyName;

			if (a_layer.GetGeoType() == LayerManager.EGeoType.polygon)
			{
				//m_background.gameObject.SetActive(true);
				m_pointKey.gameObject.SetActive(false);
				m_lineKey.gameObject.SetActive(false);
				m_areaKey.gameObject.SetActive(true);
				m_outlineKey.gameObject.SetActive(true);
				m_areaKey.texture = pattern;
				m_areaKey.color = color;
				Color outlineColor = a_entityType.DrawSettings.LineColor;
				outlineColor.a = 1.0f; //Force Alpha to 1 as it is with the outline line rendering.
				m_outlineKey.color = outlineColor;
			}
			else if (a_layer.GetGeoType() == LayerManager.EGeoType.line)
			{
				//m_background.gameObject.SetActive(true);
				m_areaKey.gameObject.SetActive(false);
				m_lineKey.gameObject.SetActive(true);
				m_pointKey.gameObject.SetActive(false);
				m_outlineKey.gameObject.SetActive(false);
				m_lineKey.color = color;
				m_lineKey.sprite = InterfaceCanvas.Instance.activeLayerLineSprites[(int)a_entityType.DrawSettings.LinePatternType];
			}
			else if (a_layer.GetGeoType() == LayerManager.EGeoType.point)
			{
				//m_background.gameObject.SetActive(false);
				m_areaKey.gameObject.SetActive(false);
				m_lineKey.gameObject.SetActive(false);
				m_pointKey.gameObject.SetActive(true);
				m_outlineKey.gameObject.SetActive(false);
				m_pointKey.sprite = a_entityType.DrawSettings.PointSprite;
				m_pointKey.color = color;
			}
			else
			{
				//m_background.gameObject.SetActive(true);
				m_pointKey.gameObject.SetActive(false);
				m_lineKey.gameObject.SetActive(false);
				m_areaKey.gameObject.SetActive(true);
				m_outlineKey.gameObject.SetActive(false);
				m_areaKey.color = color;
				m_areaKey.texture = pattern;
			}
		}
	}
}