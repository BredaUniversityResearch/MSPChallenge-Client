using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using static MSP2050.Scripts.Plan;

namespace MSP2050.Scripts
{
	public class AP_StateSelect : AP_PopoutWindow
	{
		[SerializeField] CustomDropdown m_statusDropdown;
		[SerializeField] TextMeshProUGUI m_infoText;
		[SerializeField] GameObject m_buttonSection;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;

		List<PlanState> m_stateDropdownOptions;

		protected override void Start()
		{
			base.Start();
			m_confirmButton.onClick.AddListener(OnAccept);
			m_cancelButton.onClick.AddListener(TryClose);
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			m_statusDropdown.interactable = false;
			m_statusDropdown.captionText.text = a_content.State.GetDisplayName();

			//If simulation, disable all changeable UI elements
			if (TimeManager.Instance.CurrentState == TimeManager.PlanningState.Simulation)
			{
				m_infoText.gameObject.SetActive(true);
				m_buttonSection.SetActive(false);
				m_infoText.text = "Plan details cannot be changed while the game is in simulation mode. Please wait for the simulation to end.";
			}
			else if (a_APWindow.Editing)
			{
				m_infoText.gameObject.SetActive(true);
				m_buttonSection.SetActive(false);
				m_infoText.text = "The plan's state cannot be changed in edit mode. Finish editing before changing the plan's state.";
			}
			else if (m_plan.RequiresTimeChange)
			{
				m_infoText.gameObject.SetActive(true);
				m_buttonSection.SetActive(false);
				m_infoText.text = "The plan's construction start time has passed. To restore the plan, change its implementation date.";
			}
			else if (a_content.State == Plan.PlanState.IMPLEMENTED)
			{
				m_infoText.gameObject.SetActive(true);
				m_buttonSection.SetActive(false);
				m_infoText.text = "Implemented plans cannot change state.";
			}
			else
			{ 
				m_statusDropdown.interactable = true;
				SetStatusDropdownOptions();
				m_infoText.gameObject.SetActive(false);
				m_buttonSection.SetActive(true);
			}
		}

		void OnAccept()
		{
			m_contentToggle.ForceClose(true); //applies content
			m_APWindow.RefreshContent();
		}

		public override bool MayClose(out bool a_applyChanges)
		{
			a_applyChanges = false;
			return true;
		}

		public override void ApplyContent()
		{
			if (!m_statusDropdown.interactable)
				return;
			if (m_statusDropdown.value >= m_stateDropdownOptions.Count)
				Debug.LogError("State of higher index than available states selected");

			PlanState newState = m_stateDropdownOptions[m_statusDropdown.value];

			if (newState == m_plan.State)
				return;
			if (m_plan.RequiresTimeChange)
			{
				DialogBoxManager.instance.NotificationWindow("Cannot change state", "The plan's construction start time has passed. To restore the plan, change its implementation date.", () => { }, "Dismiss");
				for (int i = 0; i < m_stateDropdownOptions.Count; i++)
				{
					if (m_stateDropdownOptions[i] == m_plan.State)
					{
						m_statusDropdown.value = i;
						//m_statusDropdown.captionText.text = "State: " + m_plan.State.GetDisplayName();
						break;
					}
				}
			}
			else
			{
				AcceptStatus(newState);
			}
		}

