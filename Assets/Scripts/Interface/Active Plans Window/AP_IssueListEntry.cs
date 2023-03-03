using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using static System.Net.Mime.MediaTypeNames;
using ColourPalette;

namespace MSP2050.Scripts
{
	public class AP_IssueListEntry : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_issueTypeText = null;
		[SerializeField] TextMeshProUGUI m_issueText = null;
		[SerializeField] Button m_viewIssueButton = null;
		[SerializeField] ColourAsset m_infoIssueColour;
		[SerializeField] ColourAsset m_warningIssueColour;
		[SerializeField] ColourAsset m_errorIssueColour;
		PlanIssueObject m_issue;

		private void Start()
		{
			m_viewIssueButton.onClick.AddListener(OnViewOnMapClicked);
		}

		public void SetIssue(PlanIssueObject a_issue)
		{
			m_issue = a_issue;
			m_viewIssueButton.gameObject.SetActive(true);
			m_issueText.text = ConstraintManager.Instance.GetRestrictionMessage(m_issue.restriction_id);
			switch (m_issue.type)
			{
				case ERestrictionIssueType.Info:
					m_issueTypeText.text = "Info";
					m_issueTypeText.color = m_infoIssueColour.GetColour();
					break;
				case ERestrictionIssueType.Warning:
					m_issueTypeText.text = "Warning";
					m_issueTypeText.color = m_warningIssueColour.GetColour();
					break;
				case ERestrictionIssueType.Error:
					m_issueTypeText.text = "Error";
					m_issueTypeText.color = m_errorIssueColour.GetColour();
					break;
				default:
					m_issueText.text = "Unknown issue type " + m_issue.type;
					break;
			}
			gameObject.SetActive(true);
		}

		public void SetIssue(string a_text)
		{
			m_issue = null;
			m_issueText.text = a_text;
			m_viewIssueButton.gameObject.SetActive(false);
			m_issueTypeText.text = "Error";
			m_issueTypeText.color = m_errorIssueColour.GetColour();
			gameObject.SetActive(true);
		}

		private void OnViewOnMapClicked()
		{
			IssueManager.Instance.ShowRelevantPlanLayersForIssue(m_issue);
			Rect viewBounds = new Rect(m_issue.x, m_issue.y, 1.0f, 1.0f);
			CameraManager.Instance.ZoomToBounds(viewBounds);
		}
	}
}
