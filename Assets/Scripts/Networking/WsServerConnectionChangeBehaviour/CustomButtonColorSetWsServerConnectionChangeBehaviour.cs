using System.Collections.Generic;
using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	[RequireComponent(typeof(CustomButtonColorSet))]
	public class CustomButtonColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		public List<CustomButtonColorSet> customButtonColorSets = new List<CustomButtonColorSet>();
		
		[SerializeField] public bool useHighlightColorOnStart = true;
		[SerializeField] public bool useHighlightColorOnConnected = true;
		[SerializeField] public bool useHighlightColorOnDisconnected = false;
		
		protected override void OnStart()
		{
			if (customButtonColorSets.Count == 0)
			{
				// auto-fill
				customButtonColorSets.AddRange(gameObject.GetComponents<CustomButtonColorSet>());
			}
			if (customButtonColorSets.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component CustomToggleColorSet for game object:" + gameObject.name);
				return;
			}
			
			customButtonColorSets.ForEach(delegate(CustomButtonColorSet set)
			{
				set.useHighlightColor = useHighlightColorOnStart;
			});
		}

		protected override void OnNotifyConnection(bool connected)
		{
			if (customButtonColorSets.Count == 0)
			{
				return;
			}
			
			customButtonColorSets.ForEach(delegate(CustomButtonColorSet set)
			{
				set.useHighlightColor = connected ? useHighlightColorOnConnected : useHighlightColorOnDisconnected;
			});
		}
	}
}