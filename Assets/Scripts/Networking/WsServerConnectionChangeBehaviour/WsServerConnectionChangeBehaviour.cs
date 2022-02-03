using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public abstract class WsServerConnectionChangeBehaviour : MonoBehaviour
	{
		public abstract void NotifyConnection(bool connected);
	}
}