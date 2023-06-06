using System.Collections;
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
	}
}