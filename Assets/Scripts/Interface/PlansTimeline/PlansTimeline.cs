using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//using NUnit.Framework;

namespace MSP2050.Scripts
{
	public class PlansTimeline : MonoBehaviour
	{
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

		protected void Awake()
		{
			trackLocation.sizeDelta = new Vector2(trackLocation.sizeDelta.x, 1f);
			tracks = new List<TimelineTrack>(16);

			teamTrackID = new Dictionary<int, int>();

			PlanManager.Instance.OnPlanVisibleInUIEvent += OnAddNewPlan;
			PlanManager.Instance.OnPlanUpdateInUIEvent += OnUpdatePlan;
			PlanManager.Instance.OnPlanHideInUIEvent += OnRemoveExistingPlan;

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

		private void CreateTrack(Color col)
		{
			GameObject go = Instantiate(trackPrefab);

			TimelineTrack track = go.GetComponent<TimelineTrack>();

			tracks.Add(track);

			go.transform.SetParent(trackLocation, false);

			track.SetTrackColor(col);
			track.timeline = this;

			trackLocation.sizeDelta = new Vector2(trackLocation.sizeDelta.x, trackLocation.sizeDelta.y + 16f);
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