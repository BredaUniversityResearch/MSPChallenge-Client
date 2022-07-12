using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MSP2050.Scripts
{
	[RequireComponent(typeof(CustomDropdownColorSet))]
	public class CustomDropdownColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		[FormerlySerializedAs("customDropdownColorSets")]
		[SerializeField] private List<CustomDropdownColorSet> m_CustomDropdownColorSets = new List<CustomDropdownColorSet>();
		[FormerlySerializedAs("useHighlightColorOnStart")]
		[SerializeField] private bool m_UseHighlightColorOnStart = true;
		[FormerlySerializedAs("useHighlightColorOnConnected")]
		[SerializeField] private bool m_UseHighlightColorOnConnected = true;
		[FormerlySerializedAs("useHighlightColorOnDisconnected")]
		[SerializeField] private bool m_UseHighlightColorOnDisconnected = false;
		
		protected override void OnStart()
		{
			if (m_CustomDropdownColorSets.Count == 0)
			{
				// auto-fill
				m_CustomDropdownColorSets.AddRange(gameObject.GetComponents<CustomDropdownColorSet>());
			}
			if (m_CustomDropdownColorSets.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component CustomDropdownColorSet for game object:" + gameObject.name);
				return;
			}
			
			m_CustomDropdownColorSets.ForEach(delegate(CustomDropdownColorSet a_Set)
			{
				a_Set.useHighlightColor = m_UseHighlightColorOnStart;
			});
		}

		protected override void OnNotifyConnection(bool a_Connected)
		{
			if (m_CustomDropdownColorSets.Count == 0)
			{
				return;
			}
			
			m_CustomDropdownColorSets.ForEach(delegate(CustomDropdownColorSet a_Set)
			{
				a_Set.useHighlightColor = a_Connected ? m_UseHighlightColorOnConnected : m_UseHighlightColorOnDisconnected;
			});
		}
	}
}