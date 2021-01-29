using UnityEngine.Events;

namespace Interface.Notifications
{
	public class NotificationData
	{
		public readonly string identifier;	//Unique identifier for this notification. Can be used to remove notification afterwards.
		public readonly string summary;		//Summary text
		public readonly string description;	//Full descriptive text for the notification.
		public string buttonText = null;	//If not null will populate button with this text.
		public UnityAction onButtonPress = null;	//If buttonText not null will invoke this callback when requested.

		public NotificationData(string identifier, string summary, string description)
		{
			this.identifier = identifier;
			this.summary = summary;
			this.description = description;
		}
	}
}