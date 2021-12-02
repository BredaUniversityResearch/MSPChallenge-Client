using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class GenericContent : MonoBehaviour
{
    public Transform entryLocation;

    [Header("Prefabs")]
    public GameObject genericEntryPrefab;
    public GameObject genericEntryIconPrefab;
    public GameObject genericEntryButtonPrefab;
    public GameObject genericLayerPrefab;
    
    private List<GenericEntry> genericEntry = new List<GenericEntry>();
    private List<GenericLayer> genericLayer = new List<GenericLayer>();

	private PrefabObjectPool genericEntryPool;
	private PrefabObjectPool genericEntryIconPool;
	private PrefabObjectPool genericEntryButtonPool;
    bool initialised = false;

	private void Awake()
    {
        Initialise();
	}

    public void Initialise()
    {
        if (initialised)
            return;

        initialised = true;
        genericEntryPool = PrefabObjectPool.Create(gameObject, genericEntryPrefab, entryLocation, transform);
		genericEntryIconPool = PrefabObjectPool.Create(gameObject, genericEntryIconPrefab, entryLocation, transform);
		genericEntryButtonPool = PrefabObjectPool.Create(gameObject, genericEntryButtonPrefab, entryLocation, transform);
    }

    /// <summary>
    /// Remove from list and destroy a content window entry
    /// </summary>
    public void DestroyGenericEntry(GenericEntry entry)
	{
		genericEntry.Remove(entry);

		PrefabObjectPoolTracker tracker = entry.gameObject.GetComponent<PrefabObjectPoolTracker>();
		if (tracker != null)
		{
			tracker.OwningPool.Release(entry.gameObject);
		}
		else
		{
			Destroy(entry.gameObject);
		}
	}

    /// <summary>
    /// Remove from list and destroy a content window entry
    /// </summary>
    public void DestroyGenericLayer(GenericLayer layer)
    {
        genericLayer.Remove(layer);
        Destroy(layer.gameObject);
    }

	public void DestroyAllContent()
	{
		for (int i = genericEntry.Count - 1; i >= 0; --i)
		{
			DestroyGenericEntry(genericEntry[i]);
		}

		for (int i = genericLayer.Count - 1; i >= 0; --i)
		{
			DestroyGenericLayer(genericLayer[i]);
		}

		if (genericEntry.Count > 0 || genericLayer.Count > 0)
		{
			Debug.LogError("Incomplete destruction of generic content");
		}
	}

    /// <summary>
    /// Create a new entry
    /// </summary>
    public GenericEntry CreateEntry<T>(string name, T param)
    {
        // Instantiate prefab
        GameObject go = genericEntryPool.Get();

        // Store component
        GenericEntry entry = go.GetComponent<GenericEntry>();

        // Add to list
        genericEntry.Add(entry);

        // Assign parent
        go.transform.SetParent(entryLocation, false);

        if(typeof(T) == typeof(Texture) || typeof(T) == typeof(RenderTexture))
        {
            entry.PropertyImage<T>(name, param);
        }
        else
        {
	        entry.PropertyLabel<T>(name, param);
        }

		go.SetActive(true);

        return entry;
    }

	/// <summary>
	/// Create a new entry
	/// </summary>
	public GenericEntry CreateEntry<T>(string name, T param, Sprite icon, Color color)
	{
		// Instantiate prefab
		GameObject go = genericEntryIconPool.Get();

		// Store component
		GenericEntryIcon entry = go.GetComponent<GenericEntryIcon>();

		// Add to list
		genericEntry.Add(entry);

		// Assign parent
		go.transform.SetParent(entryLocation, false);

		// Is this an editable property?
		entry.PropertyLabel<T>(name, param, icon, color);

		go.SetActive(true);

		return entry;
	}

	/// <summary>
	/// Create a new entry with button and given callback
	/// </summary>
	public GenericEntry CreateEntry<T>(string name, T param, UnityAction buttonCallBack)
	{
		// Instantiate prefab
		GameObject go = genericEntryButtonPool.Get();

		// Store component
		GenericEntry entry = go.GetComponent<GenericEntry>();

		// Add to list
		genericEntry.Add(entry);

		// Assign parent
		go.transform.SetParent(entryLocation, false);
		entry.PropertyLabel<T>(name, param, buttonCallBack);

		go.SetActive(true);

		return entry;
	}

	/// <summary>
	/// Create a new layer
	/// </summary>
	public GenericLayer CreateLayer(string name, bool editable, bool closeable = false)
    {
        // Instantiate prefab
        GameObject go = Instantiate(genericLayerPrefab);

        // Store component
        GenericLayer layer = go.GetComponent<GenericLayer>();

        // Add to list
        genericLayer.Add(layer);

        // Assign parent
        go.transform.SetParent(entryLocation, false);

        // Set name
        layer.SetTitle(name);

        return layer;
    }
}