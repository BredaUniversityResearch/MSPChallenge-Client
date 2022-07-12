using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace MSP2050.Scripts
{
	[RequireComponent(typeof(AddTooltip))]
	public class AddTooltipWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		[FormerlySerializedAs("addTooltips")]
		[SerializeField] private List<AddTooltip> m_AddTooltips = new List<AddTooltip>();
		[FormerlySerializedAs("textOnStart")]
		[SerializeField, TextArea] private string m_TextOnStart = "";
		[FormerlySerializedAs("textOnConnected")]
		[SerializeField, TextArea] private string m_TextOnConnected = "";
		[FormerlySerializedAs("textOnDisconnected")]
		[SerializeField, TextArea] public string m_TextOnDisconnected = "";

		protected override void OnStart()
		{
			if (m_AddTooltips.Count == 0)
			{
				// auto-fill
				m_AddTooltips.AddRange(gameObject.GetComponents<AddTooltip>());
			}
			if (m_AddTooltips.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component AddTooltip for game object:" + gameObject.name);
				return;
			}
			m_AddTooltips.ForEach(delegate(AddTooltip a_Tooltip)
			{
				a_Tooltip.text = m_TextOnStart;
			});
		}

		protected override void OnNotifyConnection(bool a_Connected)
		{
			if (m_AddTooltips.Count == 0)
			{
				return;
			}

			m_AddTooltips.ForEach(delegate(AddTooltip a_Tooltip)
			{
				a_Tooltip.text = a_Connected ? m_TextOnConnected : m_TextOnDisconnected;
			});
		}
	}
}