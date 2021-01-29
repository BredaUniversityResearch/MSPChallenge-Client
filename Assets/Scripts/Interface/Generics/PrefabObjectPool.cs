using System.Collections.Generic;
using UnityEngine;

public class PrefabObjectPool : MonoBehaviour
{
	private GameObject prefabToUse;
	private Transform activeContainer;
	private Transform inactiveContainer;

	private HashSet<GameObject> activeList = new HashSet<GameObject>();
	private Stack<GameObject> inactiveList = new Stack<GameObject>(4);

	public static PrefabObjectPool Create(GameObject ownerGameObject, GameObject prefab, Transform activeContainer, Transform inactiveContainer)
	{
		PrefabObjectPool pool = ownerGameObject.AddComponent<PrefabObjectPool>();
		pool.hideFlags = HideFlags.HideAndDontSave;
		pool.Initialise(prefab, activeContainer, inactiveContainer);
		return pool;
	}

	private void OnDestroy()
	{
		foreach(GameObject activeEntry in activeList)
		{
			if (activeEntry != null)
			{
				activeEntry.GetComponent<PrefabObjectPoolTracker>().SetOwningPool(null);
				Destroy(activeEntry);
			}
		}
		activeList.Clear();

		while (inactiveList.Count > 0)
		{
			if (inactiveList.Peek() != null)
			{
				GameObject inactiveEntry = inactiveList.Pop();
				inactiveEntry.GetComponent<PrefabObjectPoolTracker>().SetOwningPool(null);
				Destroy(inactiveEntry);
			}
		}
	}

	private void Initialise(GameObject prefab, Transform activeContainer, Transform inactiveContainer)
	{
		this.prefabToUse = prefab;
		this.activeContainer = activeContainer;
		this.inactiveContainer = inactiveContainer;
	}

	public GameObject Get()
	{
		if (inactiveList.Count == 0)
		{
			InstantiateFromPrefab(1);
		}

		GameObject result = inactiveList.Pop();
		result.transform.SetParent(activeContainer, false);

		activeList.Add(result);

		return result;
	}

	public void Release(GameObject instance)
	{
		if (activeList.Contains(instance))
		{
			instance.SetActive(false);
			instance.transform.SetParent(inactiveContainer, false);

			activeList.Remove(instance);
			inactiveList.Push(instance);
		}
		else
		{
			Debug.LogError("Trying to release an object to the PrefabObjectPool that is not owned by the object pool", gameObject);
		}
	}

	private void InstantiateFromPrefab(int objectCountToInstantiate)
	{
		for (int i = 0; i < objectCountToInstantiate; ++i)
		{
			GameObject result = Instantiate(prefabToUse, inactiveContainer);
			result.SetActive(false);
			PrefabObjectPoolTracker tracker = result.AddComponent<PrefabObjectPoolTracker>();
			tracker.SetOwningPool(this);
			tracker.hideFlags = HideFlags.DontSave;

			inactiveList.Push(result);
		}
	}
}