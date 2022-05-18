using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class WsServerConnectionChangeBehaviour : MonoBehaviour
	{
		private void OnEnable()
		{
			bool? connected = UpdateData.WsServerCommunicationInteractor?.IsConnected();
			if (connected == null)
			{
				return;
			}
			NotifyConnection(connected.Value);
		}

		private void Start()
		{
			OnStart();
			bool? connected = UpdateData.WsServerCommunicationInteractor?.IsConnected();
			if (connected == null)
			{
				return;
			}
			NotifyConnection(connected.Value);
		}

		public void NotifyConnection(bool a_Connected)
		{
			OnNotifyConnection(a_Connected);
		}

		protected abstract void OnNotifyConnection(bool a_Connected);
		protected abstract void OnStart();
	}
}