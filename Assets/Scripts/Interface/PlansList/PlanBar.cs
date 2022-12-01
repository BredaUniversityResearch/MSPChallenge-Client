using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ColourPalette;
using System;

namespace MSP2050.Scripts
{
	public class PlanBar : MonoBehaviour
	{
		[SerializeField] Image m_lockIcon;
		[SerializeField] Image m_countryIcon;
		[SerializeField] Image m_actionRequiredIcon;
		[SerializeField] Image m_stateIcon;
		[SerializeField] TextMeshProUGUI m_title;
		[SerializeField] TextMeshProUGUI m_date;
		[SerializeField] Toggle m_barToggle;
		[SerializeField] ColourAsset m_errorColour;
		[SerializeField] ColourAsset m_actionRequiredColour;

		private Plan m_plan;
		private bool m_ignoreBarCallback;
		private bool m_actionWasRequired;
		private bool m_hasError;
		private bool m_hiddenByFilter;
		private bool m_hiddenByVisibility;
		private PlansGroupBar m_group;

		public void Initialise(Plan a_plan)
		{
			UpdateInfo();
			this.m_plan = a_plan;

			m_barToggle.onValueChanged.AddListener((b) =>
			{
				if (!m_ignoreBarCallback && b)
				{
					PlanManager.Instance.ShowPlan(a_plan);
				}
			});
			UpdateActionRequired();
		}

		public void UpdateInfo()
		{
			m_title.text = m_plan.Name;
			m_date.text = Util.MonthToText(m_plan.StartTime, true);
			m_countryIcon.color = SessionManager.Instance.GetTeamByTeamID(m_plan.Country).color;
		}

		public void UpdateActionRequired()
		{
			bool actionRequired = false;
			if (m_plan.State == Plan.PlanState.APPROVAL)
			{
				EPlanApprovalState approvalState;
				if (m_plan.countryApproval.TryGetValue(SessionManager.Instance.CurrentUserTeamID, out approvalState))
				{
					if (approvalState == EPlanApprovalState.Maybe)
					{
						actionRequired = true;
					}
				}
			}

			if (actionRequired)
			{
				PlayerNotifications.AddApprovalActionRequiredNotification(m_plan);
				m_actionRequiredIcon.color = m_actionRequiredColour.GetColour();
				m_actionRequiredIcon.gameObject.SetActive(true);
			}
			else
			{
				PlayerNotifications.RemoveApprovalActionRequiredNotification(m_plan);
				if(m_hasError)
				{
					m_actionRequiredIcon.color = m_errorColour.GetColour();
				}
				else
				{
					m_actionRequiredIcon.gameObject.SetActive(false);
				}
			}

			m_actionWasRequired = actionRequired;
		}

		public void SetIssue(ERestrictionIssueType a_issue)
		{
			if(a_issue == ERestrictionIssueType.Error)
			{
				m_hasError = true;
				if(!m_actionWasRequired)
				{
					m_actionRequiredIcon.gameObject.SetActive(true);
					m_actionRequiredIcon.color = m_errorColour.GetColour();
				}
			}
			else if (m_hasError)
			{
				m_hasError = false;
				if (!m_actionWasRequired)
					m_actionRequiredIcon.gameObject.SetActive(false);
			}
		}

		public void SetPlanBarToggleValue(bool a_value)
		{
			m_ignoreBarCallback = true;
			m_barToggle.isOn = a_value;
			m_ignoreBarCallback = false;
		}

		public void SetPlanBarToggleInteractability(bool a_value)
		{
			m_barToggle.interactable = a_value;
		}

		public void SetLockActive(bool a_value)
		{
			m_lockIcon.gameObject.SetActive(true);
		}

		public void MoveToGroup(PlansGroupBar a_group)
		{
			transform.SetParent(a_group.ContentParent);

			if (m_group != null)
				m_group.CheckEmpty();
			m_group = a_group;
		}

		public void MoveToParent(Transform a_parent)
		{
			transform.SetParent(a_parent);

			if (m_group != null)
				m_group.CheckEmpty();
			m_group = null;
		}

		public void Filter(string a_filter)
		{
			if(string.IsNullOrEmpty(a_filter))
			{
				m_hiddenByFilter = false;
			}
			else
			{
				m_hiddenByFilter = m_plan.Name.IndexOf(a_filter, StringComparison.OrdinalIgnoreCase) >= 0;
				if(!m_hiddenByFilter)
				{
					foreach(PlanLayer pl in m_plan.PlanLayers)
					{
						m_hiddenByFilter = pl.BaseLayer.ShortName.IndexOf(a_filter, StringComparison.OrdinalIgnoreCase) >= 0;
						if (m_hiddenByFilter)
							break;
					}
				}
			}
			UpdateActivity();
		}

		public void SetPlanVisibility(bool a_value)
		{
			m_hiddenByVisibility = !a_value;
			UpdateActivity();
		}

		void UpdateActivity()
		{
			gameObject.SetActive(!m_hiddenByFilter && !m_hiddenByVisibility);
		}
	}
}