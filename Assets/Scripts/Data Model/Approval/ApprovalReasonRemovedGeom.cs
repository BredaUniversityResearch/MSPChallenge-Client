using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApprovalReasonRemovedGeom : IApprovalReason
	{
		SubEntity m_removedGeometry;

		public ApprovalReasonRemovedGeom(SubEntity a_removedGeometry)
		{
			m_removedGeometry = a_removedGeometry;
		}

		public string FormatAsText(string a_teamName)
		{
			return $"Geometry belonging to {a_teamName} was removed.";
		}
	}
}