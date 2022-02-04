
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Networking.WsServerConnectionChangeBehaviour
{
	[RequireComponent(typeof(AddTooltip))]
	public class AddTooltipWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		public List<AddTooltip> addTooltips = new List<AddTooltip>();

		[SerializeField, TextArea]
		public string textOnStart = "";
		[SerializeField, TextArea]
		public string textOnConnected = "";

		[SerializeField, TextArea] public string textOnDisconnected = "";

		protected override void OnStart()
		{
			if (addTooltips.Count == 0)
			{
				// auto-fill
				addTooltips.AddRange(gameObject.GetComponents<AddTooltip>());
			}
			if (addTooltips.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component AddTooltip for game object:" + gameObject.name);
				return;
			}
			addTooltips.ForEach(delegate(AddTooltip tooltip)
			{
				tooltip.text = textOnStart;
			});
		}

		protected override void OnNotifyConnection(bool connected)
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