using System.Collections.Generic;
using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public class CustomToggleColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		public List<CustomToggleColorSet> customToggleColorSets = new List<CustomToggleColorSet>();
		
		[SerializeField] public bool useHighlightColorOnStart = true;
		[SerializeField] public bool useHighlightColorOnConnected = true;
		[SerializeField] public bool useHighlightColorOnDisconnected = false;

		private void Start()
		{
			if (customToggleColorSets.Count == 0)
			{
				// auto-fill
				customToggleColorSets.AddRange(gameObject.GetComponents<CustomToggleColorSet>());
			}
			if (customToggleColorSets.Count == 0)
			{
				Debug.LogError("Missing component CustomToggleColorSet for game object:" + gameObject.name);
				return;
			}
			
			customToggleColorSets.ForEach(delegate(CustomToggleColorSet set)
			{
				set.useHighlightColor = useHighlightColorOnStart;
			});
		}

		public override void NotifyConnection(bool connected)
		{
			if (customToggleColorSets.Count == 0)
			{
				return;
			}

			customToggleColorSets.ForEach(delegate(CustomToggleColorSet set)
			{
				set.useHighlightColor = connected ? useHighlightColorOnConnected : useHighlightColorOnDisconnected;
			});
		}
	}
}