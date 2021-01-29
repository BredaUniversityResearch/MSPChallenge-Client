using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public abstract class Entity
{
	public const int INVALID_COUNTRY_ID = -1;

	public int creationBatchCallID; //ID of the PostGeometry call in the batch

    public AbstractLayer Layer { get; set; }
    public PlanLayer PlanLayer;
    private int country;

    public Dictionary<string, string> metaData;
	public Vector2 patternRandomOffset;
	public string name;

	public List<EntityType> EntityTypes { get; set; }

	public int Country
	{
		get { return country; }
		set
		{
			country = value;
			if (Layer.editingType == AbstractLayer.EditingType.SourcePolygon)
			{
				((EnergyPolygonSubEntity)GetSubEntity(0)).sourcePoint.Entity.Country = value;
			}
		}
	}

	protected Entity(AbstractLayer layer, List<EntityType> entityTypes, int countryID = INVALID_COUNTRY_ID)
    {
        Layer = layer;
        EntityTypes = entityTypes;

        metaData = new Dictionary<string, string>();
        country = countryID;

		name = "Unnamed";
		patternRandomOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

	protected Entity(AbstractLayer layer, SubEntityObject entityObject) 
		: this(layer, entityObject.GetEntityType(layer), entityObject.country)
	{
		if (entityObject.data != null && entityObject.data.ToString() != "[]")
		{
			metaData = entityObject.data;
			List<string> tNames = new List<string>() { "Name", "NAME", "name", "SITE_NAME" };

			foreach (string s in tNames)
				if (TryGetMetaData(s, out name))
					break;
		}
	}

    public abstract void RemoveGameObjects();
    public abstract void DrawGameObjects(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default);
    public abstract void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, bool forceScaleUpdate = false);
    public abstract SubEntity GetSubEntity(int index);
    public abstract int GetSubEntityCount();
    public abstract float GetRestrictionAreaSurface();

    public virtual float GetInvestmentCost()
    {
        string cost;
        if (TryGetMetaData("levelized_cost_of_energy", out cost))
        {
            return float.Parse(cost, Localisation.NumberFormatting);
        }
        return 0;
    }

    public void ReplaceMetaData(Dictionary<string, string> newMetadata)
    {
        metaData = newMetadata;
    }

    public List<int> GetEntityTypeKeys()
    {
        List<int> types = new List<int>();

        foreach (EntityType type in EntityTypes)
        {
            types.Add(Layer.GetEntityTypeKey(type));
        }

        return types;
    }

	public string GetMetaData(string key)
	{
		return  metaData[key];
	}

	public bool TryGetMetaData(string key, out string value)
    {
        if (DoesPropertyExist(key))
        {
			value = metaData[key];
            return true;
        }
		value = "";
        return false;
    }

    public void SetMetaData(string key, string value)
    {
        if (DoesPropertyExist(key))
        {
            metaData[key] = value;
        }
        else
        {
            metaData.Add(key, value);
        }
    }

    public bool DoesPropertyExist(string key)
    {
        return metaData.ContainsKey(key);
    }

	public void SetPropertyMetaData(EntityPropertyMetaData property, string value)
	{
		SetMetaData(property.PropertyName, value);
        if (property.UpateCalculation)
            GetSubEntity(0).CalculationPropertyUpdated();
        if(property.UpateText)
            GetSubEntity(0).UpdateTextMeshText();
		if (property.UpateVisuals)
			RedrawGameObjects(CameraManager.Instance.gameCamera);
    }

	public string GetPropertyMetaData(EntityPropertyMetaData property)
	{
		if (DoesPropertyExist(property.PropertyName))
			return GetMetaData(property.PropertyName);
		return property.DefaultValue;
	}

	public void SetPropertyName(string name)
    {
        SetMetaData("Name", name);
    }

    public string MetaToJSON()
    {
		if (metaData.Count == 0)
        {
            return "{}";
        }       
        return JsonConvert.SerializeObject(metaData);
    }

    public Rect GetEntityBounds()
    {
        int subEntityCount = GetSubEntityCount();

        if (subEntityCount == 0) { return new Rect(); }

        Rect result = GetSubEntity(0).BoundingBox;
        for (int i = 1; i < subEntityCount; ++i)
        {
            Vector2 min = Vector2.Min(result.min, GetSubEntity(i).BoundingBox.min);
            Vector2 max = Vector2.Max(result.max, GetSubEntity(i).BoundingBox.max);
            result = new Rect(min, max - min);
        }

        return result;
    }

    public void MovedToLayer(AbstractLayer newLayer, int newEntityTypeKey)
    {
        Layer = newLayer;

        EntityTypes = new List<EntityType>() { Layer.GetEntityTypeByKey(newEntityTypeKey) };

        for (int i = 0; i < GetSubEntityCount(); ++i)
        {
            GetSubEntity(i).RemoveGameObject();
        }
    }

    public void UpdateGameObjectsForEveryLOD()
    {
        int count = GetSubEntityCount();
        for (int i = 0; i < count; ++i)
        {
            GetSubEntity(i).UpdateGameObjectForEveryLOD();
        }
    }

    public bool GreenEnergy
    {
        get { return Layer.greenEnergy; }
    }

    public int DatabaseID
    {
        get { return GetSubEntity(0).GetDatabaseID(); }
    }
    public int PersistentID
    {
        get { return GetSubEntity(0).GetPersistentID(); }
    }

	//returns the restriction size of this entity for the respective owner country and the current time.
	public float GetCurrentRestrictionSize()
    {
		return RestrictionAreaManager.instance.GetRestrictionAreaSizeAtPlanTime(PlanManager.planViewing, EntityTypes[0], country);
    }

	/// <summary>
	/// Callback which allows overriding of the current draw settings from a sub-entity on an entity level. 
	/// When modifying draw settings in here make sure to not change the original draw settings but set it to a clone 
	/// otherwise the draw settings will be saved as the default for that entity type.
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="drawMode">The current drawmode we need to override settings for.</param>
	/// <param name="meshDirtyFromOverride">Set to true if you want to treat the mesh as dirty</param>
	public virtual void OverrideDrawSettings(SubEntityDrawMode drawMode, ref SubEntityDrawSettings settings, ref bool meshDirtyFromOverride)
	{
	}
}