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
			editionText.text = SessionManager.Instance.MspGlobalData.edition_name;
		}

		public void Activate()
		{
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
		}
	}
}

