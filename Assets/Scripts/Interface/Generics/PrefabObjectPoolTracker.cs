using UnityEngine;

public class PrefabObjectPoolTracker : MonoBehaviour
{
	public PrefabObjectPool OwningPool
	{
		get;
		private set;
	}

	public void SetOwningPool(PrefabObjectPool owningPool)
	{
		OwningPool = owningPool;
	}

	private void OnDestroy()
	{
		if (OwningPool != null)
		{
			Debug.LogError("Unintended destruction of object pool entity?");
		}
	}
}