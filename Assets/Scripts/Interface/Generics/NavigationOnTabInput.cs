using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

class NavigationOnTabInput: MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			EventSystem system = EventSystem.current;
			Selectable next = null;
			if (system.currentSelectedGameObject != null)
			{
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
				}
				else
				{
					next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
				}
			}

			if (next != null)
			{
				system.SetSelectedGameObject(next.gameObject);
			}
		}
	}
}
