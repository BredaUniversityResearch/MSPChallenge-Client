using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Interface.Notifications
{
	public class NotificationEntryElement: MonoBehaviour
	{
		private string identifier = string.Empty;

		[SerializeField]
		private TextMeshProUGUI summaryText = null;

		[SerializeField]
		private TextMeshProUGUI fullDescriptionText = null;

		[SerializeField]
		private CustomButton dismissButton = null;

		[SerializeField]
		private CustomButton notificationActionButton = null;

		[SerializeField]
		private TextMeshProUGUI notificationActionButtonText = null;

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
			summaryText.text = data.summary;
			fullDescriptionText.text = data.description;

			bool buttonActive = !string.IsNullOrEmpty(data.buttonText) && data.onButtonPress != null;
			notificationActionButton.gameObject.SetActive(buttonActive);
			if (buttonActive)
			{
				notificationActionButtonText.text = data.buttonText;
				notificationActionButton.onClick.RemoveAllListeners();
				notificationActionButton.onClick.AddListener(data.onButtonPress);
			}
		}
	}
}
