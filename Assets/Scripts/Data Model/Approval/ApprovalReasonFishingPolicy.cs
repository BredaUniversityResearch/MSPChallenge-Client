using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApprovalReasonFishingPolicy : IApprovalReason
	{
		string m_fleet;

		public ApprovalReasonFishingPolicy(string a_fleet)
		{
			m_fleet = a_fleet;
		}

		public string FormatAsText(string a_teamName)
		{
			return $"{a_teamName}'s fishing effort for the {m_fleet} fleet was altered.";
		}

		public string FormatGroupText(List<IApprovalReason> a_group, string a_teamName)
		{
			return FormatAsText(a_teamName);
		}

		public bool ShouldBeGrouped(IApprovalReason a_other)
		{
			return false;
		}
	}
}