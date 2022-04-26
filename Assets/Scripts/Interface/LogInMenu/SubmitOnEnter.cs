using UnityEngine;
using UnityEngine.Events;

namespace MSP2050.Scripts
{
	public class SubmitOnEnter: MonoBehaviour
	{
		[SerializeField]
		private UnityEvent submitEvent = null;

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
			{
				if (submitEvent != null)
				{
					submitEvent.Invoke();
				}
			}
		}
	}
}
