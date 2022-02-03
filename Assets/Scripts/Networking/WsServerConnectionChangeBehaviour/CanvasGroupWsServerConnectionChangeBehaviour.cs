
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public class CanvasGroupWsServerConnectionChangeBehaviour: WsServerConnectionChangeBehaviour
	{
		private readonly List<float> _originalAlphas = new List<float>();
		public List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
		
		[SerializeField] public float alphaFactorOnStart = 1.0f;
		[SerializeField] public float alphaFactorOnConnected = 1.0f;
		[SerializeField] public float alphaFactorOnDisconnected = 1.0f;

		[SerializeField] public bool interactableOnStart = true;
		[SerializeField] public bool interactableOnConnected = true;
		[SerializeField] public bool interactableOnDisconnected = true;

		[SerializeField] public bool blockRaycastsOnStart = true;
		[SerializeField] public bool blockRaycastsOnConnected = true;
		[SerializeField] public bool blockRaycastsOnDisconnected = true;
		
		public void Start()
		{
			if (canvasGroups.Count == 0)
			{
				// auto-fill
				canvasGroups.AddRange(gameObject.GetComponents<CanvasGroup>());
			}
			if (canvasGroups.Count == 0)
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

		public override void NotifyConnection(bool connected)
		{
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