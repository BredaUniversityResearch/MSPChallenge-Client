using UnityEngine;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public abstract class WsServerConnectionChangeBehaviour : MonoBehaviour
	{
		private void OnEnable()
		{
			if (UpdateData.WsServerCommunication?.IsConnected == null)
			{
				return;
			}
			NotifyConnection(UpdateData.WsServerCommunication.IsConnected.Value);
		}

		private void Start()
		{
			OnStart();
			if (UpdateData.WsServerCommunication?.IsConnected == null)
			{
				return;
			}
			NotifyConnection(UpdateData.WsServerCommunication.IsConnected.Value);
		}

		public void NotifyConnection(bool a_Connected)
		{
			OnNotifyConnection(a_Connected);
		}

		protected abstract void OnNotifyConnection(bool a_Connected);
		protected abstract void OnStart();
	}
}