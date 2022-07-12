using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HEBGraph
{
	public class UIRadialDrawer : MaskableGraphic
	{
		[SerializeField] float m_outerPointInterval;
		[SerializeField] float m_innerPointInterval;
		[SerializeField] float m_innerRadius;
		[SerializeField] float m_outerRadius;

		[SerializeField] List<(float start, float end)> m_gaps;

		public void SetGaps(List<(float start, float end)> a_gaps)
		{
			m_gaps = a_gaps;
			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper a_vh)
		{
			if (m_gaps == null || m_gaps.Count == 0)
				return;

			a_vh.Clear();

			List<UIVertex> verts = new List<UIVertex>(100);
			List<int> indices = new List<int>(300);

			//Add initial inner and outer vertex
			verts.Add(GetInnerVertex(0f));
			verts.Add(GetOuterVertex(0f));

			int nextGapIndex = 0;
			int lastInnerPointIndex = 0;
			float nextGapTheta = m_gaps[0].start;
			float nextInnerTheta = m_innerPointInterval;
			float nextOuterTheta = m_outerPointInterval;
			float theta = 0f;
			float end = Mathf.PI * 2f;

			while (true)
			{
				if (nextGapTheta <= nextInnerTheta && nextGapTheta <= nextOuterTheta)
				{
					//Create gap
					theta = nextGapTheta;
					verts.Add(GetInnerVertex(nextGapTheta));
					verts.Add(GetOuterVertex(nextGapTheta));

					//Close current side
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

					theta = m_gaps[nextGapIndex].end;
					if (theta >= end)
					{
						//Close
						break;
					}

					//Add inner and outer vertices after gap
					verts.Add(GetInnerVertex(theta));
					verts.Add(GetOuterVertex(theta));
					lastInnerPointIndex = verts.Count - 2;

					//Update data for next gap
					nextGapIndex++;
					if (nextGapIndex == m_gaps.Count)
						nextGapTheta = Mathf.Infinity;
					else
						nextGapTheta = m_gaps[nextGapIndex].start;

					//Increase next inner and outer theta to be from the gap end
					nextInnerTheta = theta + m_innerPointInterval;
					nextOuterTheta = theta + m_outerPointInterval;
				}
				else if (nextInnerTheta <= nextOuterTheta)
				{
					//Create inner point
					theta = nextInnerTheta;
					if (theta >= end)
					{
						//Close
						indices.Add(lastInnerPointIndex);
						indices.Add(verts.Count - 1);
						indices.Add(0);

						indices.Add(verts.Count - 1);
						indices.Add(1);
						indices.Add(0);
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
						//Close
						if (lastInnerPointIndex == verts.Count - 1)
						{
							//Last was an inner
							indices.Add(verts.Count - 2);
							indices.Add(1);
							indices.Add(lastInnerPointIndex);

							indices.Add(lastInnerPointIndex);
							indices.Add(1);
							indices.Add(0);
						}
						else
						{
							//Last was an outer
							indices.Add(lastInnerPointIndex);
							indices.Add(verts.Count - 1);
							indices.Add(0);

							indices.Add(verts.Count - 1);
							indices.Add(1);
							indices.Add(0);
						}
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

			a_vh.AddUIVertexStream(verts, indices);
		}

		private UIVertex GetInnerVertex(float a_theta)
		{
			UIVertex vert = UIVertex.simpleVert;
			vert.position = new Vector3(Mathf.Sin(a_theta), Mathf.Cos(a_theta)) * m_innerRadius;
			vert.color = color;
			return vert;
		}

		private UIVertex GetOuterVertex(float a_theta)
		{
			UIVertex vert = UIVertex.simpleVert;
			vert.position = new Vector3(Mathf.Sin(a_theta), Mathf.Cos(a_theta)) * m_outerRadius;
			vert.color = color;
			return vert;
		}
	}
}
