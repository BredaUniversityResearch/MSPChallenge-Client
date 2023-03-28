using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class NotificationEntryElement: MonoBehaviour
	{
		private string identifier = string.Empty;

		[SerializeField] TextMeshProUGUI fullDescriptionText = null;
		[SerializeField] TextMeshProUGUI titleText = null;
		[SerializeField] CustomButton dismissButton = null;
		[SerializeField] CustomButton notificationActionButton = null;
		//[SerializeField] TextMeshProUGUI notificationActionButtonText = null;

		private void Start()
		{
			if (dismissButton != null)
			{
				dismissButton.onClick.AddListener(OnDismissClicked);
			}
		}

		private void OnDismissClicked()
		{
			PlayerNotifications.RemoveNotification(identifier);
		}

		public void InitializeForData(NotificationData data)
		{
			identifier = data.identifier;
			fullDescriptionText.text = data.description;
			titleText.text = data.summary;

			bool buttonActive = !string.IsNullOrEmpty(data.buttonText) && data.onButtonPress != null;
			notificationActionButton.gameObject.SetActive(buttonActive);
			if (buttonActive)
			{
				//notificationActionButtonText.text = data.buttonText;
				notificationActionButton.onClick.RemoveAllListeners();
				notificationActionButton.onClick.AddListener(data.onButtonPress);
			}
		}
	}
}
