using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reactive.Joins;

namespace MSP2050.Scripts
{
	public class AP_IssueList : AP_PopoutWindow
	{
		[SerializeField] GameObject m_entryPrefab;
		[SerializeField] Transform m_entryParent;
		[SerializeField] GameObject m_noIssuesEntry;

		List<AP_IssueListEntry> m_entries = new List<AP_IssueListEntry>();

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{	
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			//Plan (layer) issues
			int nextIssueIndex = 0;
			if (a_content.PlanLayers != null)
			{
				foreach (PlanLayer planlayer in a_content.PlanLayers)
				{
					if (planlayer.issues != null)
					{
						foreach (PlanIssueObject issue in planlayer.issues)
						{
							if (nextIssueIndex < m_entries.Count)
							{
								m_entries[nextIssueIndex].SetIssue(issue);
							}
							else
							{
								AP_IssueListEntry newInstance = Instantiate(m_entryPrefab, m_entryParent).GetComponent<AP_IssueListEntry>();
								newInstance.SetIssue(issue);
								m_entries.Add(newInstance);
							}
							nextIssueIndex++;
						}
					}
				}
			}

			//Policy issues
			List<string> policyIssues = new List<string>();
			PolicyManager.Instance.GetPolicyIssueText(a_content, policyIssues);
			foreach (string issue in policyIssues)
			{
				if (nextIssueIndex < m_entries.Count)
				{
					m_entries[nextIssueIndex].SetIssue(issue);
				}
				else
				{
					AP_IssueListEntry newInstance = Instantiate(m_entryPrefab, m_entryParent).GetComponent<AP_IssueListEntry>();
					newInstance.SetIssue(issue);
					m_entries.Add(newInstance);
				}
				nextIssueIndex++;
			}

			m_noIssuesEntry.SetActive(nextIssueIndex == 0);

			for (; nextIssueIndex < m_entries.Count; nextIssueIndex++)
			{
				m_entries[nextIssueIndex].gameObject.SetActive(false);
			}
		}
	}
}
