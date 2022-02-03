using System.Collections.Generic;
using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public class CustomButtonColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		public List<CustomButtonColorSet> customButtonColorSets = new List<CustomButtonColorSet>();
		
		[SerializeField] public bool useHighlightColorOnStart = true;
		[SerializeField] public bool useHighlightColorOnConnected = true;
		[SerializeField] public bool useHighlightColorOnDisconnected = false;
		
		private void Start()
		{
			if (customButtonColorSets.Count == 0)
			{
				// auto-fill
				customButtonColorSets.AddRange(gameObject.GetComponents<CustomButtonColorSet>());
			}
			if (customButtonColorSets.Count == 0)
			{
				Debug.LogError("Missing component CustomToggleColorSet for game object:" + gameObject.name);
				return;
			}
			
			customButtonColorSets.ForEach(delegate(CustomButtonColorSet set)
			{
				set.useHighlightColor = useHighlightColorOnStart;
			});
		}

		public override void NotifyConnection(bool connected)
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