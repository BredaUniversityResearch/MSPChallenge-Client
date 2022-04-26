using UnityEngine;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class OrderWindowOnClick : MonoBehaviour, IPointerDownHandler
	{
		public void OnPointerDown(PointerEventData eventData)
		{
			transform.SetAsLastSibling();
		}

		void OnEnable()
		{
			transform.SetAsLastSibling();
		}
	}
}
