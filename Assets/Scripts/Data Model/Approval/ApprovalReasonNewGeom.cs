using System.Collections;
using System.Collections.Generic;
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
				return $"Geometry on the {m_newGeometry.Layer.ShortName} layer was added or altered in {a_teamName}'s EEZ.";
			else
				return $"Geometry belonging to {a_teamName} was added or altered on the {m_newGeometry.Layer.ShortName} layer.";
		}

		public string FormatGroupText(List<IApprovalReason> a_group, string a_teamName)
		{
			bool single = a_group.Count == 1;
			if (m_eezOverlap)
				return $"{a_group.Count} piece{(single ? "" : "s")} of geometry on the {m_newGeometry.Layer.ShortName} layer {(single ? "was" : "were")} added or altered in {a_teamName}'s EEZ.";
			else
				return $"{a_group.Count} piece{(single ? "" : "s")} of geometry belonging to {a_teamName} {(single ? "was" : "were")} added or altered on the {m_newGeometry.Layer.ShortName} layer.";
		}

		public bool ShouldBeGrouped(IApprovalReason a_other)
		{
			ApprovalReasonNewGeom otherCast = a_other as ApprovalReasonNewGeom;
			return otherCast != null && otherCast.m_eezOverlap == m_eezOverlap && otherCast.m_newGeometry.Layer == m_newGeometry.Layer;
		}
	}
}