using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DashboardWidgetLayout
	{
		const int columns = 5;
		List<ADashboardWidget> m_widgets;
		List<ADashboardWidget[]> m_widgetLayout;
		bool m_favorites;

		public List<ADashboardWidget> Widgets => m_widgets;

		public DashboardWidgetLayout(bool a_favorites)
		{
			m_favorites = a_favorites;
			m_widgets = new List<ADashboardWidget>();
			m_widgetLayout = new List<ADashboardWidget[]>() { new ADashboardWidget[5] };
		}

		public void AddWidget(ADashboardWidget a_widget)
		{
			m_widgets.Add(a_widget);
			(m_favorites ? a_widget.m_favPosition : a_widget.m_position).SetSize(a_widget.DefaultW, a_widget.DefaultH);
			var position = FindFittingPosition(a_widget.DefaultW, a_widget.DefaultH);
			(m_favorites ? a_widget.m_favPosition : a_widget.m_position).SetPosition(position.x, position.y);
			InsertWidget(a_widget);
		}

		public (int y, int x) FindFittingPosition(int a_w, int a_h)
		{
			//Moving window containing all indices of valid horizontal positions in row
			List<List<int>> validIndices = new List<List<int>>();

			for (int y = 0; y < m_widgetLayout.Count; y++)
			{
				//Discard rows now beyond scope
				if (validIndices.Count == a_h)
					validIndices.RemoveAt(0);
				validIndices.Add(new List<int>(columns));

				//Find available indices in current row
				int x = 0;
				int lastEmptyindex = -1;
				while (x <= columns - a_w)
				{
					if (m_widgetLayout[y][x] != null)
					{
						lastEmptyindex = -1;
						x += m_widgetLayout[y][x].m_position.W;
						continue;
					}
					if (lastEmptyindex < 0)
						lastEmptyindex = x;
					else if (x - lastEmptyindex >= a_w)
						validIndices[validIndices.Count].Add(x - a_w);
					x++;
				}

				//If we've covered rows equal to widget H, check if any positions are valid
				if (validIndices.Count == a_h)
				{
					foreach (int index in validIndices[0])
					{
						bool valid = true;
						for (int j = 1; j < validIndices.Count; j++)
						{
							if (!validIndices[j].Contains(index))
							{
								valid = false;
								break;
							}
						}
						if (valid)
						{
							return (y-validIndices.Count+1, index);
						}
					}
				}
			}
			//Current slots do not fit widget, find best partial position, sticking out at the bottom
			if(a_h > 1)
			{
				for (int i = 0; i < validIndices.Count; i++)
				{
					foreach (int index in validIndices[i])
					{
						bool valid = true;
						for (int j = i+1; j < validIndices.Count; j++)
						{
							if (!validIndices[j].Contains(index))
							{
								valid = false;
								break;
							}
						}
						if (valid)
						{
							return (m_widgetLayout.Count - validIndices.Count + i, index);
						}
					}
				}
			}
			return (m_widgetLayout.Count, 0);
		}

		void InsertWidget(ADashboardWidget a_widget)
		{
			int posX = m_favorites ? a_widget.m_favPosition.X : a_widget.m_position.X;
			int posY = m_favorites ? a_widget.m_favPosition.Y : a_widget.m_position.Y;
			int w = m_favorites ? a_widget.m_favPosition.W : a_widget.m_position.W;
			int h = m_favorites ? a_widget.m_favPosition.H : a_widget.m_position.H;

			for (int i = m_widgetLayout.Count; i < posY + a_widget.DefaultH; i++)
			{
				m_widgetLayout.Add(new ADashboardWidget[5]);
			}
			for(int y = posY; y < posY + h; y++)
			{
				for (int x = posX; x < posX + w; x++)
				{
					m_widgetLayout[y][x] = a_widget;
				}
			}
		}

		public void DuplicateWidget(ADashboardWidget a_widget)
		{ }

		public void ChangeWidgetSize(ADashboardWidget a_widget, int a_newW, int a_newH)
		{ }

		public void Remove(ADashboardWidget a_widget)
		{
			m_widgets.Remove(a_widget);

			int posX = m_favorites ? a_widget.m_favPosition.X : a_widget.m_position.X;
			int posY = m_favorites ? a_widget.m_favPosition.Y : a_widget.m_position.Y;
			int w = m_favorites ? a_widget.m_favPosition.W : a_widget.m_position.W;
			int h = m_favorites ? a_widget.m_favPosition.H : a_widget.m_position.H;

			for (int y = posY; y < posY + h; y++)
			{
				for (int x = posX; x < posX + w; x++)
				{
					m_widgetLayout[y][x] = null;
				}
			}
		}
	}
}