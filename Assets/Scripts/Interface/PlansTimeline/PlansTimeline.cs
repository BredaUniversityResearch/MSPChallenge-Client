using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//using NUnit.Framework;

namespace MSP2050.Scripts
{
	public class PlansTimeline : MonoBehaviour
	{
		// Fold Button
		public Button foldButton;
		public RectTransform foldButtonRect;
		public GameObject foldParent;

		// Date Marker
		public RawImage dateMarkerGraphic;

		// Tracks
		public GameObject trackPrefab;
		public RectTransform trackLocation;
		private List<TimelineTrack> tracks;
		public TimeLineUtil timeLineUtil;

		// Button Group Overlay
		public RectTransform groupButtonLocation;
		public RectTransform trackCover;
		public Button trackCoverButton;
		public TextMeshProUGUI inspectingYearText;

		private Dictionary<int, int> teamTrackID;

		[SerializeField]
		private bool foldOnStart = true;

		protected void Awake()
		{
			trackLocation.sizeDelta = new Vector2(trackLocation.sizeDelta.x, 1f);
			dateMarkerGraphic.uvRect = new Rect(0f, 0f, 1f, 1f);
			tracks = new List<TimelineTrack>(16);

			teamTrackID = new Dictionary<int, int>();

			PlanManager.Instance.OnPlanVisibleInUIEvent += OnAddNewPlan;
			PlanManager.Instance.OnPlanUpdateInUIEvent += OnUpdatePlan;
			PlanManager.Instance.OnPlanHideInUIEvent += OnRemoveExistingPlan;

			if (foldOnStart)
			{
				gameObject.SetActive(false);
				Fold();
			}

			CreateTracks();
		}


		private void CreateTracks()
		{
			// Create tracks
			foreach (var kvp in SessionManager.Instance.GetTeamsByID())
			{
				CreateTrack(kvp.Value.color);
				teamTrackID.Add(kvp.Key, tracks.Count - 1);
			}
		}

		private void OnAddNewPlan(Plan plan)
		{
			int trackID = teamTrackID[plan.Country];
			tracks[trackID].RegisterEvent(plan);
		}

		private void OnUpdatePlan(Plan plan, int oldTime)
		{
			int trackID = teamTrackID[plan.Country];
			tracks[trackID].UpdatetrackEventFor(plan, oldTime);
		}

		private void OnRemoveExistingPlan(Plan plan, int oldPlanTime = -1)
		{
			int trackID = teamTrackID[plan.Country];
			tracks[trackID].RemoveTrackEvent(plan, oldPlanTime);
		}

		public void Fold()
		{
			if (foldButtonRect != null && foldParent != null)
			{
				Vector3 rot = foldButtonRect.eulerAngles;
				foldButtonRect.eulerAngles = (rot.z == 0) ? new Vector3(rot.x, rot.y, 90f) : new Vector3(rot.x, rot.y, 0f);
				foldParent.SetActive(!foldParent.activeSelf);
			}
		}

		private void CreateTrack(Color col)
		{
			GameObject go = Instantiate(trackPrefab);

			TimelineTrack track = go.GetComponent<TimelineTrack>();

			tracks.Add(track);

			go.transform.SetParent(trackLocation, false);

			track.SetTrackColor(col);
			track.timeline = this;

			trackLocation.sizeDelta = new Vector2(trackLocation.sizeDelta.x, trackLocation.sizeDelta.y + 16f);
			dateMarkerGraphic.uvRect = new Rect(0f, 0f, 1f, dateMarkerGraphic.uvRect.height + 1f);
		}

		public void IsolateButtonGroup(bool dir)
		{
			if (dir)
			{
				trackCover.SetAsLastSibling();
				groupButtonLocation.transform.SetAsLastSibling();
			}

			trackCover.gameObject.SetActive(dir);
		}
	}
}