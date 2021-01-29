using System;

namespace SoftwareRasterizerLib
{
	public class RasterizerSurface
	{
		public readonly int Width;
		public readonly int Height;

		public int[] Values
		{
			get;
			private set;
		}

		public RasterizerSurface(int a_Width, int a_Height)
		{
			Width = a_Width;
			Height = a_Height;
			Values = new int[a_Width * a_Height];
		}


		public int GetValueAt(int a_X, int a_Y)
		{
			if (a_X < 0 || a_X >= Width)
			{
				throw new ArgumentOutOfRangeException("a_X");
			}

			if (a_Y < 0 || a_Y > Height)
			{
				throw new ArgumentOutOfRangeException("a_Y");
			}

			return Values[a_X + (a_Y * Width)];
		}

		public bool TrySetValueAt(int a_X, int a_Y, int a_Value)
		{
			if (a_X >= 0 && a_X < Width && a_Y >= 0 && a_Y < Height)
			{
				Values[a_X + (a_Y * Width)] = a_Value;
				return true;
			}

			return false;
		}
	}
}
