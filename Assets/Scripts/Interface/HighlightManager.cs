﻿using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
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

		[SerializeField] Object pointHighlightPrefab;
		[SerializeField] Object uiHighlightPrefab;
		[SerializeField] private Transform uiHighlightParent;
		List<GameObject> pointHighlightObjects = new List<GameObject>();
		List<GameObject> uiHighlightObjects = new List<GameObject>();

		void Start()
		{
			singleton = this;
		}

		public void HighlightPointSubEntity(PointSubEntity subEnt)
		{
			GameObject temp = Instantiate(pointHighlightPrefab, subEnt.GetGameObject().transform) as GameObject;
			temp.GetComponent<SpriteRenderer>().color = SessionManager.Instance.GetTeamByTeamID(subEnt.Entity.Country).color;
			temp.transform.localPosition = Vector3.zero;
			pointHighlightObjects.Add(temp);
		}

		public void ClearPointHighlights()
		{
			foreach (GameObject go in pointHighlightObjects)
				Destroy(go);
			pointHighlightObjects = new List<GameObject>();
		}

		public void ClearUIHighlights()
		{
			foreach (GameObject go in uiHighlightObjects)
				Destroy(go);
			uiHighlightObjects = new List<GameObject>();
		}

		public void SetUIHighlights(string[] uiReferences)
		{
			if(uiHighlightObjects.Count > 0)
				ClearUIHighlights();

			foreach (string reference in uiReferences)
			{
				GameObject target = InterfaceCanvas.Instance.GetUIObject(reference);
				if (target != null)
				{
					GameObject temp = Instantiate(uiHighlightPrefab, uiHighlightParent) as GameObject;
					temp.GetComponent<HighlightPulse>().SetTarget(target.transform);
					uiHighlightObjects.Add(temp);
				}
				else
				{
					Debug.LogError("Trying to highlight not existent UI object: " + reference);
				}
			}
		}
	}
}
