using System.Collections;
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
	}
}