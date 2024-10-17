using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApprovalReasonFishingPolicy : IApprovalReason
	{
		int m_gearType;
		bool m_requireAll;

		public ApprovalReasonFishingPolicy(int a_gearType, bool a_requireAll = false)
		{
			m_gearType = a_gearType;
			m_requireAll = a_requireAll;
		}

		public string FormatAsText(string a_teamName)
		{
			if (m_requireAll)
				return $"A team's fishing effort was altered, requiring approval from all teams.";
			else
				return $"{a_teamName}'s fishing effort for the {PolicyLogicFishing.Instance.GetGearName(m_gearType)} fleet was altered.";
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