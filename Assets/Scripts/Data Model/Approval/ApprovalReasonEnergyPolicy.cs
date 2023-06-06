using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApprovalReasonEnergyPolicy : IApprovalReason
	{
		EnergyGrid m_grid;
		bool m_removed;

		public ApprovalReasonEnergyPolicy(EnergyGrid a_grid, bool a_removed)
		{
			m_grid = a_grid;
			m_removed = a_removed;
		}

		public string FormatAsText(string a_teamName)
		{
			if (m_removed)
				return $"The energy grid \"{m_grid.m_name}\" was removed, which contained one of {a_teamName}'s sockets.";
			else
				return $"The energy grid \"{m_grid.m_name}\" was changed, which contains one of {a_teamName}'s sockets.";
		}
	}
}