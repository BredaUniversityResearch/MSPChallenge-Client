using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class UnLoadingScreen : MonoBehaviour
	{
		[SerializeField]
		private Text editionText = null;
		public DynamicLogo mspIcon;

		protected void Start()
		{
			mspIcon.SetContent(SessionManager.Instance.MspGlobalData.edition_colour, SessionManager.Instance.MspGlobalData.edition_letter);
			editionText.text = SessionManager.Instance.MspGlobalData.edition_name;
		}

		public void Activate()
		{
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
		}
	}
}

