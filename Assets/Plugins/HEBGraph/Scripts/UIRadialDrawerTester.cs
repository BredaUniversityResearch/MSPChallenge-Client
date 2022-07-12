using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEBGraph
{
	public class UIRadialDrawerTester : MonoBehaviour
	{
		[SerializeField] UIRadialDrawer m_drawer;
		[SerializeField] float[] m_gaps;

		private void Start()
		{
			List<(float start, float end)> gaps = new List<(float start, float end)>();
			for(int i = 0; i < m_gaps.Length-1; i+=2)
			{
				gaps.Add((m_gaps[i], m_gaps[i + 1]));
			}

			m_drawer.SetGaps(gaps);
		}

	}
}
