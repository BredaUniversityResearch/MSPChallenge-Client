using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public abstract class WsServerConnectionChangeBehaviour : MonoBehaviour
	{
		private void OnEnable()
		{
			if (UpdateData.WsServerConnected == null)
			{
				return;
			}
			NotifyConnection(UpdateData.WsServerConnected.Value);
		}

		private void Start()
		{
			OnStart();
			if (UpdateData.WsServerConnected == null)
			{
				return;
			}
			NotifyConnection(UpdateData.WsServerConnected.Value);
		}

		public void NotifyConnection(bool a_Connected)
		{
			OnNotifyConnection(a_Connected);
		}

		protected abstract void OnNotifyConnection(bool a_Connected);
		protected abstract void OnStart();
	}
}