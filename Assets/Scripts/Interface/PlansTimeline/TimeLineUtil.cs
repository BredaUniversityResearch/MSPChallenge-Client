﻿using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
namespace MSP2050.Scripts
{
	public class TimeLineUtil : MonoBehaviour {

		public GameObject spacerPrefab;
		public GameObject yearPrefab;
		public GameObject markedYearPrefab;
		[HideInInspector] public List<Transform> yearMarkers;
		int markedYearInterval;

		private void Awake()
		{
			markedYearInterval = SessionManager.Instance.MspGlobalData.YearsPerEra;
			CreateTimeLine();
		}

		public void CreateTimeLine()
		{
			int years = SessionManager.Instance.MspGlobalData.session_num_years;
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
}
