using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEBGraph
{
	public class UIRadialSegmentDrawer : MaskableGraphic
	{
		[SerializeField] float m_outerPointInterval;
		[SerializeField] float m_innerPointInterval;
		[SerializeField] float m_innerRadius;
		[SerializeField] float m_outerRadius;
		[SerializeField] float m_thetaRange;

		public void SetRange(float a_range)
		{
			m_thetaRange = a_range;
			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper a_vh)
		{
			if (m_thetaRange <= 0f)
				return;

			a_vh.Clear();

			List<UIVertex> verts = new List<UIVertex>(100);
			List<int> indices = new List<int>(300);

			float theta = Mathf.PI / 2f - m_thetaRange / 2f;
			float end = Mathf.PI / 2f + m_thetaRange / 2f;
			int lastInnerPointIndex = 0;
			float nextInnerTheta = theta + m_innerPointInterval;
			float nextOuterTheta = theta + m_outerPointInterval;

			//Add initial inner and outer vertex
			verts.Add(GetInnerVertex(theta));
			verts.Add(GetOuterVertex(theta));

			while (true)
			{
				if (nextInnerTheta <= nextOuterTheta)
				{
					//Create inner point
					theta = nextInnerTheta;
					if (theta >= end)
					{
						break; 
					}

					nextInnerTheta += m_innerPointInterval;
					verts.Add(GetInnerVertex(theta));
					indices.Add(lastInnerPointIndex);
					indices.Add(verts.Count - 1);
					indices.Add(verts.Count - 2);
					lastInnerPointIndex = verts.Count - 1;
				}
				else
				{
					//Create outer point
					theta = nextOuterTheta;
					if (theta >= end)
					{
						break; 
					}

					nextOuterTheta += m_outerPointInterval;
					verts.Add(GetOuterVertex(theta));

					if (lastInnerPointIndex == verts.Count - 2)
					{
						//Previous point was inner, so we need to go further back for the last outer
						indices.Add(lastInnerPointIndex);
						indices.Add(verts.Count - 1);
						indices.Add(verts.Count - 3);

					}
					else
					{
						indices.Add(lastInnerPointIndex);
						indices.Add(verts.Count - 1);
						indices.Add(verts.Count - 2);
					}
				}
			}

			//Add closing segments
			verts.Add(GetInnerVertex(end));
			verts.Add(GetOuterVertex(end));
			if (lastInnerPointIndex == verts.Count - 3)
			{
				//Last vertex was an inner
				indices.Add(verts.Count - 4);
				indices.Add(verts.Count - 1);
				indices.Add(lastInnerPointIndex);

				indices.Add(lastInnerPointIndex);
				indices.Add(verts.Count - 1);
				indices.Add(verts.Count - 2);
			}
			else
			{
				//Last vertex was an outer
				indices.Add(lastInnerPointIndex);
				indices.Add(verts.Count - 3);
				indices.Add(verts.Count - 2);

				indices.Add(verts.Count - 3);
				indices.Add(verts.Count - 1);
				indices.Add(verts.Count - 2);
			}


			a_vh.AddUIVertexStream(verts, indices);
		}

		private UIVertex GetInnerVertex(float a_theta)
		{
			UIVertex vert = UIVertex.simpleVert;
			vert.position = new Vector3(Mathf.Sin(a_theta) - 1f, Mathf.Cos(a_theta)) * m_innerRadius;
			vert.color = color;
			return vert;
		}

		private UIVertex GetOuterVertex(float a_theta)
		{
			UIVertex vert = UIVertex.simpleVert;
			vert.position = new Vector3(Mathf.Sin(a_theta) * m_outerRadius - m_innerRadius, Mathf.Cos(a_theta) * m_outerRadius);
			vert.color = color;
			return vert;
		}
	}
}
