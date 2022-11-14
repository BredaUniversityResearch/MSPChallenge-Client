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
			switch (m_issue.type)
			{
				case ERestrictionIssueType.Info:
					m_issueText.text = $"<color=#{Util.ColorToHex(m_infoIssueColour.GetColour())}>[Info]</color> " + ConstraintManager.Instance.GetRestrictionMessage(m_issue.restriction_id);
					break;
				case ERestrictionIssueType.Warning:
					m_issueText.text = $"<color=#{Util.ColorToHex(m_warningIssueColour.GetColour())}>[Warning]</color> " + ConstraintManager.Instance.GetRestrictionMessage(m_issue.restriction_id);
					break;
				case ERestrictionIssueType.Error:
					m_issueText.text = $"<color=#{Util.ColorToHex(m_errorIssueColour.GetColour())}>[Error]</color> " + ConstraintManager.Instance.GetRestrictionMessage(m_issue.restriction_id);
					break;
				default:
					m_issueText.text = "Unknow issue type " + m_issue.type;
					break;
			}
			gameObject.SetActive(true);
		}

		public void SetIssue(string a_text)
		{
			m_issue = null;
			m_issueText.text = a_text;
			m_viewIssueButton.gameObject.SetActive(false);
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
