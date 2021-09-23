using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CohenSutherland
{
	const int INSIDE = 0; // 0000
	const int LEFT = 1;   // 0001
	const int RIGHT = 2;  // 0010
	const int BOTTOM = 4; // 0100
	const int TOP = 8;    // 1000

	static int ComputeOutCode(Vector2 pos, Vector2 min, Vector2 max)
	{
		int code;

		code = INSIDE;

		if (pos.x < min.x)
			code |= LEFT;
		else if (pos.x > max.x)
			code |= RIGHT;
		if (pos.y < min.y)
			code |= BOTTOM;
		else if (pos.y > max.y)
			code |= TOP;

		return code;
	}

	public static bool Calculate(Vector2 lineBegin, Vector2 lineEnd, Vector2 viewportMin, Vector2 viewportMax)
	{
		int outcode0 = ComputeOutCode(lineBegin, viewportMin, viewportMax);
		int outcode1 = ComputeOutCode(lineEnd, viewportMin, viewportMax);

		while (true)
		{
			if ((outcode0 | outcode1) == INSIDE)
			{
				return true;
			}
			else if ((outcode0 & outcode1) != INSIDE)
			{
				return false;
			}

			float x, y;
			int outcodeOut = outcode1 > outcode0 ? outcode1 : outcode0;
			if ((outcodeOut & TOP) != 0)
			{
				x = lineBegin.x + (lineEnd.x - lineBegin.x) * (viewportMax.y - lineBegin.y) / (lineEnd.y - lineBegin.y);
				y = viewportMax.y;
			}
			else if ((outcodeOut & BOTTOM) != 0)
			{
				x = lineBegin.x + (lineEnd.x - lineBegin.x) * (viewportMin.y - lineBegin.y) / (lineEnd.y - lineBegin.y);
				y = viewportMin.y;
			}
			else if ((outcodeOut & RIGHT) != 0)
			{
				y = lineBegin.y + (lineEnd.y - lineBegin.y) * (viewportMax.x - lineBegin.x) / (lineEnd.x - lineBegin.x);
				x = viewportMax.x;
			}
			else if ((outcodeOut & LEFT) != 0)
			{
				y = lineBegin.y + (lineEnd.y - lineBegin.y) * (viewportMin.x - lineBegin.x) / (lineEnd.x - lineBegin.x);
				x = viewportMin.x;
			}
			else
			{
				throw new System.Exception(); // Unable to compute
			}

			if (outcodeOut == outcode0)
			{
				lineBegin.x = x;
				lineBegin.y = y;
				outcode0 = ComputeOutCode(lineBegin, viewportMin, viewportMax);
			}
			else
			{
				lineEnd.x = x;
				lineEnd.y = y;
				outcode1 = ComputeOutCode(lineEnd, viewportMin, viewportMax);
			}
		}
	}
}
