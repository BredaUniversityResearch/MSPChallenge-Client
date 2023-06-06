using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApprovalReasonNewGeom : IApprovalReason
	{
		Entity m_newGeometry;
		bool m_eezOverlap;

		public ApprovalReasonNewGeom(Entity a_newGeometry, bool a_eezOverlap)
		{
			m_newGeometry = a_newGeometry;
			m_eezOverlap = a_eezOverlap;
		}

		public string FormatAsText(string a_teamName)
		{
			if (m_eezOverlap)
				return $"Geometry was added or altered in {a_teamName}'s EEZ.";
			else
				return $"Geometry belonging to {a_teamName} was added or altered.";
		}
	}
}