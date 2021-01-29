using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

public class TimeManager : MonoBehaviour
{
	private const int ERA_COUNT = 4;

    private static TimeManager singleton;

    public static TimeManager instance
    {
        get
        {
            if (singleton == null)
                singleton = (TimeManager)FindObjectOfType(typeof(TimeManager));
            return singleton;
        }
    }
    
    private int era = 0;
    public int Era { get { return era; } }
    private int[] eraRealTimes = new int[ERA_COUNT];
    private int eraGameTime = 0;
    int timeLeft = 0;
    //int timeBeforeForwarding = -1; //if not -1, we are forwarding and the number is the realtime before forwarding

    public void UpdateUI(TimelineState timeState)
    {
        //Use month
        int month = GameState.GetCurrentMonth();
        TimeManagerWindow.instance.timeline.Progress = (float)month / (float)Main.MspGlobalData.session_end_month;

		//Era change
		int newEra = Math.Min(Mathf.FloorToInt(month / (float)Main.MspGlobalData.era_total_months), ERA_COUNT - 1);
        if (newEra != era)
        {
            for (int i = era; i < newEra; i++)
            {
                TimeManagerWindow.instance.timeline.eraBlocks[i].IsActive = false;
            }
            era = newEra;
        }      

        //Era realtime
        string[] planningTimes = timeState.planning_era_realtime.Split(',');
        for (int i = era + 1; i < ERA_COUNT; i++)
        {
            int newEraTime = Util.ParseToInt(planningTimes[i]);
            if (newEraTime != eraRealTimes[i])
            {
                TimeManagerWindow.instance.timeline.eraBlocks[i].SetDurationUI(TimeSpan.FromSeconds(newEraTime));
                eraRealTimes[i] = newEraTime;
            }
        }

        //Current era realtime
        if (timeState.era_realtime != null)
        {
            int newEraTime = Util.ParseToInt(timeState.era_realtime);
            if (eraRealTimes[era] != newEraTime)
            {
                eraRealTimes[era] = newEraTime;
                TimeSpan newTimeSpan = TimeSpan.FromSeconds(newEraTime);
                if (!GameState.GameStarted)
                {
                    EraHUD.instance.TimeRemaining = newTimeSpan;
                    TimeManagerWindow.instance.timeline.eraBlocks[era].SetDurationUI(newTimeSpan);
                }
            }
        }

        //Era gametime
        if (timeState.era_gametime != null)
        {
            int newGameTime = Util.ParseToInt(timeState.era_gametime);
            if (eraGameTime != newGameTime)
            {
                float division = 1f - ((float)newGameTime / (float)Main.MspGlobalData.era_total_months);
                for (int i = 0; i < ERA_COUNT; i++)
                {
                    TimeManagerWindow.instance.eraDivision.SetEraSimulationDivision(i, division);
                    TimeBar.instance.markers[i].eraSimMarker.rectTransform.sizeDelta = new Vector2((TimeBar.instance.eraMarkerLocation.rect.width / (float)ERA_COUNT) * division, 4f);
                }
                TimeManagerWindow.instance.eraDivision.SetSliderValue(newGameTime / 12);
                eraGameTime = newGameTime;             
            }
        }

        //Remaining time
        if (timeState.era_timeleft != null)
        {
            int newTimeLeft = (int)Util.ParseToFloat(timeState.era_timeleft);
            if (timeLeft != newTimeLeft)
            {
                if (newTimeLeft < 0)
                {
                    EraHUD.instance.CatchingUp = true;
                }
                else
                {
                    if (timeLeft < 0)
                        EraHUD.instance.CatchingUp = false;
                    TimeSpan newTimeSpan = TimeSpan.FromSeconds(newTimeLeft);                   
                    if (GameState.GameStarted)
                    {
                        EraHUD.instance.TimeRemaining = newTimeSpan;
                        TimeManagerWindow.instance.timeline.eraBlocks[era].SetDurationUI(newTimeSpan);
                    }
                }
                timeLeft = newTimeLeft;
            }
        }
    }

