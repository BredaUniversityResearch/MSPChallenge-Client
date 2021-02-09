using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class RasterLayer : Layer<RasterEntity>
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	class RasterRequestResponse
	{
		public string image_data = null; //base64 encoded
		public float[][] displayed_bounds = null;
	};

	public const float RASTER_VALUE_TO_ENTITY_VALUE_MULTIPLIER = 1000.0f; //Constant for converting from a raster value to the EntityType's Value field.
	private const float REFERENCE_PIXELS_PER_UNIT = 100.0f;

	public RasterObject rasterObject { get; private set; }
	private Texture2D viewingRaster;	//The raster that is displayed, reference to either rasterAtRequestedTime, or rasterAtLatestTime
	private Texture2D rasterAtRequestedTime = new Texture2D(1, 1, TextureFormat.ARGB32, false);	
	private Texture2D rasterAtLatestTime = new Texture2D(1, 1, TextureFormat.ARGB32, false);
	private int viewingRasterTime = -1; //-1 if latest
	private Vector2 scale;
	private Vector2 offset;
	private FilterMode rasterFilterMode = FilterMode.Bilinear;

	private Vector2 rasterMinWorld;
	private Vector2 rasterMaxWorld;
	public Rect RasterBounds { get; private set; }

	private List<EntityType> entityTypesSortedByValue = null;

	private static int SortMethodEntityTypesByValue(EntityType lhs, EntityType rhs)
	{
		if (lhs.value > rhs.value)
		{
			return 1;
		}
		else if (lhs.value == rhs.value)
		{
			return 0;
		}
		else
		{
			return -1;
		}
	}

	public RasterLayer(LayerMeta layerMeta/*, PlanLayer planLayer = null*/)
		: base(layerMeta/*, planLayer*/)
	{
		entityTypesSortedByValue = new List<EntityType>(EntityTypes.Values);
		entityTypesSortedByValue.Sort(SortMethodEntityTypesByValue);

		viewingRaster = rasterAtLatestTime;

		try
		{
			rasterObject = JsonConvert.DeserializeObject<RasterObject>(layerMeta.layer_raster);
		}
		catch(Exception ex)
		{
			Debug.LogError("Failed to deserialize: " + FileName + "\nException message: " + ex.Message + "\nSource Data: " + layerMeta.layer_raster);
		}
	}

	private void AddColorType(int type, Color color)
	{
		SubEntityDrawSettings drawSettings = new SubEntityDrawSettings(true, color, "Default", false, 0, 0, 0, 0, false, color, 1.0f, null, Color.white, -1, 0, false, color, 0, null);
		EntityType entityType = new EntityType("type " + type, "", 0, 0, 0, drawSettings, "");
		EntityTypes.Add(type, entityType);
        if (SetEntityTypeVisibility(entityType, true))
            SetActiveToCurrentPlanAndRedraw();
	}

	private void LoadRasterObject()
	{
		UpdateRasterBounds(rasterObject.boundingbox);
		rasterFilterMode = rasterObject.layer_raster_filter_mode;
	}

	private void UpdateRasterBounds(float[][] bounds)
	{
		if(bounds == null)
		{
			Debug.LogError("Failed to load raster: " + ShortName);
			return;
		}
		rasterMinWorld = new Vector2(bounds[0][0], bounds[0][1]);
		rasterMaxWorld = new Vector2(bounds[1][0], bounds[1][1]);
		offset = rasterMinWorld / Main.SCALE;
		RasterBounds = new Rect(rasterMinWorld.x / Main.SCALE, rasterMinWorld.y / Main.SCALE, (rasterMaxWorld.x - rasterMinWorld.x) / Main.SCALE, (rasterMaxWorld.y - rasterMinWorld.y) / Main.SCALE);
	}

	private void LoadLatestRaster()
	{
		if (rasterObject.request_from_server)
		{
			string imageURL = Server.GetRasterUrl();
			//Debug.Log("Requesting " + FileName + " at " + imageURL);
			NetworkForm form = new NetworkForm();
			form.AddField("layer_name", FileName);
			ServerCommunication.DoRequest<RasterRequestResponse>(imageURL, form, HandleImportLatestRasterCallback);
		}
	}

	public void ReloadLatestRaster()
	{
		LoadLatestRaster();
	}

	private void HandleImportLatestRasterCallback(RasterRequestResponse response)
	{
		if (!string.IsNullOrEmpty(response.image_data))
		{
			byte[] imageBytes = Convert.FromBase64String(response.image_data);
			rasterAtLatestTime.LoadImage(imageBytes);
		}

		if (response.displayed_bounds != null)
		{
			UpdateRasterBounds(response.displayed_bounds);
		}

		if (viewingRasterTime < 0)
		{
			SetRasterTexture(rasterAtLatestTime);
		}

		//else
		//{
		//	if (ServerCommunication.GetHTTPResponseCode(www) != 404)
		//	{
		//		Debug.LogError("Error in request. URL: " + www.url + ". Error: " + www.downloadHandler.text);
		//	}
		//}
	}

	private void LoadRasterAtTime(int month)
	{
		if(month == -1 || month == GameState.GetCurrentMonth())
		{
			if (viewingRasterTime == -1)
				return;
			viewingRasterTime = -1;
			SetRasterTexture(rasterAtLatestTime);
			return;
		}

		viewingRasterTime = month;
		if (rasterObject.request_from_server)
		{
			string imageURL = Server.GetRasterUrl();
			Debug.Log("Requesting " + FileName + " at " + imageURL);
			NetworkForm form = new NetworkForm();
			form.AddField("layer_name", FileName);
			form.AddField("month", month);
			ServerCommunication.DoRequest<RasterRequestResponse>(imageURL, form, response => HandleImportRasterAtTimeCallback(response, viewingRasterTime));
		}
	}

	private void HandleImportRasterAtTimeCallback(RasterRequestResponse response, int month)
	{

		if (month == viewingRasterTime)
		{
			//No longer required because the same Texture2D is being used
			//Object.Destroy(rasterAtRequestedTime);

			if (!string.IsNullOrEmpty(response.image_data))
			{
				byte[] imageBytes = Convert.FromBase64String(response.image_data);
				rasterAtRequestedTime.LoadImage(imageBytes);
			}
			SetRasterTexture(rasterAtRequestedTime);
		}
	}

	public void SetRasterTexture(Texture2D texture)
	{
		if (texture == null)
		{
			Debug.LogError("Received a null raster texture when updating layer: " + ShortName);
			return;
		}

		viewingRaster = texture;
		viewingRaster.wrapMode = TextureWrapMode.Clamp;
		viewingRaster.filterMode = rasterFilterMode;

		scale.x = (rasterMaxWorld.x - rasterMinWorld.x) / (viewingRaster.width * 10); // not sure why its *10 but it works
		scale.y = (rasterMaxWorld.y - rasterMinWorld.y) / (viewingRaster.height * 10);

		SetNewRaster(viewingRaster);
		DrawGameObjects(LayerGameObject.transform);
		SetScale(scale);
	}

	public override void LoadLayerObjects(List<SubEntityObject> layerObjects)
	{
		base.LoadLayerObjects(layerObjects);

		LoadRasterObject();

		if (layerObjects != null)
		{
			foreach (SubEntityObject layerObject in layerObjects)
			{
				RasterEntity entity = new RasterEntity(this, viewingRaster, offset, scale, layerObject);
				Entities.Add(entity);
				initialEntities.Add(entity);
			}
		}
	}

	public bool IsWithinRasterBounds(Vector2 screenPos)
	{
		Vector2 uvPos = GetTextureUVForWorldPosition(screenPos);
		return uvPos.x >= 0.0f && uvPos.x <= 1.0f && uvPos.y >= 0.0f && uvPos.y <= 1.0f;
	}

	private Vector2 GetTextureUVForWorldPosition(Vector2 screenPos)
	{
	
		Vector2 texturePos = screenPos;
		texturePos.x -= offset.x;
		texturePos.y -= offset.y;

		float fx = texturePos.x / (scale.x * viewingRaster.width / REFERENCE_PIXELS_PER_UNIT);
		float fy = texturePos.y / (scale.y * viewingRaster.height / REFERENCE_PIXELS_PER_UNIT);
		return new Vector2(fx, fy);
	}

	//Returns 0..1 range.
	public Color GetValueAt(Vector3 worldPosition)
	{
		Vector2 uvPos = GetTextureUVForWorldPosition((Vector2)worldPosition);

		int x = Mathf.FloorToInt(uvPos.x * viewingRaster.width);
		int y = Mathf.FloorToInt(uvPos.y * viewingRaster.height);

		Color value = viewingRaster.GetPixel(x, y); // maybe cache all the pixels when loading 

		return value;
	}

	//returns the intensity value in a 0..1 range.
	public float SampleIntensityAtTexturePosition(int textureX, int textureY)
	{
		return viewingRaster.GetPixel(textureX, textureY).r;
	}

	public float SampleIntensityBilinear(float u, float v)
	{
		return viewingRaster.GetPixelBilinear(u, v).r;
	}

	public EntityType GetEntityTypeForRasterAt(Vector2 worldPosition)
	{
		float rasterValue = GetValueAt(worldPosition).r * RASTER_VALUE_TO_ENTITY_VALUE_MULTIPLIER;
		return GetEntityTypeForRasterValue(rasterValue);
	}

	public EntityType GetEntityTypeForRasterAt(int rasterSpaceX, int rasterSpaceY)
	{
		float rasterValue = viewingRaster.GetPixel(rasterSpaceX, rasterSpaceY).r * RASTER_VALUE_TO_ENTITY_VALUE_MULTIPLIER;
		return GetEntityTypeForRasterValue(rasterValue);
	}

	private EntityType GetEntityTypeForRasterValue(float rasterValue)
	{
		EntityType result = entityTypesSortedByValue[0];
		for (int i = 1; i < entityTypesSortedByValue.Count; ++i)
		{
			EntityType nextType = entityTypesSortedByValue[i];
			if (nextType.value > rasterValue)
			{
				break;
			}
			result = nextType;
		}
		return result;
	}

	public List<EntityType> GetEntityTypesSortedByValue()
	{
		return entityTypesSortedByValue;
	}

	public float GetRasterColourValueAt(Vector3 screenPos)
	{
		return DenormaliseRasterColour(GetValueAt(screenPos));
	}

	//Returns 0..255 for 1..0 colour on the R channel.
	private float DenormaliseRasterColour(Color color)
	{
		return (1 - color.r) * 255.0f;
	}

	private void SetNewRaster(Texture2D newRaster)
	{
		foreach (RasterEntity re in Entities)
		{
			re.SetNewRaster(newRaster);
		}
	}

	public override Entity AddObject(SubEntityObject obj)
	{
		throw new NotImplementedException();
	}

	public override Entity GetEntity(int index)
	{
		return Entities[index];
	}

	public override Entity GetEntityAt(Vector2 position)
	{
		if (Entities != null)
		{
			if (Entities[0] != null)
			{
				return Entities[0];
			}
		}
		return null;
	}

	public override int GetEntityCount()
	{
		return Entities.Count;
	}

	public override LayerManager.GeoType GetGeoType()
	{
		return LayerManager.GeoType.raster;
	}

	public override List<SubEntity> GetSubEntitiesAt(Vector2 position)
	{
		List<SubEntity> list = new List<SubEntity>();
		SubEntity subEntity = GetSubEntityAt(position);
		if (subEntity != null)
		{
			list.Add(subEntity);
		}
		return list;
	}

	public override SubEntity GetSubEntityAt(Vector2 position)
	{
		if (IsWithinRasterBounds(position))
		{
			if (GetValueAt(position).r >= rasterObject.layer_raster_minimum_value_cutoff)
			{
				if (Entities != null && Entities[0] != null)
				{
					if (Entities[0].rasterSubentity[0] != null)
					{
						return Entities[0].rasterSubentity[0];
					}
				}
			}
		}

		return null;
	}

	//public override void MoveAllGeometryTo(Layer destination, int entityTypeOffset)
	//{
	//    throw new NotImplementedException();
	//}

	public override void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode, bool forceScaleUpdate = false)
	{
		foreach (RasterEntity re in Entities)
		{
			re.RedrawGameObjects(targetCamera, drawMode, forceScaleUpdate);
		}
	}

	//public override void TransformAllEntities(float scale, Vector3 translate)
	//{
	//    throw new NotImplementedException();
	//}

	public override void UpdateScale(Camera targetCamera)
	{

	}

	public override HashSet<Entity> GetEntitiesOfType(EntityType type)
	{
		HashSet<Entity> result = new HashSet<Entity>();

		return result;
	}

	public override HashSet<Entity> GetActiveEntitiesOfType(EntityType type)
	{
		HashSet<Entity> result = new HashSet<Entity>();
		foreach (RasterEntity ent in activeEntities)
		{
			result.Add(ent);
		}
		return result;
	}

	private void SetScale(Vector2 newScale)
	{
		foreach (RasterEntity re in Entities)
		{
			re.SetScale(newScale);
		}
	}

	public override void SetEntitiesActiveUpToTime(int month)
	{
		LoadRasterAtTime(month);
	}

	public override void SetEntitiesActiveUpTo(int index, bool showRemovedInLatestPlan = true, bool showCurrentIfNotInfluencing = true)
	{
		//Reset raster to the current one
		if (viewingRasterTime != -1)
		{
			viewingRasterTime = -1;
			viewingRaster = rasterAtLatestTime;
			SetRasterTexture(viewingRaster);
		}

		activeEntities.Clear();
		activeEntities.UnionWith(Entities);
	}

	public override void SetEntitiesActiveUpTo(Plan plan)
	{
		SetEntitiesActiveUpTo(0);
	}

	public override void SetEntitiesActiveUpToCurrentTime()
	{
		viewingRasterTime = -1;
		viewingRaster = rasterAtLatestTime;
		SetRasterTexture(viewingRaster);
		activeEntities.Clear();
		activeEntities.UnionWith(Entities);
	}

	public override bool IsIDInActiveGeometry(int ID)
	{
		return true;
	}

	public override void ActivateLastEntityWith(int persistentID)
	{ 
	}

	public int GetRasterImageWidth()
	{
		return viewingRaster.width;
	}

	public int GetRasterImageHeight()
	{
		return viewingRaster.height;
	}

	public override void UpdateVisibleIndexLayerType(int visibleIndexOfLayerType)
	{
		base.UpdateVisibleIndexLayerType(visibleIndexOfLayerType);

		for (int i = 0; i < GetEntityCount(); i++)
		{
			Entity entity = GetEntity(i);
			for (int j = 0; j < entity.GetSubEntityCount(); ++j)
			{
				RasterSubentity subEntity = (RasterSubentity)entity.GetSubEntity(j);
				subEntity.SetRenderPatternOffset(0.125f * (float)visibleIndexOfLayerType);
			}
		}
	}

	public override void LayerShown()
	{
		base.LayerShown();
		ReloadLatestRaster();
	}

	public Vector3 GetWorldPositionForTextureLocation(int pixelX, int pixelY)
	{
		Rect rasterBounds = RasterBounds;
		return new Vector3(rasterBounds.x + (rasterBounds.width * Mathf.Clamp01((float)pixelX / (float)viewingRaster.width)),
			rasterBounds.y + (rasterBounds.height * Mathf.Clamp01((float)pixelY / (float)viewingRaster.height)),
				0.0f);
	}
}

public class RasterObject
{
	public string url { get; set; }
	public bool request_from_server { get; set; }
	public float[][] boundingbox { get; set; }
	public string layer_raster_material = "RasterMELNew";
	public string layer_raster_pattern = "Default";
	public float layer_raster_minimum_value_cutoff = 0.05f;
	public ERasterColorInterpolationMode layer_raster_color_interpolation = ERasterColorInterpolationMode.Linear;
	public FilterMode layer_raster_filter_mode = FilterMode.Bilinear;

	public RasterObject()
	{
		request_from_server = true;
	}
}