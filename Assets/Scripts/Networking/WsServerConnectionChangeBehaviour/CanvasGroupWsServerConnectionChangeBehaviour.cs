using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Networking.WsServerConnectionChangeBehaviour
{
	[RequireComponent(typeof(CanvasGroup))]
	public class CanvasGroupWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		private WsServerPointer m_Pointer = null;
		private readonly List<float> m_OriginalAlphas = new List<float>();
		[FormerlySerializedAs("canvasGroups")]
		[SerializeField] private List<CanvasGroup> m_CanvasGroups = new List<CanvasGroup>();

		[FormerlySerializedAs("enablePointer")]
		[SerializeField] private bool m_EnablePointer = true;
		[FormerlySerializedAs("pointerTargetGameObject")]
		[SerializeField] private GameObject m_PointerTargetGameObject = null;
		[FormerlySerializedAs("alphaFactorOnStart")]
		[SerializeField] private float m_AlphaFactorOnStart = 1.0f;
		[FormerlySerializedAs("alphaFactorOnConnected")]
		[SerializeField] private float m_AlphaFactorOnConnected = 1.0f;
		[FormerlySerializedAs("alphaFactorOnDisconnected")]
		[SerializeField] private float m_AlphaFactorOnDisconnected = 1.0f;

		[FormerlySerializedAs("interactableOnStart")]
		[SerializeField] private bool m_InteractableOnStart = true;
		[FormerlySerializedAs("interactableOnConnected")]
		[SerializeField] private bool m_InteractableOnConnected = true;
		[FormerlySerializedAs("interactableOnDisconnected")]
		[SerializeField] private bool m_InteractableOnDisconnected = true;

		[FormerlySerializedAs("blockRaycastsOnStart")]
		[SerializeField] private bool m_BlockRaycastsOnStart = true;
		[FormerlySerializedAs("blockRaycastsOnConnected")]
		[SerializeField] private bool m_BlockRaycastsOnConnected = true;
		[FormerlySerializedAs("blockRaycastsOnDisconnected")]
		[SerializeField] private bool m_BlockRaycastsOnDisconnected = true;
		
		protected override void OnStart()
		{
			if (m_PointerTargetGameObject == null)
			{
				m_PointerTargetGameObject = gameObject;
			}
			if (m_EnablePointer)
			{
				m_Pointer = new WsServerPointer(m_PointerTargetGameObject);
			}
			if (m_CanvasGroups.Count == 0)
			{
				// auto-fill
				m_CanvasGroups.AddRange(gameObject.GetComponents<CanvasGroup>());
			}
			if (m_CanvasGroups.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component CanvasGroup for game object:" + gameObject.name);
				return;
			}

			m_CanvasGroups.ForEach(delegate(CanvasGroup a_Group)
			{
				m_OriginalAlphas.Add(a_Group.alpha);
				a_Group.alpha *= m_AlphaFactorOnStart;
				a_Group.interactable = m_InteractableOnStart;
				a_Group.blocksRaycasts = m_BlockRaycastsOnStart;
			});
		}

		protected override void OnNotifyConnection(bool a_Connected)
		{
			if (a_Connected)
			{
				m_Pointer?.Hide();
			}
			else
			{
				m_Pointer?.Show();
			}

			if (m_CanvasGroups.Count == 0)
			{
				return;
			}

			foreach (var (group, i) in m_CanvasGroups.Select((a_Group, i) => (@group: a_Group, i)))
			{
				group.alpha = a_Connected ? m_OriginalAlphas[i] * m_AlphaFactorOnConnected :
					m_OriginalAlphas[i] * m_AlphaFactorOnDisconnected;
				group.interactable = a_Connected ? m_InteractableOnConnected : m_InteractableOnDisconnected;
				group.blocksRaycasts = a_Connected ? m_BlockRaycastsOnConnected : m_BlockRaycastsOnDisconnected;
			}
		}
	}
}