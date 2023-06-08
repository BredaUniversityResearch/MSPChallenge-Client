using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public interface IApprovalReason
	{
		string FormatAsText(string a_teamName);
		bool ShouldBeGrouped(IApprovalReason a_other);
		string FormatGroupText(List<IApprovalReason> a_group, string a_teamName);
	}
}