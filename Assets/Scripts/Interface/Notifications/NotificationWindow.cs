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

        [SerializeField]
        private TextMeshProUGUI notificationAmount = null;

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
                    gameObject.SetActive(false);
				}
                else
                    notificationAmount.text = notificationsByIdentifier.Count.ToString();
            }
		}
	}
}
