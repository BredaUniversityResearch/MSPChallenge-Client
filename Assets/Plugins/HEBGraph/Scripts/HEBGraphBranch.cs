using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HEBGraph
{
	public class HEBGraphBranch
	{
		HEBGraphLeaf[] m_children;
		Vector2 m_position;
		TextMeshProUGUI m_groupText;
		string m_name;

		public Vector2 Position => m_position;

		public HEBGraphBranch(HEBGraphDataGroup a_data, HEBGraph a_root)
		{
			m_children = a_root.CreateEntries(a_data.entries, this);
			m_name = a_data.name;
		}

		public void SetPosition(float a_totalRadialOffset, HEBGraph a_root)
		{
			foreach (HEBGraphLeaf entry in m_children)
				entry.SetPosition(a_totalRadialOffset);

			//*0.5f for average and *2f for Pi circle cancel eachother out
			float theta = (m_children[m_children.Length - 1].RadialOffset + m_children[0].RadialOffset) / a_totalRadialOffset * Mathf.PI;
			m_position = (new Vector2(Mathf.Sin(theta) / 2f + 0.5f, Mathf.Cos(theta) / 2f + 0.5f) + new Vector2(0.5f, 0.5f)) * 0.5f;

			m_groupText = a_root.CreateGroupText(-(90f - theta * Mathf.Rad2Deg), m_name.Length < 12);
			m_groupText.text = m_name;
		}
	}
}

