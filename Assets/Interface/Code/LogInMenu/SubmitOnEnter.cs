using UnityEngine;
using UnityEngine.Events;

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
