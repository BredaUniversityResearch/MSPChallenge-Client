using UnityEngine;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class OnPointerExitClose : MonoBehaviour, IPointerExitHandler {

		public GameObject closeThis;

		public void OnPointerExit(PointerEventData eventData)
		{
			closeThis.SetActive(false);
		}
	}
}
