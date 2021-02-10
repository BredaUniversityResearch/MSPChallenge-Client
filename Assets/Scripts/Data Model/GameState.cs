using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;

public static class GameState
{
	public enum PlanningState { Setup, Play, Simulation, Pause, FastForward, End }

	private static PlanningState gameState = PlanningState.End;

	private static int month = -1;

	private static bool firstUpdateComplete; //TODO: handle in more elegant way

	private static TimeBar timeBar;

	public delegate void OnMonthChangedDelegate(int oldCurrentMonth, int newCurrentMonth);
	public static event OnMonthChangedDelegate OnCurrentMonthChanged;

	public static PlanningState CurrentState
	{
		get { return gameState; }
	}

	public static bool GameStarted
	{
		get { return gameState != PlanningState.Setup; }
	}


	public static void Initialise()
	{
		timeBar = UIManager.GetTimeBar();
	}

	public static int GetCurrentMonth()
	{
		return month;
	}

	public static void UpdateTime(TimelineState state)
	{
		if (!firstUpdateComplete)
		{
			firstUpdateComplete = true;
			PlanningState tempState = StringToPlanningState(state.state); 
			if(tempState != PlanningState.Setup)
				TimeManager.instance.SetupStateExited();
		}

		int prevMonth = month;
		PlanningState prevState = gameState;

		gameState = StringToPlanningState(state.state);
		month = Util.ParseToInt(state.month, 0);

		//Month change
		if (month != prevMonth)
		{
			if (gameState != PlanningState.Setup)
				PlanManager.MonthTick(month);
			PlanWizard.UpdateMinSelectableTime();

			if (OnCurrentMonthChanged != null)
			{
				OnCurrentMonthChanged.Invoke(prevMonth, month);
			}
		}

		//State change
		if (gameState != prevState)
		{
			EraHUD.instance.State = gameState;
			//New state entered
			if (gameState == PlanningState.Setup)
			{
				TimeManager.instance.SetupStateEntered();
				OnSetupPhaseStarted();
			}
			else if (gameState == PlanningState.Pause)
			{
				TimeManager.instance.PauseStateEntered();
			}
			else if (gameState == PlanningState.Play)
			{
				TimeManager.instance.PlayStateEntered();
			}
			else if (gameState == PlanningState.FastForward)
			{
				TimeManager.instance.FastForwardStateEntered();
			}
			else if (gameState == PlanningState.Simulation)
			{
				OnSimulationPhaseStarted();
			}

			//Old state left
			if (prevState == PlanningState.Simulation)
			{
				OnSimulationPhaseEnded();
			}
			else if (prevState == PlanningState.Setup)
			{
				OnSetupPhaseEnded();
			}
		}
		//Update UI
		timeBar.SetDate(month);
		TimeManager.instance.UpdateUI(state);
	}

	private static void OnSetupPhaseStarted()
	{
		if(!TeamManager.AreWeGameMaster)
			InterfaceCanvas.Instance.menuBarPlanWizard.toggle.interactable = false;
		//PlansMonitor.RefreshPlanEditButtonInteractablity();
	}

	private static void OnSetupPhaseEnded()
	{
		InterfaceCanvas.Instance.menuBarPlanWizard.toggle.interactable = true;
		//Update plans once the setup state is left, so we don't have to wait for month 1 
		TimeManagerWindow.instance.eraDivision.gameObject.SetActive(false);
		PlanManager.MonthTick(month);
		PlansMonitor.RefreshPlanButtonInteractablity();
		TimeManager.instance.SetupStateExited();
	}

	private static void OnSimulationPhaseStarted()
	{
		ScreenBorderGradient.instance.SetEnabled(true);
		ScrollingTextBand.instance.SetEnabled(true);
		InterfaceCanvas.Instance.menuBarPlanWizard.toggle.interactable = false;
		TimeManager.instance.SimulationStateEntered();
		PlanDetails.UpdateStatus();
		PlansMonitor.RefreshPlanButtonInteractablity();
	}

	private static void OnSimulationPhaseEnded()
	{
		ScreenBorderGradient.instance.SetEnabled(false);
		ScrollingTextBand.instance.SetEnabled(false);
		InterfaceCanvas.Instance.menuBarPlanWizard.toggle.interactable = true;
		TimeManager.instance.SimulationStateExited();
		PlanDetails.UpdateStatus();
		PlansMonitor.RefreshPlanButtonInteractablity();
	}

	public static void SetGameState(string state)
	{
		NetworkForm form = new NetworkForm();

		form.AddField("state", state);
		Debug.Log("Send State " + state.ToString());

		ServerCommunication.DoRequest(Server.SetGameState(), form);
	}

	public static void SetStartAndEndDate(DateTime start, DateTime end)
	{
		NetworkForm form = new NetworkForm();

		string startDate = start.ToString("yyyy-MM-dd hh:mm:ss");
		string endDate = end.ToString("yyyy-MM-dd hh:mm:ss");

		form.AddField("start", startDate);
		form.AddField("end", endDate);

		Debug.Log("Start: " + startDate + " End: " + endDate);

		ServerCommunication.DoRequest(Server.SetEndAndStartDate(), form);
	}

	public static PlanningState StringToPlanningState(string value)
	{
		return (PlanningState)Enum.Parse(typeof(PlanningState), value, true);
	}

	public static string PlanningStateToString(PlanningState state)
	{
		return state.ToString().ToUpper();
	}
}

