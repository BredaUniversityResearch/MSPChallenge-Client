using ColourPalette;
using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PlanDetailsLayerEntry : MonoBehaviour
	{
		[SerializeField]
		TextMeshProUGUI layerName, addedGeometry, alteredGeometry, removedGeometry;

		[SerializeField]
		ColourAsset normalColour, zeroColour;

		public void SetLayer(PlanLayer layer)
		{
			layerName.text = layer.BaseLayer.ShortName;

			int value = layer.GetAddedGeometryCount();
			addedGeometry.text = value.ToString();
			addedGeometry.color = value == 0 ? zeroColour.GetColour() : normalColour.GetColour();

			value = layer.GetAlteredGeometryCount();
			alteredGeometry.text = value.ToString();
			alteredGeometry.color = value == 0 ? zeroColour.GetColour() : normalColour.GetColour();

			value = layer.RemovedGeometry.Count;
			removedGeometry.text = value.ToString();
			removedGeometry.color = value == 0 ? zeroColour.GetColour() : normalColour.GetColour();
		}
	}
}

