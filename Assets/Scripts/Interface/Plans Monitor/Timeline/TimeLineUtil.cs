using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//[ExecuteInEditMode]
public class TimeLineUtil : MonoBehaviour {

	public GameObject spacerPrefab;
	public GameObject seasonPrefab;
	public GameObject yearPrefab;
	public GameObject markedYearPrefab;
	public int markedYearInterval;
	public List<Transform> yearMarkers;

    private void Awake()
    {
        if (Main.MspGlobalData != null)
        {
            CreateTimeLine();
        }
        else
        {
            Main.OnGlobalDataLoaded += GlobalDataLoaded;
        }
    }

    void GlobalDataLoaded()
    {
        Main.OnGlobalDataLoaded -= GlobalDataLoaded;
        CreateTimeLine();
    }

    public void CreateTimeLine()
	{
        int years = Main.MspGlobalData.session_num_years;
		yearMarkers = new List<Transform>();
		for (int i = 0; i < years; i++)
		{
			if (i % markedYearInterval == 0)
                AddObjectWithText(markedYearPrefab, i, true);
			else
				AddObject(yearPrefab, true);

			AddObject(spacerPrefab);
		}

		//Final year
		if (years % markedYearInterval == 0)
            AddObjectWithText(markedYearPrefab, years);
		else
			AddObject(yearPrefab);
	}

	void AddObject(GameObject prefab, bool addToList = false)
	{
		GameObject go = Instantiate(prefab, transform);
		if (addToList)
			yearMarkers.Add(go.transform);
	}

	void AddObjectWithText(GameObject prefab, int yearOffset, bool addToList = false)
	{
		GameObject go = Instantiate(prefab, transform);
		SetTextToYear component = go.GetComponentInChildren<SetTextToYear>();
		if (component != null)
		{
			component.yearOffset = yearOffset;
		}

		if (addToList)
			yearMarkers.Add(go.transform);
	}    
}
