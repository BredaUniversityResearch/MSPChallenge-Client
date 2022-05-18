using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MSP2050.Scripts
{
	[RequireComponent(typeof(CustomButtonColorSet))]
	public class CustomButtonColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		[FormerlySerializedAs("customButtonColorSets")]
		[SerializeField] private List<CustomButtonColorSet> m_CustomButtonColorSets = new List<CustomButtonColorSet>();
		[FormerlySerializedAs("useHighlightColorOnStart")]
		[SerializeField] private bool m_UseHighlightColorOnStart = true;
		[FormerlySerializedAs("useHighlightColorOnConnected")]
		[SerializeField] private bool m_UseHighlightColorOnConnected = true;
		[FormerlySerializedAs("useHighlightColorOnDisconnected")]
		[SerializeField] private bool m_UseHighlightColorOnDisconnected = false;
		
		protected override void OnStart()
		{
			if (m_CustomButtonColorSets.Count == 0)
			{
				// auto-fill
				m_CustomButtonColorSets.AddRange(gameObject.GetComponents<CustomButtonColorSet>());
			}
			if (m_CustomButtonColorSets.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component CustomToggleColorSet for game object:" + gameObject.name);
				return;
			}
			
			m_CustomButtonColorSets.ForEach(delegate(CustomButtonColorSet a_Set)
			{
				a_Set.useHighlightColor = m_UseHighlightColorOnStart;
			});
		}

		protected override void OnNotifyConnection(bool a_Connected)
		{
			if (m_CustomButtonColorSets.Count == 0)
			{
				return;
			}
			
			m_CustomButtonColorSets.ForEach(delegate(CustomButtonColorSet a_Set)
			{
				a_Set.useHighlightColor = a_Connected ? m_UseHighlightColorOnConnected : m_UseHighlightColorOnDisconnected;
			});
		}
	}
}