		void SetStatusDropdownOptions()
		{
			m_stateDropdownOptions = null;

			//Set the right available states
			if (m_plan.HasErrors() || m_plan.State == PlanState.DELETED)
				m_stateDropdownOptions = new List<PlanState>() { PlanState.DELETED, PlanState.DESIGN };
			else if (m_plan.NeedsApproval())
				m_stateDropdownOptions = SessionManager.Instance.AreWeManager ?
					new List<PlanState>() { PlanState.DELETED, PlanState.DESIGN, PlanState.CONSULTATION, PlanState.APPROVAL, PlanState.APPROVED }
					: new List<PlanState>() { PlanState.DELETED, PlanState.DESIGN, PlanState.CONSULTATION, PlanState.APPROVAL };
			else
				m_stateDropdownOptions = new List<PlanState>() { PlanState.DELETED, PlanState.DESIGN, PlanState.CONSULTATION, PlanState.APPROVED };

			//Add the current state if it wasnt already available
			if (!m_stateDropdownOptions.Contains(m_plan.State))
				m_stateDropdownOptions.Add(m_plan.State);

			//Recreate dropdown options
			m_statusDropdown.ClearOptions();
			List<string> options = new List<string>(m_stateDropdownOptions.Count);
			foreach (PlanState state in m_stateDropdownOptions)
				options.Add(state.GetDisplayName());
			m_statusDropdown.AddOptions(options);

			//Set the dropdown value to the right state
			for (int i = 0; i < m_stateDropdownOptions.Count; i++)
			{
				if (m_stateDropdownOptions[i] == m_plan.State)
				{
					m_statusDropdown.value = i;
					//m_statusDropdown.captionText.text = "State: " + m_plan.State.GetDisplayName();
					break;
				}
			}
		}

		public void AcceptStatus(PlanState newState)
		{
			InterfaceCanvas.ShowNetworkingBlocker();
			//Lock plan, if successful, change state
			m_plan.AttemptLock((changedPlan) =>
			{
				bool submitDelayed = false;
				BatchRequest batch = new BatchRequest();
				changedPlan.AttemptUnlock(batch);

				if (changedPlan.Policies.ContainsKey(PolicyManager.ENERGY_POLICY_NAME))
				{
					if (changedPlan.InInfluencingState && !newState.IsInfluencingState())
					{
						//Check for later plans overlapping with this one
						submitDelayed = true;
						NetworkForm form = new NetworkForm();
						form.AddField("plan_id", changedPlan.GetDataBaseOrBatchIDReference());
						ServerCommunication.Instance.DoRequest<int[]>(Server.GetDependentEnergyPlans(), form, (planErrorsIDs) => CreatePlanChangeConfirmPopup(planErrorsIDs, changedPlan, newState, batch));
					}
					else if (!changedPlan.InInfluencingState && newState.IsInfluencingState())
					{
						submitDelayed = true;

						//Check if all cables are still valid
						if (PolicyLogicEnergy.Instance.CheckForInvalidCables(changedPlan))
						{
							//Create notification window
							DialogBoxManager.instance.NotificationWindow("Invalid energy plan", "Another plan changed state while this plan was being edited, invalidating this plan. Edit this plan to fix the error.", null);
							PolicyLogicEnergy.Instance.SubmitEnergyError(changedPlan, true, false, batch);
							batch.ExecuteBatch(HideBlocker, HideBlocker);
							return;
						}

						//Check if the plan wasnt invalidated while being edited
						Debug.Log("Request prev overlap for plan: " + changedPlan.ID);
						NetworkForm form = new NetworkForm();
						form.AddField("plan_id", changedPlan.GetDataBaseOrBatchIDReference());
						ServerCommunication.Instance.DoRequest<int>(Server.OverlapsWithPreviousEnergyPlans(), form, (i) =>
						{
							if (i == 0)
							{
								//Check for later plans overlapping with this one
								NetworkForm form2 = new NetworkForm();
								form2.AddField("plan_id", changedPlan.GetDataBaseOrBatchIDReference());
								ServerCommunication.Instance.DoRequest<int[]>(Server.GetOverlappingEnergyPlans(), form2, (planErrorsIDs2) => CreatePlanChangeConfirmPopup(planErrorsIDs2, changedPlan, newState, batch));
							}
							else
							{
								DialogBoxManager.instance.NotificationWindow("Invalid energy plan", "Another plan changed state while this plan was being edited, invalidating this plan. Edit this plan to fix the error.", null);
								batch.ExecuteBatch(HideBlocker, HideBlocker);
							}
						});
					}
					else if (changedPlan.State == Plan.PlanState.DELETED)
					{
						//If an energy plan is moved out of archived, always add an energy error
						PolicyLogicEnergy.Instance.SubmitEnergyError(changedPlan, true, false, batch);
					}
				}

				if (!submitDelayed)
				{
					changedPlan.SendMessage("Changed the plans status to: " + newState.GetDisplayName(), batch);
					changedPlan.SubmitState(newState, batch);
					batch.ExecuteBatch(HideBlocker, SubmissionFailure);
				}
			}, delegate {
				InterfaceCanvas.HideNetworkingBlocker();
			});
		}

