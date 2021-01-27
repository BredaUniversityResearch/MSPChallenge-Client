using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
//using NUnit.Framework;

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
    public Animator anim;
	public TimeLineUtil timeLineUtil;

    // Button Group Overlay
    public RectTransform groupButtonLocation;
    public RectTransform trackCover;
    public Button trackCoverButton;
	public Text inspectingYearText;

    public LayoutElement timelineGridLayout;

    private Dictionary<int, int> teamTrackID;

	[SerializeField]
	private bool foldOnStart = true;

	private delegate void PlansEventDelegate(Plan plan);
	private delegate void PlansUpdateEventDelegate(Plan plan, int oldTime);

	private static event PlansEventDelegate OnNewPlanAddedEvent;
	private static event PlansUpdateEventDelegate OnUpdatePlanEvent;
	private static event PlansUpdateEventDelegate OnPlanRemovedEvent;

    protected void Awake()
    {
        timelineGridLayout.minHeight = 0.0f;
        trackLocation.sizeDelta = new Vector2(trackLocation.sizeDelta.x, 1f);
        dateMarkerGraphic.uvRect = new Rect(0f, 0f, 1f, 1f);
		tracks = new List<TimelineTrack>(16);

		teamTrackID = new Dictionary<int, int>();

		OnNewPlanAddedEvent += OnAddNewPlan;
		OnUpdatePlanEvent += OnUpdatePlan;
		OnPlanRemovedEvent += OnRemoveExistingPlan;

        if (foldOnStart)
        {
            gameObject.SetActive(false);
            Fold();
        }

        if (TeamManager.TeamCount != 0)
        {
            CreateTracks();
        }
        else
        {
            TeamManager.OnTeamsLoadComplete += OnDelayedTeamLoad;
        }
    }

	//protected void Start()
	//{
		//if (foldOnStart)
		//{
		//	gameObject.SetActive(false);
		//	Fold();
		//}

		//if (TeamManager.teamCount != 0)
		//{
		//	CreateTracks();
		//}
		//else
		//{
		//	TeamManager.OnTeamsLoadComplete += OnDelayedTeamLoad;
		//}

	//}

	private void OnDelayedTeamLoad()
	{
		CreateTracks();
		TeamManager.OnTeamsLoadComplete -= OnDelayedTeamLoad;
	}

	private void CreateTracks()
    {
        // Create tracks
        foreach (var kvp in TeamManager.GetTeamsByID())
        {
            CreateTrack(kvp.Value.color);
            teamTrackID.Add(kvp.Key, tracks.Count - 1);
        }
		timelineGridLayout.minHeight = 16f * tracks.Count;
	}

	public static void AddNewPlan(Plan plan)
	{
		if (OnNewPlanAddedEvent != null)
		{
			OnNewPlanAddedEvent(plan);
		}
	}

	public static void RemoveExistingPlan(Plan plan, int oldPlanTime = -1)
	{
		if (OnPlanRemovedEvent != null)
		{
			OnPlanRemovedEvent(plan, oldPlanTime);
		}
	}

	public static void UpdatePlan(Plan plan, int oldPlanTime = -1)
	{
		if (OnUpdatePlanEvent != null)
		{
			OnUpdatePlanEvent(plan, oldPlanTime);
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
			//GetComponentInParent<PlansWindowMinMax>().ResizeLayout();
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
        trackLocation.GetComponent<VerticalLayoutGroup>().enabled = !dir;

        if (dir)
        {
            trackCover.SetAsLastSibling();
            groupButtonLocation.transform.SetAsLastSibling();
        }

        anim.SetBool("Show", dir);
    }
}