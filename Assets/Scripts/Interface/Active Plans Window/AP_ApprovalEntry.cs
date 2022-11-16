using ColourPalette;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class AP_ApprovalEntry : MonoBehaviour
	{
		[SerializeField]
		TextMeshProUGUI m_countryNameText;

		[SerializeField] Button m_yesButton;
		[SerializeField] Button m_noButton;
		[SerializeField] Button m_maybeButton;

		[SerializeField] Image m_yesIcon;
		[SerializeField] Image m_noIcon;
		[SerializeField] Image m_maybeIcon;

		bool m_playerCanChangeApproval;
		Team m_currentTeam;
		public ApprovalButtonCallback m_approvalButtonCallback;
		public delegate void ApprovalButtonCallback(Team a_country, EPlanApprovalState a_newApproval);

		private void Start()
		{
			m_yesButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Approved));
			m_noButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Disapproved));
			m_maybeButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Maybe));
		}

		public void SetCallback(ApprovalButtonCallback a_callback)
		{
			m_approvalButtonCallback = a_callback;
		}

		public void SetContent(Team a_country, EPlanApprovalState a_state)
		{
			m_currentTeam = a_country;
			m_countryNameText.text = a_country.name;
			m_yesIcon.color = a_country.color;
			m_noIcon.color = a_country.color;
			m_maybeIcon.color = a_country.color;
			if(SessionManager.Instance.CurrentTeam.ID == a_country.ID)
			{
				m_playerCanChangeApproval = true;
			}
			else
			{
				m_playerCanChangeApproval = SessionManager.Instance.AreWeGameMaster;
			}
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
	}
}

