using System;
using System.Collections.Generic;
using UnityEngine;

namespace HEBGraph
{
	public class BezierDrawer
	{
		private float[] m_factorialLookup;

		public BezierDrawer()
		{
			// fill untill n=16. The rest is too high to represent
			float[] a = new float[17];
			a[0] = 1f;
			a[1] = 1f;
			a[2] = 2f;
			a[3] = 6f;
			a[4] = 24f;
			a[5] = 120f;
			a[6] = 720f;
			a[7] = 5040f;
			a[8] = 40320f;
			a[9] = 362880f;
			a[10] = 3628800f;
			a[11] = 39916800f;
			a[12] = 479001600f;
			a[13] = 6227020800f;
			a[14] = 87178291200f;
			a[15] = 1307674368000f;
			a[16] = 20922789888000f;
			m_factorialLookup = a;
		}

		private float Ni(int n, int i)
		{
			float ni;
			float a1 = m_factorialLookup[n];
			float a2 = m_factorialLookup[i];
			float a3 = m_factorialLookup[n - i];
			ni = a1 / (a2 * a3);
			return ni;
		}

		// Calculate Bernstein basis
		private float Bernstein(int n, int i, float t)
		{
			float basis;
			float ti; /* t^i */
			float tni; /* (1 - t)^i */

			/* Prevent problems with pow */

			if (t == 0.0f && i == 0)
				ti = 1.0f;
			else
				ti = Mathf.Pow(t, i);

			if (n == i && t == 1.0f)
				tni = 1.0f;
			else
				tni = Mathf.Pow((1 - t), (n - i));

			//Bernstein basis
			basis = Ni(n, i) * ti * tni;
			return basis;
		}

		public float[] Bezier2D(List<Vector2> a_controlPoints, int a_lineSections)
		{
			float[] splitResult = new float[a_lineSections * 2];

			int resultIndex = 0;
			float step = (float)1.0 / (a_lineSections - 1);
			float t = 0;

			//Step through target number of points
			for (int section = 0; section != a_lineSections; section++)
			{
				if ((1.0f - t) < 5e-6f)
					t = 1.0f;

				//Add influence of all control points to current point
				for (int i = 0; i != a_controlPoints.Count; i++)
				{
					float basis1 = Bernstein(a_controlPoints.Count - 1, i, t);
					splitResult[resultIndex] += basis1 * a_controlPoints[i].x;
					splitResult[resultIndex + 1] += basis1 * a_controlPoints[i].y;
				}

				resultIndex += 2;
				t += step;
			}

			return splitResult;
		}
	}
}

