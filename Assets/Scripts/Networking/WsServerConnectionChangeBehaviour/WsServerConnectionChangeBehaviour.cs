using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public abstract class WsServerConnectionChangeBehaviour : MonoBehaviour
	{
		private void Start()
		{
			OnStart();
			if (UpdateData.wsServerConnected != null)
			{
				NotifyConnection(UpdateData.wsServerConnected.Value);
			}
		}

		public void NotifyConnection(bool connected)
		{
			OnNotifyConnection(connected);
		}

		protected abstract void OnNotifyConnection(bool connected);
		protected abstract void OnStart();
	}
}