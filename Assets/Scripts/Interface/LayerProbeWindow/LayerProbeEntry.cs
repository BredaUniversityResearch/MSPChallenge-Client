using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LayerProbeEntry : MonoBehaviour {

		[SerializeField]
		TextMeshProUGUI layerNameText = null;
		[SerializeField]
		TextMeshProUGUI geomNameText = null;
		[SerializeField]
		Image layerIcon = null;
		public CustomButton barButton;

		public void SetToSubEntity(SubEntity sub)
		{
			layerNameText.text = sub.Entity.Layer.ShortName;
			string geomName = sub.Entity.name;
			geomNameText.text = string.IsNullOrEmpty(geomName) ? "Unnamed" : geomName;
			layerIcon.sprite = LayerInterface.GetIconStatic(sub.Entity.Layer.SubCategory);		
		}
	}
}
