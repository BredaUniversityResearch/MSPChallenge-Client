using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class PropertiesWindow : MonoBehaviour
	{
		private const float PROPERTY_WINDOW_WIDTH = 300.0f;
		private const float PROPERTY_WINDOW_DEFAULT_HEIGHT = 150.0f;
	
		public GenericWindow window;
		public TextMeshProUGUI windowName;
		public Image layerIcon;

		[Header("Icons")]
		public Sprite countryIcon;
		public Sprite gridIcon, capacityIcon, usedCapacityIcon, locationIcon, areaIcon, pointsIcon;

		[Header("Content locations")]
		public GenericContent baseDataParent;
		public GenericContent entityTypeParent, otherInfoParent, debugInfoParent;

		private List<GenericEntry> genericEntries;

		[SerializeField]
		private ValueConversionCollection valueConversionCollection = null;

		public void ShowPropertiesWindow(SubEntity subEntity, Vector3 worldSamplePosition, Vector3 windowPosition)
		{
			bool reposition = !gameObject.activeSelf;
			gameObject.SetActive(true);

			//Setup data
			Entity entity = subEntity.m_entity;
			windowName.text = entity.Layer.GetShortName();
			layerIcon.sprite = LayerManager.Instance.GetSubcategoryIcon(entity.Layer.m_subCategory);

			//Base data
			baseDataParent.Initialise();
			baseDataParent.DestroyAllContent();

			Team team = SessionManager.Instance.FindTeamByID(entity.Country);
			baseDataParent.CreateEntry("Name", entity.name, countryIcon, team == null ? Color.white : team.color);
			baseDataParent.CreateEntry("X", (subEntity.m_boundingBox.center.x * 1000).FormatAsCoordinateText(), locationIcon, Color.white);
			baseDataParent.CreateEntry("Y", (subEntity.m_boundingBox.center.y * 1000).FormatAsCoordinateText(), locationIcon, Color.white);

			//Geometry type specific base data
			if (entity.Layer.GetGeoType() == LayerManager.EGeoType.Polygon)
			{
				PolygonSubEntity polygonEntity = (PolygonSubEntity)subEntity;
				baseDataParent.CreateEntry("Area", polygonEntity.SurfaceAreaSqrKm.ToString("0.00") + " km<sup>2</sup>", areaIcon, Color.white);
				baseDataParent.CreateEntry("Points", polygonEntity.GetTotalPointCount().ToString(), pointsIcon, Color.white);
			}
			else if (entity.Layer.GetGeoType() == LayerManager.EGeoType.Line)
			{
				LineStringSubEntity lineEntity = (LineStringSubEntity)subEntity;
				baseDataParent.CreateEntry("Length", lineEntity.LineLengthKm.ToString("0.00") + " km", areaIcon, Color.white);
				baseDataParent.CreateEntry("Points", lineEntity.GetPointCount().ToString(), pointsIcon, Color.white);
			}
			if (entity.Layer.IsEnergyLayer())
			{
				IEnergyDataHolder data = (IEnergyDataHolder)subEntity;
				baseDataParent.CreateEntry("Max Capacity", valueConversionCollection.ConvertUnit(data.Capacity, ValueConversionCollection.UNIT_WATT).FormatAsString(), capacityIcon, Color.white);
				baseDataParent.CreateEntry("Used Capacity", valueConversionCollection.ConvertUnit(data.UsedCapacity, ValueConversionCollection.UNIT_WATT).FormatAsString(), usedCapacityIcon, Color.white);
				if(data.CurrentGrid == null)
					baseDataParent.CreateEntry("Last Run Grid", data.LastRunGrid == null ? "-" : data.LastRunGrid.m_name, gridIcon, Color.white);
				else
					baseDataParent.CreateEntry("Current Grid", data.CurrentGrid.m_name, gridIcon, Color.white);
			}

			//Entity type information
			entityTypeParent.DestroyAllContent();
			entityTypeParent.Initialise();
			List<EntityType> entityTypes;
			RasterLayer rasterLayer = entity.Layer as RasterLayer;
			EntityType entityType = null;
			float? rasterValue = null;
			if (rasterLayer != null)
			{
				rasterValue = rasterLayer.GetRasterValueAt(worldSamplePosition);
				if (rasterValue != null)
				{
					entityType = rasterLayer.GetEntityTypeForRasterValue(rasterValue.Value);
				}
			}
			if (entityType != null)
			{
				entityTypes = new List<EntityType>(1) {entityType};
			}
			else
			{
				entityTypes = entity.EntityTypes;
			}
			for (int i = 0; i < entityTypes.Count; i++)
			{
				string entryContent = Main.IsDeveloper ? entityTypes[i].value.ToString() : "";
				//string URL = "http://www.google.com/search?q=" + entity.EntityTypes[i].Name.Replace(' ', '+');
				if (!string.IsNullOrEmpty(entityTypes[i].media))
				{
					var iCopy = i;
						entityTypeParent.CreateEntry(
						entityTypes[i].Name,
						entryContent,
						() =>
						{
							Vector3[] corners = new Vector3[4];
							window.windowTransform.GetWorldCorners(corners);

							string mediaUrl = MediaUrl.Parse(entityTypes[iCopy].media);
							InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(mediaUrl);
						}
					);
				}
				else
				{
					entityTypeParent.CreateEntry(entityTypes[i].Name, entryContent);
				}
			}

			//Other information
			otherInfoParent.DestroyAllContent();
			otherInfoParent.Initialise();
			if (entity.metaData.Count > 0)
			{
				otherInfoParent.gameObject.SetActive(true);

				foreach (var kvp in entity.metaData)
				{
					EntityPropertyMetaData propertyMeta = entity.Layer.FindPropertyMetaDataByName(kvp.Key);
					if ((propertyMeta == null || propertyMeta.Enabled) || Main.IsDeveloper)
					{
						string propertyDisplayName = (propertyMeta != null && !string.IsNullOrEmpty(propertyMeta.DisplayName)) ? propertyMeta.DisplayName : kvp.Key;
						string value = propertyMeta != null && !string.IsNullOrEmpty(propertyMeta.Unit) ? entity.GetMetaData(kvp.Key) + " " + propertyMeta.Unit : entity.GetMetaData(kvp.Key);
						otherInfoParent.CreateEntry(propertyDisplayName, value);
					}
				}
			}
			else
				otherInfoParent.gameObject.SetActive(false);

			//Debug information
			if (Main.IsDeveloper)
			{
				debugInfoParent.gameObject.SetActive(true);

				//GameObject contentContainer = debugInfoParent.transform.GetChild(1).gameObject;
				debugInfoParent.DestroyAllContent();
				debugInfoParent.Initialise();
				debugInfoParent.CreateEntry("MSP ID", subEntity.GetMspID());
				debugInfoParent.CreateEntry("Persistent ID", subEntity.GetPersistentID().ToString());
				debugInfoParent.CreateEntry("Database ID", subEntity.GetDatabaseID().ToString());
				if (rasterValue != null)
				{
					debugInfoParent.CreateEntry("Raster value", rasterValue.ToString());	
				}
			}
			else
				debugInfoParent.gameObject.SetActive(false);

			//Update window position
			if (reposition)
			{
				StartCoroutine(RepositionOnFrameEnd(windowPosition));
			}

			window.transform.SetAsLastSibling();
		}

		IEnumerator RepositionOnFrameEnd(Vector3 position)
		{
			yield return new WaitForEndOfFrame();
			window.SetPosition(new Vector2(position.x, position.y));
		}

		public void Close()
		{
			gameObject.SetActive(false);
		}
	}
}
