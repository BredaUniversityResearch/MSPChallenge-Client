using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DashboardWidgetLayout
	{
		List<ADashboardWidget> m_widgets;
		List<ADashboardWidget[]> m_widgetLayout;
		bool m_favorites;
		int m_columns = 5;

		public List<ADashboardWidget> Widgets => m_widgets;

		public DashboardWidgetLayout(bool a_favorites, int a_columns)
		{
			m_columns = a_columns;
			m_favorites = a_favorites;
			m_widgets = new List<ADashboardWidget>();
			m_widgetLayout = new List<ADashboardWidget[]>() { new ADashboardWidget[5] };
		}

		public void AddWidget(ADashboardWidget a_widget)
		{
			m_widgets.Add(a_widget);
			a_widget.m_position = new DashboardWidgetPosition();
			a_widget.m_favPosition = new DashboardWidgetPosition();
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
				validIndices.Add(new List<int>(m_columns));

				//Find available indices in current row
				int x = 0;
				int lastEmptyindex = -1;
				while (x <= m_columns - a_w)
				{
					if (m_widgetLayout[y][x] != null)
					{
						lastEmptyindex = -1;
						x += m_widgetLayout[y][x].m_position.W;
						continue;
					}
					if (lastEmptyindex < 0)
						lastEmptyindex = x;
					if (lastEmptyindex >= 0 && x - lastEmptyindex >= a_w)
						validIndices[validIndices.Count-1].Add(x - a_w);
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

		public void InsertWidget(ADashboardWidget a_widget)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			for (int i = m_widgetLayout.Count; i < layout.Y + layout.H; i++)
			{
				m_widgetLayout.Add(new ADashboardWidget[5]);
			}

			for (int y = layout.Y; y < layout.Y + layout.H; y++)
			{
				for (int x = layout.X; x < layout.X + layout.W; x++)
				{
					m_widgetLayout[y][x] = a_widget;
				}
			}
			a_widget.Reposition();
		}

		public void ChangeNumberColumns(int a_columns)
		{
			m_columns = a_columns; 
			//TODO: restructure content
		}

		public void ChangeWidgetSize(ADashboardWidget a_widget, int a_newW, int a_newH)
		{
			//TODO
		}

		public void Remove(ADashboardWidget a_widget, bool a_layoutOnly = false)
		{
			if(!a_layoutOnly)
				m_widgets.Remove(a_widget);
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;

			for (int y = layout.Y; y < layout.Y + layout.H; y++)
			{
				for (int x = layout.X; x < layout.X + layout.W; x++)
				{
					m_widgetLayout[y][x] = null;
				}
			}
		}

		public void MoveWidget(ADashboardWidget a_widget, int a_newX, int a_newY)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			Remove(a_widget, true);
			layout.SetPosition(a_newX, a_newY);
			InsertWidget(a_widget);
		}

		public void MoveWidgetAboveRow(ADashboardWidget a_widget, int a_newX, int a_newY)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			Remove(a_widget, true);
			layout.SetPosition(a_newX, a_newY);
			for(int i = 0; i < layout.H; i++)
			{
				m_widgetLayout.Insert(a_newY, new ADashboardWidget[5]);
			}
			InsertWidget(a_widget);
		}

		public bool WidgetFitsAt(ADashboardWidget a_widget, (int x, int y) a_newPos)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			return WidgetFitsAt(a_widget, a_newPos.x, a_newPos.y, layout.W, layout.H);
		}

		public bool WidgetFitsAt(ADashboardWidget a_widget, int a_w, int a_h)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			return WidgetFitsAt(a_widget, layout.X, layout.Y, a_w, a_h);
		}

		public bool WidgetFitsAt(ADashboardWidget a_widget, int a_x, int a_y, int a_w, int a_h)
		{
			if (m_columns < a_x + a_w)
				return false;
			for (int y = a_y; y < a_y + a_h && y < m_widgetLayout.Count; y++)
			{
				for (int x = a_x; x < a_x + a_w; x++)
				{
					if (m_widgetLayout[y][x] != null && m_widgetLayout[y][x] != a_widget)
						return false;
				}
			}
			return true;
		}
	}
}