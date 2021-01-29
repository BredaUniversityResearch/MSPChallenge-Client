using UnityEngine;
using System.Collections.Generic;

public class HighlightManager : MonoBehaviour {

	private static HighlightManager singleton;
	public static HighlightManager instance
	{
		get
		{
			if (singleton == null)
			{
				singleton = FindObjectOfType<HighlightManager>();
			}
			return singleton;
		}
	}

	public Object highlightPrefab;
	List<GameObject> highlightObjects = new List<GameObject>();

	void Start()
	{
		singleton = this;
	}

	public void HighlightPointSubEntity(PointSubEntity subEnt)
	{
		GameObject temp = Instantiate(highlightPrefab) as GameObject;
		temp.GetComponent<SpriteRenderer>().color = TeamManager.GetTeamByTeamID(subEnt.Entity.Country).color;
		temp.transform.SetParent(subEnt.GetGameObject().transform);
		temp.transform.localPosition = Vector3.zero;
		highlightObjects.Add(temp);
	}

	public void RemoveHighlight()
	{
		foreach (GameObject go in highlightObjects)
			Destroy(go);
		highlightObjects = new List<GameObject>();
	}
}
