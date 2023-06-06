using ColourPalette;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class AP_ApprovalEntry : MonoBehaviour
	{
		[SerializeField]
		TextMeshProUGUI m_countryNameText;

		[SerializeField] Button m_yesButton;
		[SerializeField] Button m_noButton;
		[SerializeField] Button m_maybeButton;
		[SerializeField] Button m_whyButton;

		[SerializeField] Graphic m_yesIcon;
		[SerializeField] Graphic m_noIcon;
		[SerializeField] Graphic m_maybeIcon;

		bool m_playerCanChangeApproval;
		Team m_currentTeam;
		Plan m_plan;
		public ApprovalButtonCallback m_approvalButtonCallback;
		public delegate void ApprovalButtonCallback(Team a_country, EPlanApprovalState a_newApproval);

		private void Start()
		{
			m_yesButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Approved));
			m_noButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Disapproved));
			m_maybeButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Maybe));
			m_whyButton.onClick.AddListener(OnWhyButtonPressed);
		}

		public void SetCallback(ApprovalButtonCallback a_callback)
		{
			m_approvalButtonCallback = a_callback;
		}

		public void SetContent(Team a_country, EPlanApprovalState a_state, bool a_inApproval, Plan a_plan)
		{
			m_plan = a_plan;
			m_currentTeam = a_country;
			m_countryNameText.text = a_country.name;
			m_yesIcon.color = a_country.color;
			m_noIcon.color = a_country.color;
			m_maybeIcon.color = a_country.color;
			m_playerCanChangeApproval = (SessionManager.Instance.CurrentTeam.ID == a_country.ID || SessionManager.Instance.AreWeGameMaster) && a_inApproval;
			SetApprovalState(a_state);
		}

		void SetApprovalState(EPlanApprovalState a_state)
		{
			m_yesButton.gameObject.SetActive(m_playerCanChangeApproval && a_state != EPlanApprovalState.Approved);
			m_noButton.gameObject.SetActive(m_playerCanChangeApproval && a_state != EPlanApprovalState.Disapproved);
			m_maybeButton.gameObject.SetActive(m_playerCanChangeApproval && a_state != EPlanApprovalState.Maybe);
			m_yesIcon.gameObject.SetActive(a_state == EPlanApprovalState.Approved);
			m_noIcon.gameObject.SetActive(a_state == EPlanApprovalState.Disapproved);
			m_maybeIcon.gameObject.SetActive(a_state == EPlanApprovalState.Maybe);
		}

		void ApprovalButtonPressed(EPlanApprovalState a_state)
		{
			m_approvalButtonCallback.Invoke(m_currentTeam, a_state);
		}

		void OnWhyButtonPressed()
		{
			if (m_plan.countryApprovalReasons != null && m_plan.countryApprovalReasons.TryGetValue(m_currentTeam.ID, out var reasons))
			{
				List<string> explanations = new List<string>(reasons.Count);
				foreach (IApprovalReason reason in reasons)
				{
					explanations.Add(reason.FormatAsText(m_currentTeam.name));
				}
				DialogBoxManager.instance.NotificationListWindow("Approval requirements",
					$"This plan requires the {m_currentTeam.name} team's approval for the following reasons:\n\n",
					explanations, null);
			}
			else
			{
				DialogBoxManager.instance.NotificationWindow("Approval requirements", "No up to date approval requirements for this team were found, please recalculate required approval.", null);
			}
		}
	}
}

