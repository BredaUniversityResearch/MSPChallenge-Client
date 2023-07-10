using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ColourPalette;

namespace MSP2050.Scripts
{
	public class UIGradient : BaseMeshEffect
	{
		[SerializeField] ColourAsset m_colour1;
		[SerializeField] ColourAsset m_colour2;

		protected UIGradient()
		{ }

		public override void ModifyMesh(Mesh mesh)
		{
			if (!IsActive())
				return;

			mesh.SetColors(new System.Collections.Generic.List<Color> { Color.white, Color.white, Color.black, Color.black });
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (!IsActive() || vh.currentVertCount != 4 || m_colour1 == null || m_colour2 == null)
				return;

			var vert = new UIVertex();

			vh.PopulateUIVertex(ref vert, 0);
			vert.color = m_colour1.GetColour();
			vh.SetUIVertex(vert, 0);

			vh.PopulateUIVertex(ref vert, 1);
			vert.color = m_colour1.GetColour();
			vh.SetUIVertex(vert, 1);

			vh.PopulateUIVertex(ref vert, 2);
			vert.color = m_colour2.GetColour();
			vh.SetUIVertex(vert, 2);

			vh.PopulateUIVertex(ref vert, 3);
			vert.color = m_colour2.GetColour();
			vh.SetUIVertex(vert, 3);
		}
	}
}
