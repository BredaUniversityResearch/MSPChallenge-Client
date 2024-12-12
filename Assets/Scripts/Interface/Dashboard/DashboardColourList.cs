using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	[CreateAssetMenu(fileName = "DashboardColourList", menuName = "MSP2050/DashboardColourList")]
	public class DashboardColourList : ScriptableObject
	{
		public List<Color> m_colours;

		public Color GetColour(int a_index)
		{
			return m_colours[a_index % m_colours.Count];
		}
	}
}