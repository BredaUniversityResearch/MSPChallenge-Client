using System.Collections;
using System.Collections.Generic;
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

		List<GameObject> uiHighlightObjects = new List<GameObject>();
		private HashSet<string> unresolvedHighlights = new HashSet<string>();
		bool highlightingTags;

		void Start()
		{
			singleton = this;
			InterfaceCanvas.Instance.uiReferenceNameRegisteredEvent += OnUIObjectNameRegistered;
			InterfaceCanvas.Instance.uiReferenceTagsRegisteredEvent += OnUIObjectTagsRegistered;
		}

		public void HighlightPointSubEntity(PointSubEntity subEnt)
		{
			GameObject temp = Instantiate(pointHighlightPrefab, subEnt.GetGameObject().transform) as GameObject;
			temp.GetComponent<SpriteRenderer>().color = SessionManager.Instance.GetTeamByTeamID(subEnt.Entity.Country).color;
			temp.transform.localPosition = Vector3.zero;
			//pointHighlightObjects.Add(temp);
		}

		public void ClearUIHighlights()
		{
			foreach (GameObject go in uiHighlightObjects)
				Destroy(go);
			uiHighlightObjects = new List<GameObject>();
			unresolvedHighlights = new HashSet<string>();
		}

		public void SetUIHighlights(string[] a_uiReferences, bool a_highlightTags)
		{
			if(uiHighlightObjects.Count > 0)
				ClearUIHighlights();
			highlightingTags = a_highlightTags;

			if (highlightingTags)
			{
				foreach (string reference in a_uiReferences)
				{
					unresolvedHighlights.Add(reference);
					HashSet<GameObject> targets = InterfaceCanvas.Instance.GetUIWithTag(reference);
					if (targets != null)
					{
						foreach (GameObject obj in targets)
						{
							GameObject temp = Instantiate(uiHighlightPrefab, uiHighlightParent) as GameObject;
							temp.GetComponent<IHighlightObject>().SetTarget(obj.GetComponent<RectTransform>());
							uiHighlightObjects.Add(temp);
						}
					}
					
				}
			}
			else
			{
				foreach (string reference in a_uiReferences)
				{
					GameObject target = InterfaceCanvas.Instance.GetUIObject(reference);
					if (target != null)
					{
						GameObject temp = Instantiate(uiHighlightPrefab, uiHighlightParent) as GameObject;
						temp.GetComponent<IHighlightObject>().SetTarget(target.GetComponent<RectTransform>());
						uiHighlightObjects.Add(temp);
					}
					else
					{
						unresolvedHighlights.Add(reference);
					}
				}
			}
		}

		void OnUIObjectNameRegistered(string name, GameObject obj)
		{
			if (highlightingTags)
				return;

			if(unresolvedHighlights.Contains(name))
			{
				unresolvedHighlights.Remove(name);
				StartCoroutine(CreateHighlightNextFrame(obj));
			}
		}

		void OnUIObjectTagsRegistered(string[] tags, GameObject obj)
		{
			if (!highlightingTags)
				return;

			foreach (string tag in tags)
			{
				if (unresolvedHighlights.Contains(tag))
				{
					StartCoroutine(CreateHighlightNextFrame(obj));
					break;
				}
			}
		}

		IEnumerator CreateHighlightNextFrame(GameObject obj)
		{
			yield return new WaitForEndOfFrame();
			GameObject temp = Instantiate(uiHighlightPrefab, uiHighlightParent) as GameObject;
			temp.GetComponent<IHighlightObject>().SetTarget(obj.GetComponent<RectTransform>());
			uiHighlightObjects.Add(temp);			
		}
	}
}
