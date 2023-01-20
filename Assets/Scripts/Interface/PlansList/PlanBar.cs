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
		//[SerializeField] Image m_lockIcon;
		[SerializeField] Image m_countryIcon;
		[SerializeField] Button m_forceUnlockButton;
		[SerializeField] RectTransform m_countryIconRect;
		[SerializeField] Sprite m_regularCountrySprite;
		[SerializeField] Sprite m_lockedCountrySprite;
		[SerializeField] float m_lockedCountrySize;
		[SerializeField] float m_regularCountrySize;

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
			this.m_plan = a_plan;
			UpdateInfo();
			m_barToggle.onValueChanged.AddListener((b) =>
			{
				if (!m_ignoreBarCallback)
				{
					if(b)
						PlanManager.Instance.ShowPlan(a_plan);
					else
						PlanManager.Instance.HideCurrentPlan();

				}
			});
			PlanManager.Instance.OnViewingPlanChanged += OnViewedPlanChanged;
			UpdateActionRequired();
			m_forceUnlockButton.interactable = false;
			m_forceUnlockButton.onClick.AddListener(OnForceUnlockClicked);
		}

		private void OnDestroy()
		{
			PlanManager.Instance.OnViewingPlanChanged -= OnViewedPlanChanged;
		}

		public void UpdateInfo()
		{
			m_title.text = m_plan.Name;
			m_date.text = Util.MonthToText(m_plan.StartTime, true);
			m_countryIcon.color = SessionManager.Instance.GetTeamByTeamID(m_plan.Country).color;
			m_stateIcon.sprite = VisualizationUtil.Instance.VisualizationSettings.GetplanStateSprite(m_plan.State);
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

		void OnViewedPlanChanged(Plan a_newPlan)
		{
			if(m_barToggle.isOn && a_newPlan != m_plan)
			{
				m_ignoreBarCallback = true;
				m_barToggle.isOn = false;
				m_ignoreBarCallback = false;
			}
			else if(a_newPlan == m_plan)
			{
				m_ignoreBarCallback = true;
				m_barToggle.isOn = true;
				m_ignoreBarCallback = false;
			}
		}

		public void SetPlanBarToggleInteractability(bool a_value)
		{
			m_barToggle.interactable = a_value;
		}

		public void SetLockActive(bool a_value)
		{
			m_countryIcon.sprite = a_value ? m_lockedCountrySprite : m_regularCountrySprite;
			m_countryIconRect.sizeDelta = a_value ? new Vector2(m_lockedCountrySize, m_lockedCountrySize) : new Vector2(m_regularCountrySize, m_regularCountrySize);
			m_forceUnlockButton.interactable = a_value && SessionManager.Instance.AreWeGameMaster;
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

		void OnForceUnlockClicked()
		{
			DialogBoxManager.instance.ConfirmationWindow("Force unlock plan", $"Are you sure you want to force unlock the plan: {m_plan.Name}? If anyone is still editing this plan their changes will be discarded.", null, ForceCancel);
		}

		void ForceCancel()
		{
			m_plan.AttemptUnlock(true);
		}
	}
}