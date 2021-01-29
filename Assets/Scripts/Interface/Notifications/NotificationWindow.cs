using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Interface.Notifications
{
	public class NotificationWindow : MonoBehaviour
	{
		private Dictionary<string, NotificationEntryElement> notificationsByIdentifier = new Dictionary<string, NotificationEntryElement>();

		[SerializeField]
		private GameObject notificationEntryPrefab = null;

		[SerializeField]
		private RectTransform notificationContainer = null;

		[SerializeField]
		private Animator animator = null;

		[SerializeField]
		private CustomButton headerButton = null;

        [SerializeField]
        private TextMeshProUGUI notificationAmount = null;

        private void Awake()
		{
            gameObject.SetActive(false);
            PlayerNotifications.OnAddNotification += OnAddNewNotification;
			PlayerNotifications.OnRemoveNotification += OnRemoveNotification;

			headerButton.onClick.AddListener(ToggleVisibility);

			SetNotificationElementOpen(false);
		}

		private void OnDestroy()
		{
			PlayerNotifications.OnAddNotification -= OnAddNewNotification;
			PlayerNotifications.OnRemoveNotification -= OnRemoveNotification;
			headerButton.onClick.RemoveListener(ToggleVisibility);
		}

		private void OnAddNewNotification(NotificationData data)
		{
			NotificationEntryElement element;
			if (!notificationsByIdentifier.TryGetValue(data.identifier, out element))
			{
				GameObject notificationObject = Instantiate(notificationEntryPrefab, notificationContainer);
				element = notificationObject.GetComponent<NotificationEntryElement>();
				notificationsByIdentifier.Add(data.identifier, element);
			}

			element.InitializeForData(data);
			SetNotificationElementOpen(true);
            gameObject.SetActive(true);
            notificationAmount.text = notificationsByIdentifier.Count.ToString();

        }

		private void OnRemoveNotification(string identifier)
		{
			NotificationEntryElement entry;
			if (notificationsByIdentifier.TryGetValue(identifier, out entry))
			{
				notificationsByIdentifier.Remove(identifier);
				Destroy(entry.gameObject);

				if (notificationsByIdentifier.Count == 0)
				{
					SetNotificationElementOpen(false);
                    gameObject.SetActive(false);
				}
                else
                    notificationAmount.text = notificationsByIdentifier.Count.ToString();
            }
		}

		private void SetNotificationElementOpen(bool openState)
		{
			animator.SetBool("Open", openState);
		}

		private bool IsNotificationElementOpen()
		{
			return animator.GetBool("Open");
		}

		private void ToggleVisibility()
		{
			SetNotificationElementOpen(!IsNotificationElementOpen());
		}
	}
}
