using System.Collections;
using System.Collections.Generic;
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

		public string FormatGroupText(List<IApprovalReason> a_group, string a_teamName)
		{
			if (a_group.Count == 1)
				return FormatAsText(a_teamName);
			if (m_removed)
				return $"{a_group.Count} energy grids were removed, which contained one of {a_teamName}'s sockets.";
			else
				return $"{a_group.Count} energy grids were added or moved, which contain one of {a_teamName}'s sockets.";
		}

		public bool ShouldBeGrouped(IApprovalReason a_other)
		{
			ApprovalReasonEnergyPolicy otherCast = a_other as ApprovalReasonEnergyPolicy;
			return otherCast != null && otherCast.m_removed == m_removed && otherCast.m_grid == m_grid;
		}
	}
}