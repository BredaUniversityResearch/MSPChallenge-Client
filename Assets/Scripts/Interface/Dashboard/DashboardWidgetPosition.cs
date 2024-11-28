using System;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DashboardWidgetPosition : IComparable<DashboardWidgetPosition>
	{
		int x, y, w, h;

		public int X => x;
		public int Y => y;
		public int W => w;
		public int H => h;

		public int CompareTo(DashboardWidgetPosition other)
		{
			if(other.y == y)
			{
				return x.CompareTo(other.x);
			}
			return y.CompareTo(other.y);
		}

		public void SetPosition(int a_x, int a_y)
		{ 
			x = a_x; 
			y = a_y; 
		}

		public void SetPosition((int y, int x) a_pos)
		{
			x = a_pos.x;
			y = a_pos.y;
		}

		public void SetSize(int a_w, int a_h)
		{
			w = a_w;
			h = a_h;
		}
	}
}
 