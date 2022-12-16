using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class UnLoadingScreen : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI editionText = null;
		public Image mspIcon;

		protected void Start()
		{
			RegionInfo region = InterfaceCanvas.Instance.regionSettings.GetRegionInfo(SessionManager.Instance.MspGlobalData.region);
			mspIcon.sprite = region.sprite;
			editionText.text = region.editionPostFix;
		}

		public void Activate()
		{
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
		}
	}
}

