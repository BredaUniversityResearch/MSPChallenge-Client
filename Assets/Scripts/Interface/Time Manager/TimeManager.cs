using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TimeManager : MonoBehaviour
	{
		private const int ERA_COUNT = 4;

		private static TimeManager singleton;
		public static TimeManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<TimeManager>();
				return singleton;
			}
		}

		[SerializeField] GameObject m_screenborderGradient;
		[SerializeField] GameObject m_scrollingTextBand;

		//Time
		private int month = -1;
		private int? transitionMonth = -1;
		private int era = 0;
		public int Era { get { return era; } }
		private int[] eraRealTimes = new int[ERA_COUNT];
		private int eraGameTime = 0;
		int timeLeft = 0;

		//Gamestate
		public enum PlanningState { Setup, Play, Simulation, Pause, FastForward, End, Request, None }
		private PlanningState gameState = PlanningState.None;
		private PlanningState transitionState = PlanningState.None;
		private bool firstUpdateComplete;

		public delegate void OnMonthChangedDelegate(int oldCurrentMonth, int newCurrentMonth);
		public event OnMonthChangedDelegate OnCurrentMonthChanged;

		private float m_timeLeftElapsed = 0f;

		public int MonthsPerEra => eraGameTime;
		public int? TransitionMonth => transitionMonth;
		public int Month => month;
		public PlanningState GameState => gameState;
		public PlanningState TransitionState => transitionState;

		public void Update()
		{
			if (CurrentState != PlanningState.Play)
			{
				return;
			}

			m_timeLeftElapsed += Time.deltaTime;
			TimeBar.instance.SetTimeRemaining(TimeSpan.FromSeconds(Math.Max(timeLeft - (int)m_timeLeftElapsed, 0)));
		}

		void Start()
		{
			if(singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
		}

		void OnDestroy()
		{
			singleton = null;
		}

		public void UpdateUI(TimelineState timeState)
		{
			m_timeLeftElapsed = 0f;

			//Use month
			int month = GetCurrentMonth();
			if (month == -1) return; // nothing to do in Setup
			TimeManagerWindow.instance.timeline.Progress = (float)month / (float)SessionManager.Instance.MspGlobalData.session_end_month;

			//Era change
			int newEra = Math.Min(Mathf.FloorToInt(month / (float)SessionManager.Instance.MspGlobalData.era_total_months), ERA_COUNT - 1);
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
					if (!GameStarted)
					{
						TimeBar.instance.SetTimeRemaining(newTimeSpan);
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
					float division = 1f - ((float)newGameTime / (float)SessionManager.Instance.MspGlobalData.era_total_months);
					for (int i = 0; i < ERA_COUNT; i++)
					{
						TimeManagerWindow.instance.eraDivision.SetEraSimulationDivision(i, division);
						//TimeBar.instance.eraMarkers[i].eraSimMarker.rectTransform.sizeDelta = new Vector2((TimeBar.instance.eraMarkerParent.rect.width / (float)ERA_COUNT) * division, 4f);
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
						TimeBar.instance.SetCatchingUp(true);
					}
					else
					{
						if (timeLeft < 0)
							TimeBar.instance.SetCatchingUp(false);
						TimeSpan newTimeSpan = TimeSpan.FromSeconds(newTimeLeft);
						if (GameStarted)
						{
							TimeBar.instance.SetTimeRemaining(newTimeSpan);
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
			if (!GameStarted)
			{
				string title = "Start game";
				string description = "Starting plans will not be editable once the game has started.\n\nAre you sure you want to start the game?";
				UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() => { });
				UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() =>
				{
					SetGameState(PlanningStateToString(PlanningState.Pause));
				});

				DialogBoxManager.instance.ConfirmationWindow(title, description, lb, rb);
			}
		}

		public void Play()
		{
			SetGameState(PlanningStateToString(PlanningState.Play));
		}

		public void Pause()
		{
			SetGameState(PlanningStateToString(PlanningState.Pause));
		}

		public void Forward()
		{
			// Dialog Box
			string title = "Advancing Game Time";
			string description = "Advancing the game time will finish the current era and advance the game time to the next era.\n\nAre you sure you want to do this?";
			UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() => { });
			UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() => {
				SetGameState(PlanningStateToString(PlanningState.FastForward));
			});
			DialogBoxManager.instance.ConfirmationWindow(title, description, lb, rb);
		}

		public void RemainingTimeChanged(int newSeconds)
		{
			if(GameStarted)
				ServerChangePlanningRealTime(newSeconds + (eraRealTimes[era] - timeLeft));
			else
				ServerChangePlanningRealTime(newSeconds);
		}

		public void EraRealTimeChanged(int eraChanged, TimeSpan newDuration)
		{
			if (eraChanged < era)
				return;
			if (eraChanged == era)
			{
				//Change remaining time
				int monthsLeft = eraGameTime - (GetCurrentMonth() % SessionManager.Instance.MspGlobalData.era_total_months);
				if (monthsLeft == 0)
					Debug.LogError("Era should have passed, but didn't. Causing divide by 0.");
				else if(monthsLeft < 0)
					Debug.LogError("Tried to change planning time in simulation phase");

				ServerChangePlanningRealTime(Mathf.CeilToInt((float)newDuration.TotalSeconds / ((float)monthsLeft / (float)eraGameTime)));
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

		//======================================================== Old GameState content ===========================================================

		public PlanningState CurrentState
		{
			get { return gameState; }
		}

		public bool GameStarted
		{
			get { return gameState != PlanningState.Setup; }
		}

		public int GetCurrentMonth()
		{
			return month;
		}

		public void UpdateTime(TimelineState state)
		{
			if (!firstUpdateComplete)
			{
				firstUpdateComplete = true;
				PlanningState tempState = StringToPlanningState(state.state);
				if (tempState != PlanningState.Setup)
					SetupStateExited();
			}

			int prevMonth = month;
			PlanningState prevState = gameState;
			PlanningState prevTransitionState = transitionState;

			gameState = StringToPlanningState(state.state);
			transitionState = string.IsNullOrEmpty(state.transition_state) ? PlanningState.None : StringToPlanningState(state.transition_state);
			month = state.month;
			transitionMonth = state.transition_month;

			//Month change
			if (month != prevMonth)
			{
				// fail-safe: setup month should be -1
				if (gameState == PlanningState.Setup)
				{
					month = -1;
				}

				if (gameState != PlanningState.Setup)
					PlanManager.MonthTick(month);
				InterfaceCanvas.Instance.activePlanWindow.m_timeSelect.UpdateMinTime();

				if (OnCurrentMonthChanged != null)
				{
					OnCurrentMonthChanged.Invoke(prevMonth, month);
				}
			}

			//State change
			if (gameState != prevState)
			{
				TimeBar.instance.UpdateStateAndTimeText();
				//New state entered
				if (gameState == PlanningState.Setup)
				{
					SetupStateEntered();
					OnSetupPhaseStarted();
				}
				else if (gameState == PlanningState.Pause)
				{
					PauseStateEntered();
				}
				else if (gameState == PlanningState.Play)
				{
					PlayStateEntered();
				}
				else if (gameState == PlanningState.FastForward)
				{
					FastForwardStateEntered();
				}
				else if (gameState == PlanningState.Simulation)
				{
					OnSimulationPhaseStarted();
				}
				else if (gameState == PlanningState.End)
				{
					InterfaceCanvas.Instance.menuBarCreatePlan.toggle.interactable = false;
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
			else if(prevTransitionState != transitionState)
			{
				TimeBar.instance.UpdateStateAndTimeText();
			}

			//Update UI
			InterfaceCanvas.Instance.timeBar.UpdateDate(month, transitionMonth);
			UpdateUI(state);
		}

		private void OnSetupPhaseStarted()
		{
			if (!SessionManager.Instance.AreWeGameMaster)
				InterfaceCanvas.Instance.menuBarCreatePlan.toggle.interactable = false;
		}

		private void OnSetupPhaseEnded()
		{
			InterfaceCanvas.Instance.menuBarCreatePlan.toggle.interactable = true;
			//Update plans once the setup state is left, so we don't have to wait for month 1
			TimeManagerWindow.instance.eraDivision.gameObject.SetActive(false);
			PlanManager.MonthTick(month);
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
			SetupStateExited();
		}

		private void OnSimulationPhaseStarted()
		{
			m_screenborderGradient.SetActive(true);
			m_scrollingTextBand.SetActive(true);
			InterfaceCanvas.Instance.menuBarCreatePlan.toggle.interactable = false;
			SimulationStateEntered();
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
		}

		private void OnSimulationPhaseEnded()
		{
			m_screenborderGradient.SetActive(false);
			m_scrollingTextBand.SetActive(false);
			InterfaceCanvas.Instance.menuBarCreatePlan.toggle.interactable = true;
			SimulationStateExited();
			InterfaceCanvas.Instance.plansList.RefreshPlanBarInteractablityForAllPlans();
		}

		public static void SetGameState(string state)
		{
			NetworkForm form = new NetworkForm();

			form.AddField("state", state);
			Debug.Log("Send State " + state.ToString());

			ServerCommunication.Instance.DoRequestForm(Server.SetGameState(), form);
		}

		public static PlanningState StringToPlanningState(string value)
		{
			return (PlanningState)Enum.Parse(typeof(PlanningState), value, true);
		}

		public static string PlanningStateToString(PlanningState state)
		{
			return state.ToString().ToUpper();
		}

		#region ServerCommunication

		private void ServerSetGameState(PlanningState state)
		{
			NetworkForm form = new NetworkForm();
			form.AddField("state", state.ToString());
			ServerCommunication.Instance.DoRequestForm(Server.SetGameState(), form);
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
			ServerCommunication.Instance.DoRequestForm(Server.SetGamePlanningTime(), form);
		}

		private void ServerChangePlanningRealTime(int newSeconds)
		{
			if (newSeconds < 10)
				newSeconds = 10;
			NetworkForm form = new NetworkForm();
			form.AddField("realtime", newSeconds);
			ServerCommunication.Instance.DoRequestForm(Server.SetRealPlanningTime(), form);
		}

		private void ServerChangeAllPlanningRealTime()
		{
			//Format era realtimes into comma separated string
			string times = eraRealTimes[0].ToString();
			for (int i = 1; i < eraRealTimes.Length; i++)
				times += "," + eraRealTimes[i].ToString();

			NetworkForm form = new NetworkForm();
			form.AddField("realtime", times);
			ServerCommunication.Instance.DoRequestForm(Server.SetFuturePlanningTime(), form);
		}
		#endregion
	}
}