using System.Collections.Generic;
using UnityEngine;

public class KPIRoot : MonoBehaviour
{
	[SerializeField]
	private KPICategoryPopulator categoryPopulator = null;

	[SerializeField, Tooltip("Children to toggle on and off at the start and end of the animations")]
	private GameObject[] childrenToToggle = new GameObject[0];

    public KPIGroups groups;
    public Animator anim;

    protected void Start()
    {
		ResetOverview();
	}

	public void EnableOverview()
	{
		SetAllChildrenActive(true);
	}

	public void ResetOverview()
	{
		categoryPopulator.ResetContent();
		SetAllChildrenActive(false);
	}

	public void SetAllChildrenActive(bool activeState)
	{
		foreach (GameObject child in childrenToToggle)
		{
			if (child != null)
			{
				child.SetActive(activeState);
			}
			else
			{
				Debug.LogError("Found nullreference in objects to toggle for KPI Root. Please verify settings", gameObject);
			}
		}
	}
}