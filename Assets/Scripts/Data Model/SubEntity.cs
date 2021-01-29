using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GeoJSON.Net.Feature;
using UnityEngine.Networking;

public enum SubEntityDrawMode { Default, BeingCreated, Hover, Selected, SetOperationSubject, SetOperationClip, Invalid, BeingCreatedInvalid, PlanReference };
public enum SubEntityPlanState { NotShown, NotInPlan, Added, Removed, Moved }

public abstract class SubEntity
{
	private const float layerTypeOrderMaxZOffset = 0.05f;
    private const float frontOfLayerZOffset = -0.90f;
	private const float frontOfEverythingZOffset = -100.0f;

	public Entity Entity;
	protected int databaseID;
	protected int persistentID;
	protected int mspID;

	protected GameObject gameObject;
    protected SubEntityDrawSettings drawSettings;
    public SubEntityDrawSettings DrawSettings { get { return drawSettings; } }
    public SubEntityPlanState planState { get; protected set; }
	protected SubEntityPlanState previousPlanState { get; private set; } //Temporary until we can move the planState checks out of the RedrawGameObject function
    public bool edited; //Has this subentity been altered in the current editing session. Only set for polygons.

	public Rect BoundingBox;

	public bool SnappingToThisEnabled { get; set; }

	public float Order = 0;

	public bool restrictionNeedsUpdate = false;
	protected bool restrictionHidden = false;
	private float currentRestrictionSize = 0.0f;

	protected EntityInfoText textMesh = null;
    protected bool textMeshVisibleAtZoom = true;

	public delegate void SubEntityVisiblityChangedCallback(SubEntity entity, AbstractLayer layer, bool newVisibility);
	public static event SubEntityVisiblityChangedCallback OnEntityVisibilityChanged;

	public SubEntity(Entity entity, int databaseID = -1, int persistentID = -1, int mspID = -1)
	{
		Entity = entity;
		this.databaseID = databaseID;
		this.persistentID = persistentID;
		this.mspID = mspID;
	}

	public abstract void DrawGameObject(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null);

	public virtual void RedrawGameObject(SubEntityDrawMode drawMode = SubEntityDrawMode.Default, HashSet<int> selectedPoints = null, HashSet<int> hoverPoints = null, bool updatePlanState = true)
	{
        if(updatePlanState)
		    UpdatePlanState();

		float restrictionSize = Entity.GetCurrentRestrictionSize();
		if (restrictionNeedsUpdate || restrictionSize != currentRestrictionSize)
		{
			UpdateRestrictionArea(restrictionSize);
		}

		if (textMesh != null)
		{
			textMesh.UpdateTextMeshText();
			textMesh.SetBackgroundVisibility(drawMode == SubEntityDrawMode.Hover || drawMode == SubEntityDrawMode.Selected);
		}
	}

	public abstract void UpdateGameObjectForEveryLOD();
	public abstract Vector3 GetPointClosestTo(Vector3 position);
	protected abstract void UpdateBoundingBox();
	public abstract void SetOrderBasedOnType();
	public abstract SubEntityObject GetLayerObject();
	public abstract void UpdateGeometry(GeometryObject geo);
    public abstract void SetDataToObject(SubEntityObject subEntityObject);

	protected void calculateOrderBasedOnType()
	{
		// Because keys can have gaps in them
		List<int> entityTypeKeysOrdered = new List<int>();

		foreach (var kvp in Entity.Layer.EntityTypes)
		{
			entityTypeKeysOrdered.Add(kvp.Key);
		}

		entityTypeKeysOrdered.Sort(); // smallest first
		entityTypeKeysOrdered.Reverse();

		int currentEntityTypeKey = Entity.Layer.GetEntityTypeKey(Entity.EntityTypes[0]);

		Order = ((float)entityTypeKeysOrdered.IndexOf(currentEntityTypeKey) / (float)Entity.Layer.EntityTypes.Count) * layerTypeOrderMaxZOffset;
	}


	public GameObject GetGameObject()
	{
		return gameObject;
	}

	public virtual void RemoveGameObject()
	{
		if (textMesh != null)
		{
			textMesh.Destroy();
			textMesh = null;
		}

		GameObject.Destroy(gameObject);
		gameObject = null;
	}

	public virtual void SetDatabaseID(int databaseID)
	{
		this.databaseID = databaseID;
	}

	public virtual void SetPersistentID(int persistentID)
	{
		this.persistentID = persistentID;
	}

