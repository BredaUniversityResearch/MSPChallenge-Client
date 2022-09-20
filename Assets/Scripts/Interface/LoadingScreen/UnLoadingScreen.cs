using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class UnLoadingScreen : MonoBehaviour
	{
		[SerializeField]
		private Text editionText = null;
		public Image mspIcon;

		protected void Start()
		{
			mspIcon.sprite = SessionManager.Instance.MspGlobalData.edition_icon;
			editionText.text = SessionManager.Instance.MspGlobalData.edition_name;
		}

		public void Activate()
		{
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
		}
	}
}