	public void SetupStateEntered()
	{
        TimeManagerWindow.instance.eraDivision.gameObject.SetActive(true);
        TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Pause);
	}

	public void PauseStateEntered()
    {
        TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Pause);
    }

    public void PlayStateEntered()
    {
        TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Play);
    }

	public void FastForwardStateEntered()
	{
		TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Forward);
	}

	public void SimulationStateEntered()
	{
		TimeManagerWindow.instance.timeline.eraBlocks[era].IsActive = false;
        TimeManagerWindow.instance.controls.Interactable = false;
        TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Play);
        //timeBeforeForwarding = -1;
    }

    public void SimulationStateExited()
    {
        TimeManagerWindow.instance.controls.Interactable = true;
    }

	public void SetupStateExited()
	{
		TimeManagerWindow.instance.controls.HideSetupFinishButton();
	}

	public void FinishSetup()
	{
		//This is the first time the game is run: ask for confirmation
		if (!GameState.GameStarted)
		{
			string title = "Start game";
			string description = "Starting plans will not be editable once the game has started.\n\nAre you sure you want to start the game?";
			UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() => { });
			UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() =>
			{
				GameState.SetGameState(GameState.PlanningStateToString(GameState.PlanningState.Pause));
			});

			DialogBoxManager.instance.ConfirmationWindow(title, description, lb, rb);
		}
	}

    public void Play()
    {
        //if (timeBeforeForwarding != -1)//If we were forwarding, go back to previous speed
        //{
        //    ServerChangePlanningRealTime(timeBeforeForwarding);
        //    timeBeforeForwarding = -1;
        //    //TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Play);
        //}
        //else
        //{
			GameState.SetGameState(GameState.PlanningStateToString(GameState.PlanningState.Play));
        //}
    }

    public void Pause()
    {
		GameState.SetGameState(GameState.PlanningStateToString(GameState.PlanningState.Pause));
	}

    public void Forward()
    {
        // Dialog Box
        string title = "Advancing Game Time";
        string description = "Advancing the game time will finish the current era and advance the game time to the next era.\n\nAre you sure you want to do this?";
        UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() => { });
        UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() => {
            //ServerChangePlanningRealTime(0);
            //timeBeforeForwarding = eraRealTimes[era];
			//TimeManagerWindow.instance.controls.SetGlowTo(TimeManagerControls.TimeControlButton.Forward);
			GameState.SetGameState(GameState.PlanningStateToString(GameState.PlanningState.FastForward));
		});
        DialogBoxManager.instance.ConfirmationWindow(title, description, lb, rb);
    }

    public void Restart()
    {
        // Dialog Box
        //string title = "Reset Game Time";
        //string description = "Resetting game time will restart the game from 2010 with the current game settings.\n\nAre you sure you want to do this?";
        //UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() => { });
        //UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() => {

        //});

        //DialogBox box = DialogBoxManager.instance.ConfirmationWindow(title, description, lb, rb);

        //Debug.Log("TimeManager: Reset");
    }

    public void RemainingTimeChanged(int newSeconds)
    {
        if(GameState.GameStarted)
            ServerChangePlanningRealTime(newSeconds + (eraRealTimes[era] - timeLeft));
        else
            ServerChangePlanningRealTime(newSeconds);
        //ServerChangeRemainingTime(newSeconds);
    }

    public void EraRealTimeChanged(int eraChanged, TimeSpan newDuration)
    {
        if (eraChanged < era)
            return;
        if (eraChanged == era)
        {
            //Change remaining time
            int monthsLeft = eraGameTime - (GameState.GetCurrentMonth() % Main.MspGlobalData.era_total_months);
            if (monthsLeft == 0)
                Debug.LogError("Era should have passed, but didn't. Causing divide by 0.");
            else if(monthsLeft < 0)
                Debug.LogError("Tried to change planning time in simulation phase");

            ServerChangePlanningRealTime(Mathf.CeilToInt((float)newDuration.TotalSeconds / ((float)monthsLeft / (float)eraGameTime)));
            //ServerChangePlanningRealTime((int)newDuration.TotalSeconds + (eraRealTimes[era] - timeLeft));
        }
        else
        {
            //Change future realtime
            eraRealTimes[eraChanged] = (int)newDuration.TotalSeconds;
            ServerChangeAllPlanningRealTime();
        }   
    }

	public void SetEraRealtimeValues(int[] eraTimesInSeconds)
	{
		if (eraTimesInSeconds.Length == ERA_COUNT)
		{
			Array.Copy(eraTimesInSeconds, eraRealTimes, ERA_COUNT);
			ServerChangePlanningRealTime(eraTimesInSeconds[era]);
			ServerChangeAllPlanningRealTime();
		}
		else
		{
			Debug.LogError(string.Format("EraTime in seconds does not match the required count. Received {0} expected {1}", eraTimesInSeconds.Length, ERA_COUNT));
		}
	}

	public void EraGameTimeChanged(int value)
    {
        ServerChangePlanningGameTime(value);
    }

    #region ServerCommunication

    private void ServerSetGameState(GameState.PlanningState state)
    {
        NetworkForm form = new NetworkForm();
        form.AddField("state", state.ToString());
        ServerCommunication.DoRequest(Server.SetGameState(), form);
    }

	private void ServerChangePlanningGameTime(int newMonths)
	{
		if (newMonths < 12)
		{
			Debug.LogWarning("Time manager tried to set a planning time less than 12 months. This should not be done! New Months: " + newMonths);
			newMonths = 12;
		}

		NetworkForm form = new NetworkForm();
		form.AddField("months", newMonths);
        ServerCommunication.DoRequest(Server.SetGamePlanningTime(), form);
    }

    private void ServerChangePlanningRealTime(int newSeconds)
    {
        if (newSeconds < 10)
            newSeconds = 10;
        NetworkForm form = new NetworkForm();
        form.AddField("realtime", newSeconds);
        ServerCommunication.DoRequest(Server.SetRealPlanningTime(), form);
    }

    private void ServerChangeAllPlanningRealTime()
    {
        //Format era realtimes into comma separated string
        string times = eraRealTimes[0].ToString();
        for (int i = 1; i < eraRealTimes.Length; i++)
            times += "," + eraRealTimes[i].ToString();

        NetworkForm form = new NetworkForm();
        form.AddField("realtime", times);
        ServerCommunication.DoRequest(Server.SetFuturePlanningTime(), form);
    }

    private void ServerChangeRemainingTime(int newSeconds)
    {
        if (newSeconds < 10)
            newSeconds = 10;
        NetworkForm form = new NetworkForm();
        form.AddField("time", newSeconds);
        ServerCommunication.DoRequest(Server.SetPlanningTimeRemaining(), form);
    }
    #endregion
}