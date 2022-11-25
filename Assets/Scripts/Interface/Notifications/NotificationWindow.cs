using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class NotificationWindow : MonoBehaviour
	{
		private Dictionary<string, NotificationEntryElement> notificationsByIdentifier = new Dictionary<string, NotificationEntryElement>();

		[SerializeField]
		private GameObject notificationEntryPrefab = null;

		[SerializeField]
		private RectTransform notificationContainer = null;

        private void Awake()
		{
            gameObject.SetActive(false);
            PlayerNotifications.OnAddNotification += OnAddNewNotification;
			PlayerNotifications.OnRemoveNotification += OnRemoveNotification;
		}

		private void OnDestroy()
		{
			PlayerNotifications.OnAddNotification -= OnAddNewNotification;
			PlayerNotifications.OnRemoveNotification -= OnRemoveNotification;
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
            gameObject.SetActive(true);

        }

		private void OnRemoveNotification(string identifier)
		{
			NotificationEntryElement entry;
			if (notificationsByIdentifier.TryGetValue(identifier, out entry))
			{
				notificationsByIdentifier.Remove(identifier);
				Destroy(entry.gameObject);
            }
		}
	}
}
