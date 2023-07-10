using System.Collections;
using System.Collections.Generic;
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

		public string FormatGroupText(List<IApprovalReason> a_group, string a_teamName)
		{
			bool single = a_group.Count == 1;
			return $"{a_group.Count} piece{(single ? "" : "s")} of geometry belonging to {a_teamName} {(single ? "was" : "were")} removed on the {m_removedGeometry.m_entity.Layer.ShortName} layer.";
		}

		public bool ShouldBeGrouped(IApprovalReason a_other)
		{
			ApprovalReasonRemovedGeom otherCast = a_other as ApprovalReasonRemovedGeom;
			return otherCast != null && otherCast.m_removedGeometry.m_entity.Layer == m_removedGeometry.m_entity.Layer;
		}
	}
}