	public bool HasDatabaseID()
	{
		return databaseID != -1;
	}

	public virtual int GetDatabaseID()
	{
		return databaseID;
	}

	public virtual string GetDataBaseOrBatchIDReference()
	{
		if (HasDatabaseID())
			return databaseID.ToString();
		else
			return BatchRequest.FormatCallIDReference(Entity.creationBatchCallID);
	}

	public virtual int GetPersistentID()
	{
		return persistentID;
	}

	public int GetMspID()
	{
		return mspID;
	}

	public bool IsPlannedForRemoval()
	{
		return planState == SubEntityPlanState.Removed;
	}

	public bool IsNotShownInPlan()
	{
		return planState == SubEntityPlanState.NotShown;
	}

	public bool IsNotAffectedByPlan()
	{
		return planState == SubEntityPlanState.NotInPlan;
	}

	//public string ToJSON()
	//{
	//	return JsonConvert.SerializeObject(GetLayerObject().geometry);
	//}

	public virtual void SubmitUpdate(BatchRequest batch)
	{
		JObject dataObject = new JObject();
		if (this is PolygonSubEntity && (this as PolygonSubEntity).GetHoleCount() > 0)
		{
			//Delete the geometry and create a new one
			SubmitDelete(batch);
			SubmitNew(batch);
		}
		else
		{
			dataObject.Add("geometry", JsonConvert.SerializeObject(GetLayerObject().geometry));
			//dataObject.Add("geometry", JToken.FromObject(GetLayerObject().geometry));
			dataObject.Add("country", Entity.Country);
			//if (Entity.PlanLayer != null)
			//{
			//	dataObject.Add("layer", Entity.PlanLayer.ID);
			//}
			//else
			//{
			//	dataObject.Add("layer", Entity.Layer.ID);
			//}
			//if (persistentID != -1)
			//	dataObject.Add("persistent", persistentID);
			dataObject.Add("id", databaseID);

			batch.AddRequest<int>(Server.UpdateGeometry(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_UPDATE, handleDatabaseIDResult);
			SubmitData(batch);
		}
	}

	public virtual void SubmitDelete(BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", databaseID);
		batch.AddRequest(Server.DeleteGeometry(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DELETE);
	}
	
	public virtual void SubmitData(BatchRequest batch)
	{
		JObject dataObject = new JObject();

		dataObject.Add("id", GetDataBaseOrBatchIDReference());
		dataObject.Add("data", Entity.MetaToJSON());
		dataObject.Add("type", Util.IntListToString(Entity.GetEntityTypeKeys()));

		batch.AddRequest(Server.SendGeometryData(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
	}

	public virtual void SubmitNew(BatchRequest batch)
	{
		JObject dataObject = new JObject();

		dataObject.Add("geometry", JsonConvert.SerializeObject(GetLayerObject().geometry));
		//dataObject.Add("geometry", JToken.FromObject(GetLayerObject().geometry));
		dataObject.Add("country", Entity.Country);

		if (persistentID != -1)
			dataObject.Add("persistent", persistentID);

		if (Entity.PlanLayer != null)
		{
			dataObject.Add("layer", Entity.PlanLayer.ID);
			dataObject.Add("plan", Entity.PlanLayer.Plan.ID);
		}
		else
		{
			dataObject.Add("plan", "");
			dataObject.Add("layer", Entity.Layer.ID);
		}

		Entity.creationBatchCallID = batch.AddRequest<int>(Server.PostGeometry(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_ADD, handleDatabaseIDResult);

		if (this is PolygonSubEntity)
		{
			int total = ((PolygonSubEntity)this).GetHoleCount();

			for (int i = 0; i < total; i++)
			{
				dataObject = new JObject();
				dataObject.Add("geometry", ((PolygonSubEntity)this).HolesToJSON(i));
				if (Entity.PlanLayer != null)
				{
					dataObject.Add("layer", Entity.PlanLayer.ID);
					dataObject.Add("plan", Entity.PlanLayer.Plan.ID);
				}
				else
				{
					dataObject.Add("plan", "");
					dataObject.Add("layer", Entity.Layer.ID);
				}
				dataObject.Add("subtractive", BatchRequest.FormatCallIDReference(Entity.creationBatchCallID)); 

				batch.AddRequest(Server.PostGeometrySub(), dataObject, BatchRequest.BATCH_GROUP_GEOMETRY_DATA);
			}
		}
		SubmitData(batch);
	}

	protected virtual void handleDatabaseIDResult(int result)
	{
		SetDatabaseID(result);
		if (GetPersistentID() == -1)
			SetPersistentID(result);
	}
	
	public virtual void ForceGameObjectVisibility(bool value)
	{
		gameObject.SetActive(value);
	}
		
	protected void objectVisibility(GameObject gameObject, float distance, Camera targetCamera)
	{
		if (distance > targetCamera.orthographicSize)
		{
			gameObject.SetActive(true);
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	protected int getImportance(string entityPropertyName, int multiplier, bool oneIsLargest = false)
	{
		int importance = int.MaxValue;

		if (Entity.DoesPropertyExist(entityPropertyName))
		{
			importance = Util.ParseToInt(Entity.GetMetaData(entityPropertyName), int.MaxValue);

			if (oneIsLargest)
			{
				importance -= 10;
			}

			importance = (int)Mathf.Abs((float)importance);

			importance *= multiplier;
		}

		return importance;
	}

	protected bool IsSnapToDrawMode(SubEntityDrawMode drawMode)
	{
		return drawMode != SubEntityDrawMode.BeingCreated &&
			   drawMode != SubEntityDrawMode.BeingCreatedInvalid &&
			   drawMode != SubEntityDrawMode.Selected;
	}

	/// <summary>
	/// Excepts the restriction area to be updated afterwards
	/// </summary>
	public void UnHideRestrictionArea(bool forceUpdate = false)
	{
		restrictionHidden = false;
		if (forceUpdate)
		{
			UpdateRestrictionArea(Entity.GetCurrentRestrictionSize());
		}
	}

	public virtual void CreateTextMesh(Transform parent, Vector3 textPosition, bool setWorldPosition = false)
	{
		if (textMesh == null)
		{
			textMesh = new EntityInfoText(this, Entity.Layer.textInfo, parent);
			textMesh.SetPosition(textPosition, setWorldPosition);
		}
    
        ScaleTextMesh();
    }

    public bool TextMeshVisibleAtZoom
    {
        get { return textMeshVisibleAtZoom; }
        set
        {
            if (textMeshVisibleAtZoom != value)
            {
                textMeshVisibleAtZoom = value;
                if (value && Entity.Layer.LayerTextVisible)
                    SetTextMeshActivity(true);
                else
                    SetTextMeshActivity(false);
            }
        }
    }

    public void SetTextMeshActivity(bool active)
	{
		if (textMesh != null)
			textMesh.SetVisibility(active);
    }

    protected void ScaleTextMesh(float parentScale = 1f)
    {
		if (Entity.Layer.textInfo == null)
		{
			return;
		}

		if (TextMeshVisibleAtZoom)
        {
            if (CameraManager.Instance.cameraZoom.currentZoom > Entity.Layer.textInfo.zoomCutoff)
                TextMeshVisibleAtZoom = false;
			else
			{
				if (textMesh != null)
				{
					textMesh.UpdateTextMeshScale(Entity.Layer.textInfo.UseInverseScale, parentScale);
				}
			}
		}
        else
        {
            if (CameraManager.Instance.cameraZoom.currentZoom <= Entity.Layer.textInfo.zoomCutoff)
            {
                TextMeshVisibleAtZoom = true;
				if (textMesh != null)
				{
					textMesh.UpdateTextMeshScale(Entity.Layer.textInfo.UseInverseScale, parentScale);
				}
			}
        }
    }

	public void UpdateTextMeshText()
	{
		if (textMesh != null)
		{
			textMesh.UpdateTextMeshText();
		}
	}

	protected virtual void UpdatePlanState()
	{
		previousPlanState = planState;
		planState = PlanManager.GetSubEntityPlanState(this);

		if (gameObject != null)
		{
			if (planState == SubEntityPlanState.NotShown)
			{
				gameObject.SetActive(false);
				NotifySubEntityVisibilityChanged();
			}
			else if (previousPlanState == SubEntityPlanState.NotShown)
			{
				gameObject.SetActive(true);
				NotifySubEntityVisibilityChanged();
			}
		}
	}

    public void SetPlanState(SubEntityPlanState newState)
    {
        previousPlanState = planState;
        planState = newState;
        if (gameObject != null)
        {
            if (planState == SubEntityPlanState.NotShown)
            {
                gameObject.SetActive(false);
                NotifySubEntityVisibilityChanged();
            }
            else if (previousPlanState == SubEntityPlanState.NotShown)
            {
                gameObject.SetActive(true);
                NotifySubEntityVisibilityChanged();
            }
        }
    }

	public void NotifySubEntityVisibilityChanged()
	{
		bool newVisibility = gameObject != null && gameObject.activeInHierarchy;
		if (OnEntityVisibilityChanged != null)
		{
			OnEntityVisibilityChanged.Invoke(this, Entity.Layer, newVisibility);
		}
	}

	public SubEntityDataCopy GetDataCopy()
	{
		return new SubEntityDataCopy(
			new List<EntityType>(Entity.EntityTypes),
			new List<Vector3>(GetPoints()),
			new Dictionary<string, string>(Entity.metaData),
			GetHoles(true),
			Entity.Country,
            edited
			);
	}

    public void SetInFrontOfLayer(bool isInFrontOfLayer)
    {
        if (gameObject == null)
            return;
        Vector3 oldPos = gameObject.transform.position;
		if (isInFrontOfLayer)
		{
			gameObject.transform.localPosition = new Vector3(oldPos.x, oldPos.y, Order + frontOfLayerZOffset);
			if (textMesh != null)
			{
				textMesh.SetZOffset(frontOfEverythingZOffset);
			}
		}
		else
		{
			gameObject.transform.localPosition = new Vector3(oldPos.x, oldPos.y, Order);
			if (textMesh != null)
			{
				textMesh.SetZOffset(0.0f);
			}
		}
	}

	public virtual void SetDataToCopy(SubEntityDataCopy copy)
	{
		SetPoints(copy.pointsCopy);
		SetHoles(copy.holesCopy);
		Entity.EntityTypes = copy.entityTypeCopy;
		Entity.metaData = copy.metaDataCopy;
		Entity.Country = copy.country;
        edited = copy.edited;
	}

    public virtual void FinishEditing()
    {
        edited = false;
    }

    public string GetProperty(string key)
    {
        if (Entity.Layer.presetProperties.ContainsKey(key))
        {
            return Entity.Layer.presetProperties[key](this);
        }
        else
        {
            string result;
            Entity.TryGetMetaData(key, out result);
            return result;
        }
    }
    public virtual void UpdateScale(Camera targetCamera)
    { }
    public virtual void SetPoints(List<Vector3> points)
	{ }
	public virtual void SetHoles(List<List<Vector3>> holes)
	{ }
	public virtual List<Vector3> GetPoints()
	{ return null; }
	public virtual List<List<Vector3>> GetHoles(bool copy = false)
	{ return null; }

    public abstract Feature GetGeoJSONFeature(int idToUse);

    public virtual void SetPropertiesToGeoJSONFeature(Feature feature)
    {
        foreach (var kvp in feature.Properties)
        {
            Entity.SetMetaData(kvp.Key, kvp.Value.ToString());
        }
    }

    #region Virtual methods
    //Overridden for energy & shipping functions
    public virtual void RemoveDependencies()
	{ }

	//Overridden for energy & shipping functions
	public virtual void RestoreDependencies()
	{ }

	//Overridden for energy & shipping functions
	public virtual void ActivateConnections()
	{ }

	//Clears the list of connections. These are re-added when an energy layer is activated.
	public virtual void ClearConnections()
	{ }

	protected virtual void UpdateRestrictionArea(float newRestrictionSize)
	{
		currentRestrictionSize = newRestrictionSize;
	}

	public virtual void HideRestrictionArea()
	{
		restrictionHidden = true;
		//Children should disable the restriction gameobject
	}

    public virtual void CalculationPropertyUpdated()
    { }
    #endregion

}

public class SubEntityDataCopy
{
	public readonly List<EntityType> entityTypeCopy;
	public readonly List<Vector3> pointsCopy;
	public readonly List<List<Vector3>> holesCopy;
	public readonly Dictionary<string, string> metaDataCopy;
	public readonly int country;
    public readonly bool edited;

	public SubEntityDataCopy(List<EntityType> entityTypeCopy, List<Vector3> pointsCopy, Dictionary<string, string> metaDataCopy, List<List<Vector3>> holesCopy, int country, bool edited)
	{
		this.entityTypeCopy = entityTypeCopy;
		this.pointsCopy = pointsCopy;
		this.metaDataCopy = metaDataCopy;
		this.holesCopy = holesCopy;
		this.country = country;
        this.edited = edited;
	}
}