		void HideBlocker(BatchRequest a_batch)
		{
			m_contentToggle.ForceClose(false);
			InterfaceCanvas.HideNetworkingBlocker();
		}

		static void SubmissionFailure(BatchRequest a_batch)
		{ 
			InterfaceCanvas.HideNetworkingBlocker();
			DialogBoxManager.instance.NotificationWindow("Submitting state failed", "There was an error when submitting the plan's state change to the server. Please try again or see the error log for more information.", null);
		}

		private void CreatePlanChangeConfirmPopup(int[] planErrorsIDs, Plan plan, PlanState targetState, BatchRequest batch)
		{
			if (planErrorsIDs == null || planErrorsIDs.Length == 0)
			{
				//No plans with errors, set new state
				plan.SendMessage("Changed the plans status to: " + targetState.GetDisplayName(), batch);
				plan.SubmitState(targetState, batch);
				batch.ExecuteBatch(HideBlocker, HideBlocker);
			}
			else
			{
				//Will cause errors in other plans, ask for confirmation
				Plan errorPlan = PlanManager.Instance.GetPlanWithID(planErrorsIDs[0]);

				//Create text for warning message, with names of affected plans
				StringBuilder notificationText = new StringBuilder(256);
				notificationText.Append("Changing this plan's state will cause errors for other plans, they will be moved to design and need to have their energy distribution confirmed.\n\nThe affected plans are:\n\n");
				for (int i = 0; i < planErrorsIDs.Length && i < 4; i++)
				{
					errorPlan = PlanManager.Instance.GetPlanWithID(planErrorsIDs[i]);
					//notificationText.Append("<color=#").Append(Util.ColorToHex(SessionManager.Instance.GetTeamByTeamID(errorPlan.Country).color)).Append(">");
					notificationText.Append(" - ").Append(errorPlan.Name).Append("\n");
					//notificationText.Append("</color>");
				}
				if (planErrorsIDs.Length > 4)
					notificationText.Append("and " + (planErrorsIDs.Length - 4).ToString() + " others.");

				//Create confirmation window
				BatchRequest batchRequest = batch; //Create local variable for batch
				string description = notificationText.ToString();
				UnityEngine.Events.UnityAction lb = new UnityEngine.Events.UnityAction(() =>
				{
					batchRequest.ExecuteBatch(HideBlocker, HideBlocker); //Only calls unlock
				});
				UnityEngine.Events.UnityAction rb = new UnityEngine.Events.UnityAction(() =>
				{
					plan.SendMessage("Changed the plans status to: " + targetState.GetDisplayName(), batch);
					plan.SubmitState(targetState, batchRequest);
					batchRequest.ExecuteBatch(HideBlocker, SubmissionFailure);
				});
				DialogBoxManager.instance.ConfirmationWindow("Energy error warning", description, lb, rb);
			}
		}

		public static void SubmitPlanRecovery(Plan a_plan)
		{
			InterfaceCanvas.ShowNetworkingBlocker();
			BatchRequest batch = new BatchRequest();
			a_plan.AttemptUnlock(batch);
			a_plan.SendMessage("Restored the plans status to: " + PlanState.DESIGN.GetDisplayName(), batch);
			a_plan.SubmitState(PlanState.DESIGN, batch);
			batch.ExecuteBatch((_) => { InterfaceCanvas.HideNetworkingBlocker(); }, SubmissionFailure);
		}
	}
}
