
using System.Collections.Generic;
using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public class AddTooltipWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		public List<AddTooltip> addTooltips = new List<AddTooltip>();

		[SerializeField, TextArea]
		public string textOnStart = "";
		[SerializeField, TextArea]
		public string textOnConnected = "";

		[SerializeField, TextArea] public string textOnDisconnected = "";

		private void Start()
		{
			if (addTooltips.Count == 0)
			{
				// auto-fill
				addTooltips.AddRange(gameObject.GetComponents<AddTooltip>());
			}
			if (addTooltips.Count == 0)
			{
				Debug.LogError("Missing component AddTooltip for game object:" + gameObject.name);
				return;
			}

			addTooltips.ForEach(delegate(AddTooltip tooltip)
			{
				tooltip.text = textOnStart;
			});
		}

		public override void NotifyConnection(bool connected)
		{
			if (addTooltips.Count == 0)
			{
				return;
			}

			addTooltips.ForEach(delegate(AddTooltip tooltip)
			{
				tooltip.text = connected ? textOnConnected : textOnDisconnected;
			});
		}
	}
}