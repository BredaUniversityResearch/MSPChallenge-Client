using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DashboardWidgetLayout
	{
		List<ADashboardWidget> m_widgets;
		List<ADashboardWidget[]> m_widgetLayout;
		bool m_favorites;
		bool m_visible;
		int m_columns = 5;

		public List<ADashboardWidget> Widgets => m_widgets;

		public int Rows => m_widgetLayout.Count;
		public bool Visible
		{
			get { return m_visible; }
			set 
			{ 
				m_visible = value;
				if(m_visible)
				{
					foreach (ADashboardWidget widget in m_widgets)
					{
						widget.Show();
						widget.Reposition(m_favorites);
					}
				}
				else
				{
					foreach (ADashboardWidget widget in m_widgets)
					{
						widget.Hide();
					}
				}
			}
		}


		public DashboardWidgetLayout(bool a_favorites, int a_columns)
		{
			m_columns = a_columns;
			m_favorites = a_favorites;
			m_widgets = new List<ADashboardWidget>();
			m_widgetLayout = new List<ADashboardWidget[]>() { new ADashboardWidget[m_columns] };
		}

		public void AddWidget(ADashboardWidget a_widget)
		{
			if(m_favorites)
			{ 
				a_widget.m_favPosition = new DashboardWidgetPosition();
				a_widget.m_favPosition.SetSize(a_widget.m_position.W, a_widget.m_position.H);
				a_widget.m_favPosition.SetPosition(FindFittingPosition(a_widget.m_favPosition.W, a_widget.m_favPosition.H));
			}
			else
			{
				a_widget.m_position.SetPosition(FindFittingPosition(a_widget.m_position.W, a_widget.m_position.H));			
			}
			m_widgets.Add(a_widget);
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
					if (x - lastEmptyindex + 1 >= a_w)
						validIndices[validIndices.Count-1].Add(x - a_w + 1);
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
			bool rowsInserted = false;
			for (int i = m_widgetLayout.Count; i < layout.Y + layout.H; i++)
			{
				m_widgetLayout.Add(new ADashboardWidget[m_columns]);
				rowsInserted = true;
			}
			if(rowsInserted)
				DashboardManager.Instance.OnNumberRowsChanged(m_widgetLayout.Count);

			for (int y = layout.Y; y < layout.Y + layout.H; y++)
			{
				for (int x = layout.X; x < layout.X + layout.W; x++)
				{
					m_widgetLayout[y][x] = a_widget;
				}
			}
			if(m_visible)
				a_widget.Reposition(m_favorites);

		}

		public void ChangeNumberColumns(int a_columns)
		{
			m_columns = a_columns; 
			//TODO: restructure content
		}

		public void DeleteAndClear()
		{
			foreach (ADashboardWidget widget in m_widgets)
			{
				GameObject.Destroy(widget.gameObject);
			}
			m_widgets = new List<ADashboardWidget>();
			m_widgetLayout = new List<ADashboardWidget[]>() { new ADashboardWidget[m_columns] };
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

		public void MoveWidget(ADashboardWidget a_widget, int a_newX, int a_newY, int a_newW, int a_newH)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			Remove(a_widget, true);
			layout.SetPosition(a_newX, a_newY);
			layout.SetSize(a_newW, a_newH);
			InsertWidget(a_widget);
		}

		public void MoveWidgetAboveRow(ADashboardWidget a_widget, int a_newX, int a_newY, int a_newW)
		{
			DashboardWidgetPosition layout = m_favorites ? a_widget.m_favPosition : a_widget.m_position;
			Remove(a_widget, true);
			layout.SetPosition(a_newX, a_newY);
			layout.SetSize(a_newW, layout.H);
			InsertRows(a_newY, layout.H);	
			InsertWidget(a_widget);

			for(int y = a_newY + 1; y < m_widgetLayout.Count; y++)
			{
				for(int x = 0; x < m_columns; x++)
				{
					if (m_widgetLayout[y][x] == null)
						continue;
					DashboardWidgetPosition newLayout = m_favorites ? m_widgetLayout[y][x].m_favPosition : m_widgetLayout[y][x].m_position;
					if (newLayout.Y < y)
					{
						newLayout.SetPosition(newLayout.X, newLayout.Y + layout.H);
						m_widgetLayout[y][x].Reposition(m_favorites);
					}
				}
			}
			DashboardManager.Instance.OnNumberRowsChanged(m_widgetLayout.Count);
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

		public bool WidgetFitsAt(ADashboardWidget a_widget, int a_x, int a_y, int a_currentW, int a_currentH, out int a_outW, out int a_outH)
		{
			a_outW = a_widget.MinW;
			a_outH = a_widget.MinH;
			for (int y = a_y; y < a_y + a_outH && y < m_widgetLayout.Count; y++)
			{
				for (int x = a_x; x < a_x + a_outW && x < m_columns; x++)
				{
					if (m_widgetLayout[y][x] != null && m_widgetLayout[y][x] != a_widget)
						return false;
				}
			}
			//Min size possible, expand from there
			bool xDone = false;
			bool yDone = false;
			while(!xDone || !yDone)
			{
				if(!xDone)
				{
					if (a_outW == a_currentW || a_outW + a_x >= m_columns)
					{ 
						xDone = true;
					}
					else
					{
						int newX = a_x + a_outW;
						for (int y = a_y; y < a_y + a_outH && y < m_widgetLayout.Count; y++)
						{
							if (m_widgetLayout[y][newX] != null && m_widgetLayout[y][newX] != a_widget)
							{
								xDone = true;
								break;
							}
						}
						if (!xDone)
							a_outW++;
					}
					
				}
				if (!yDone)
				{
					if (a_outH == a_currentH)
					{
						yDone = true;
					}
					else if (a_y + a_outH >= m_widgetLayout.Count)
					{
						a_outH = a_currentH;
						yDone = true;
					}
					else
					{
						int newY = a_y + a_outH;
						for (int x = a_x; x < a_x + a_outW; x++)
						{
							if (m_widgetLayout[newY][x] != null && m_widgetLayout[newY][x] != a_widget)
							{
								yDone = true;
								break;
							}
						}
						if (!yDone)
							a_outH++;
					}
				}
			}
			return true;
		}

		public bool WidgetInsertRowPossible(ADashboardWidget a_widget, int a_row, int a_x, int a_currentW, out int a_maxW)
		{
            if (m_columns - a_x < a_widget.MinW)
            {
				a_maxW = 0;
				return false;

			}
            if (a_row == 0)
			{
				a_maxW = Mathf.Min(a_currentW, m_columns - a_x);
				return true;
			}
			for (int x = a_x; x < a_x + a_currentW && x < m_columns; x++)
			{
				ADashboardWidget newWidget = m_widgetLayout[a_row][x];
				if (newWidget != null && newWidget != a_widget)
				{
					if((m_favorites ? newWidget.m_favPosition : newWidget.m_position).Y < a_row)
					{
						a_maxW = x - a_x;
						return a_maxW >= a_widget.MinW;
					}
				}
			}
			a_maxW = a_currentW;
			return true;
		}

		void InsertRows(int a_row, int a_amount)
		{
			if (a_row > 0)
			{
				List<ADashboardWidget[]> newRows = new List<ADashboardWidget[]>(a_amount);
				for (int i = 0; i < a_amount; i++)
					newRows.Add(new ADashboardWidget[m_columns]);

				//Check if this would intersect widgets
				for (int x = 0; x < m_columns; x++)
				{
					ADashboardWidget widget = m_widgetLayout[a_row][x];
					if (widget != null)
					{
						DashboardWidgetPosition layout = m_favorites ? widget.m_favPosition : widget.m_position;
						if (layout.Y < a_row)
						{
							//Splits widget, remove from old rows and add to new
							int rowsBelow = layout.Y + layout.H - a_row;
							for (int y = 0; y <= rowsBelow; y++)
							{
								m_widgetLayout[layout.Y + layout.H - 1 - y] = null;
								if(y < a_amount)
									newRows[y][x] = widget;
							}
						}
					}
				}

				m_widgetLayout.InsertRange(a_row, newRows);
			}
			else
			{
				for (int i = 0; i < a_amount; i++)
					m_widgetLayout.Insert(0,new ADashboardWidget[m_columns]);
			}
		}
	}
}