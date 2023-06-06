using UnityEditor;
using UnityEngine;

namespace MSP2050.Scripts
{
	public interface IApprovalReason
	{
		string FormatAsText(string a_teamName);
	}
}