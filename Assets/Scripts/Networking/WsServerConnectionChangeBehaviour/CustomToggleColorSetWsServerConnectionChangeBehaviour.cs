using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Networking.WsServerConnectionChangeBehaviour
{
	[RequireComponent(typeof(CustomToggleColorSet))]
	public class CustomToggleColorSetWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		[FormerlySerializedAs("customToggleColorSets")]
		[SerializeField] private List<CustomToggleColorSet> m_CustomToggleColorSets = new List<CustomToggleColorSet>();
		[FormerlySerializedAs("useHighlightColorOnStart")]
		[SerializeField] private bool m_UseHighlightColorOnStart = true;
		[FormerlySerializedAs("useHighlightColorOnConnected")]
		[SerializeField] private bool m_UseHighlightColorOnConnected = true;
		[FormerlySerializedAs("useHighlightColorOnDisconnected")]
		[SerializeField] private bool m_UseHighlightColorOnDisconnected = false;

		protected override void OnStart()
		{
			if (m_CustomToggleColorSets.Count == 0)
			{
				// auto-fill
				m_CustomToggleColorSets.AddRange(gameObject.GetComponents<CustomToggleColorSet>());
			}
			if (m_CustomToggleColorSets.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component CustomToggleColorSet for game object:" + gameObject.name);
				return;
			}
			
			m_CustomToggleColorSets.ForEach(delegate(CustomToggleColorSet a_Set)
			{
				a_Set.useHighlightColor = m_UseHighlightColorOnStart;
			});
		}

		protected override void OnNotifyConnection(bool a_Connected)
		{
			if (m_CustomToggleColorSets.Count == 0)
			{
				return;
			}

			m_CustomToggleColorSets.ForEach(delegate(CustomToggleColorSet a_Set)
			{
				a_Set.useHighlightColor = a_Connected ? m_UseHighlightColorOnConnected : m_UseHighlightColorOnDisconnected;
			});
		}
	}
}