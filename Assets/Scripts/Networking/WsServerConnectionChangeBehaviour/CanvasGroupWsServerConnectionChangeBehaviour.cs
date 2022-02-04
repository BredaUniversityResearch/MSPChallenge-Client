
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	[RequireComponent(typeof(CanvasGroup))]
	public class CanvasGroupWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		private WsServerPointer _pointer = null;
		private readonly List<float> _originalAlphas = new List<float>();
		public List<CanvasGroup> canvasGroups = new List<CanvasGroup>();

		[SerializeField] public bool enablePointer = true;
		[SerializeField] public GameObject pointerTargetGameObject = null;
		[SerializeField] public float alphaFactorOnStart = 1.0f;
		[SerializeField] public float alphaFactorOnConnected = 1.0f;
		[SerializeField] public float alphaFactorOnDisconnected = 1.0f;

		[SerializeField] public bool interactableOnStart = true;
		[SerializeField] public bool interactableOnConnected = true;
		[SerializeField] public bool interactableOnDisconnected = true;

		[SerializeField] public bool blockRaycastsOnStart = true;
		[SerializeField] public bool blockRaycastsOnConnected = true;
		[SerializeField] public bool blockRaycastsOnDisconnected = true;
		
		protected override void OnStart()
		{
			if (pointerTargetGameObject == null)
			{
				pointerTargetGameObject = gameObject;
			}
			if (enablePointer)
			{
				_pointer = new WsServerPointer(pointerTargetGameObject);
			}
			if (canvasGroups.Count == 0)
			{
				// auto-fill
				canvasGroups.AddRange(gameObject.GetComponents<CanvasGroup>());
			}
			if (canvasGroups.Count == 0) // this should not happen because of "RequireComponent"
			{
				Debug.LogError("Missing component CanvasGroup for game object:" + gameObject.name);
				return;
			}

			canvasGroups.ForEach(delegate(CanvasGroup group)
			{
				_originalAlphas.Add(group.alpha);
				group.alpha *= alphaFactorOnStart;
				group.interactable = interactableOnStart;
				group.blocksRaycasts = blockRaycastsOnStart;
			});
		}

		protected override void OnNotifyConnection(bool connected)
		{
			if (connected)
			{
				_pointer?.Hide();
			}
			else
			{
				_pointer?.Show();
			}

			if (canvasGroups.Count == 0)
			{
				return;
			}

			foreach (var (group, i) in canvasGroups.Select((group, i) => (group, i)))
			{
				group.alpha = connected ? _originalAlphas[i] * alphaFactorOnConnected :
					_originalAlphas[i] * alphaFactorOnDisconnected;
				group.interactable = connected ? interactableOnConnected : interactableOnDisconnected;
				group.blocksRaycasts = connected ? blockRaycastsOnConnected : blockRaycastsOnDisconnected;
			}
		}
	}
}