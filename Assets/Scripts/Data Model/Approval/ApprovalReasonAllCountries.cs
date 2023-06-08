using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApprovalReasonAllCountries : IApprovalReason
	{
		EntityType m_type;
		Entity m_entity;
		bool m_removed;

		public ApprovalReasonAllCountries(EntityType a_type, Entity a_entity, bool a_removed)
		{
			m_type = a_type;
			m_entity = a_entity;
			m_removed = a_removed;
		}

		public string FormatAsText(string a_teamName)
		{
			if (m_removed)
				return $"Geometry of type {m_type.Name} was removed, which requires approval from all countries.";
			else
				return $"Geometry of type {m_type.Name} was added or moved, which requires approval from all countries.";
		}

		public string FormatGroupText(List<IApprovalReason> a_group, string a_teamName)
		{
			bool single = a_group.Count == 1;
			if (m_removed)
				return $"{a_group.Count} piece{(single ? "" : "s")} of geometry of type {m_type.Name} {(single ? "was" : "were")} removed, which requires approval from all countries.";
			else
				return $"{a_group.Count} piece{(single ? "" : "s")} of geometry of type {m_type.Name} {(single ? "was" : "were")} added or moved, which requires approval from all countries.";
		}

		public bool ShouldBeGrouped(IApprovalReason a_other)
		{
			ApprovalReasonAllCountries otherCast = a_other as ApprovalReasonAllCountries;
			return otherCast != null && otherCast.m_removed == m_removed && otherCast.m_type == m_type;
		}
	}
}