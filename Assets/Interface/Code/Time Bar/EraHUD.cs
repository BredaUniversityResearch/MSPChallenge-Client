using UnityEngine;
using UnityEngine.UI;
using System;
using ColourPalette;
using TMPro;

public class EraHUD : MonoBehaviour {

    private static EraHUD singleton;

    public static EraHUD instance
    {
        get
        {
            if (singleton == null)
                singleton = (EraHUD)FindObjectOfType(typeof(EraHUD));
            return singleton;
        }
    }

    public CustomButton timeManagerButton;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI timeText;
    public ColourAsset planAndPauseTextColour;
    public ColourAsset highlightTextColour;
    public GameObject background;
    private TimeSpan time;

    private void Start()
    {
        LayerImporter.OnDoneImporting += () =>
        {
            timeManagerButton.interactable = TeamManager.IsGameMaster;
			background.SetActive(TeamManager.IsGameMaster); //Deactivate the gameobject so the info popup doesn't show up for players.

		};
    }
    
    private GameState.PlanningState planningState;
    public GameState.PlanningState State
    {
        get
        {
            return planningState;
        }
        set
        {
            switch (value) {
				case GameState.PlanningState.Setup:
					stateText.text = "Setup";
					stateText.alignment = TextAlignmentOptions.MidlineGeoAligned;
					stateText.color = highlightTextColour.GetColour();
					timeText.gameObject.SetActive(false);
					break;
				case GameState.PlanningState.Play:
                    stateText.text = "Planning";
                    stateText.alignment = TextAlignmentOptions.BottomGeoAligned;
                    stateText.color = planAndPauseTextColour.GetColour();
                    timeText.gameObject.SetActive(true);
                    break;
				case GameState.PlanningState.FastForward:
					stateText.text = "Fast Forward";
					stateText.alignment = TextAlignmentOptions.MidlineGeoAligned;
					stateText.color = highlightTextColour.GetColour();
					timeText.gameObject.SetActive(false);
					break;
				case GameState.PlanningState.Simulation:
                    stateText.text = "Simulating";
                    stateText.alignment = TextAlignmentOptions.MidlineGeoAligned;
                    stateText.color = highlightTextColour.GetColour();
                    timeText.gameObject.SetActive(false);
                    break;
                case GameState.PlanningState.Pause:
                    stateText.text = "Paused";
                    stateText.alignment = TextAlignmentOptions.MidlineGeoAligned;
                    stateText.color = planAndPauseTextColour.GetColour();
                    break;
                case GameState.PlanningState.End:
                    stateText.text = "End";
                    stateText.alignment = TextAlignmentOptions.MidlineGeoAligned;
                    stateText.color = highlightTextColour.GetColour();
                    timeText.gameObject.SetActive(false);
                    break;
            }
            planningState = value;
        }
    }

    private bool catchingUp;
    public bool CatchingUp
    {
        get { return catchingUp; }
        set
        {
            if (catchingUp != value)
            {
                
                if (value && planningState == GameState.PlanningState.Play)
                {
                    catchingUp = true;
                    stateText.text = "Calculating";
                    stateText.alignment = TextAlignmentOptions.MidlineGeoAligned;
                    stateText.color = planAndPauseTextColour.GetColour();
                    timeText.gameObject.SetActive(false);
                }
                else
                {
                    catchingUp = false;
                    State = planningState;
                }
            }
        }
    }
    
    public TimeSpan TimeRemaining
    {
        get
        {
            return time;
        }
        set
        {
            if (value.Ticks > TimeSpan.TicksPerDay) {
                timeText.text = string.Format("{0:D1}:{1:D2}:{2:D2}:{3:D2}", value.Days, value.Hours, value.Minutes, value.Seconds);
            } else if (value.Ticks < TimeSpan.TicksPerHour) {
                timeText.text = string.Format("{0:D1}:{1:D2}", value.Minutes, value.Seconds);
            } else if (value.Ticks < TimeSpan.TicksPerDay) {
                timeText.text = string.Format("{0:D1}:{1:D2}:{2:D2}", value.Hours, value.Minutes, value.Seconds);
            }
            time = value;
        }
    }
}