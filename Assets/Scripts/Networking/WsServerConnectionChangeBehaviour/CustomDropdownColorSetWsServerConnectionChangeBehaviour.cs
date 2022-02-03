using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public class CustomDropdownColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		public List<CustomDropdownColorSet> customDropdownColorSets = new List<CustomDropdownColorSet>();
		
		[SerializeField] public bool useHighlightColorOnStart = true;
		[SerializeField] public bool useHighlightColorOnConnected = true;
		[SerializeField] public bool useHighlightColorOnDisconnected = false;
		
		private void Start()
		{
			if (customDropdownColorSets.Count == 0)
			{
				// auto-fill
				customDropdownColorSets.AddRange(gameObject.GetComponents<CustomDropdownColorSet>());
			}
			if (customDropdownColorSets.Count == 0)
			{
				Debug.LogError("Missing component CustomDropdownColorSet for game object:" + gameObject.name);
				return;
			}
			
			customDropdownColorSets.ForEach(delegate(CustomDropdownColorSet set)
			{
				set.useHighlightColor = useHighlightColorOnStart;
			});
		}

		public override void NotifyConnection(bool connected)
		{
			if (customDropdownColorSets.Count == 0)
			{
				return;
			}
			
			customDropdownColorSets.ForEach(delegate(CustomDropdownColorSet set)
			{
				set.useHighlightColor = connected ? useHighlightColorOnConnected : useHighlightColorOnDisconnected;
			});
		}
	}
}