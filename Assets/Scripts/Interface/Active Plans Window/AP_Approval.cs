using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class AP_Approval : AP_PopoutWindow
	{
		[SerializeField] Transform planApprovalEntryParent;
		[SerializeField] GameObject planApprovalEntryPrefab;

		List<AP_ApprovalEntry> m_approvalEntries = new List<AP_ApprovalEntry>();

		protected override void Start()
		{
			base.Start();
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			int nextIndex = 0;

			//Set entries
			foreach (var kvp in a_content.countryApproval)
			{
				if (nextIndex < m_approvalEntries.Count)
				{
					m_approvalEntries[nextIndex].SetContent(SessionManager.Instance.GetTeamByTeamID(kvp.Key), kvp.Value);
					m_approvalEntries[nextIndex].gameObject.SetActive(true);
				}
				else
				{
					AP_ApprovalEntry entry = Instantiate(planApprovalEntryPrefab, planApprovalEntryParent).GetComponentInChildren<AP_ApprovalEntry>();
					entry.SetCallback(ApprovalChangedCountry);
					m_approvalEntries.Add(entry);
					entry.SetContent(SessionManager.Instance.GetTeamByTeamID(kvp.Key), kvp.Value);
				}
				nextIndex++;
			}

			//Turn off unused entries
			for (int i = nextIndex; i < m_approvalEntries.Count; i++)
			{
				m_approvalEntries[i].gameObject.SetActive(false);
			}
		}

		public void ApprovalChangedCountry(Team team, EPlanApprovalState newApproval)
		{
			m_plan.AttemptLock((changedPlan) =>
			{
				BatchRequest batch = new BatchRequest();
				m_plan.AttemptUnlock(batch);

				int planId = m_plan.ID;
				if (newApproval == EPlanApprovalState.Disapproved)
				{
					if (team.ID == SessionManager.Instance.CurrentUserTeamID)
						m_plan.SendMessage("Disapproved the plan.", batch);
					else
						m_plan.SendMessage("Disapproved the plan for <color=#" + Util.ColorToHex(team.color) + ">" + team.name + "</color>.", batch);

					SubmitApprovalState(newApproval, team, batch);

				}
				else if (newApproval == EPlanApprovalState.Maybe)
				{
					if (team.ID == SessionManager.Instance.CurrentUserTeamID)
						m_plan.SendMessage("Retracted the previous approval state.", batch);
					else
						m_plan.SendMessage("Retracted the previous approval state for <color=#" + Util.ColorToHex(team.color) + ">" + team.name + "</color>.", batch);

					SubmitApprovalState(newApproval, team, batch);
				}
				else
				{
					if (team.ID == SessionManager.Instance.CurrentUserTeamID)
						m_plan.SendMessage("Approved the plan.", batch);
					else
						m_plan.SendMessage("Approved the plan for <color=#" + Util.ColorToHex(team.color) + ">" + team.name + "</color>.", batch);

					//Set approval immediately so we can check for completion
					m_plan.countryApproval[team.ID] = EPlanApprovalState.Approved;
					if (m_plan.HasApproval())
					{
						m_plan.SubmitState(Plan.PlanState.APPROVED, batch);
					}
					else
						SubmitApprovalState(EPlanApprovalState.Approved, team, batch);
				}
				batch.ExecuteBatch(null, null);
			}, null);
		}

		void SubmitApprovalState(EPlanApprovalState state, Team country, BatchRequest batch)
		{
			JObject dataObject = new JObject();

			dataObject.Add("plan", m_plan.ID);
			dataObject.Add("country", country.ID);
			dataObject.Add("vote", (int)state);

			batch.AddRequest(Server.SetApproval(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
		}
	}
}
