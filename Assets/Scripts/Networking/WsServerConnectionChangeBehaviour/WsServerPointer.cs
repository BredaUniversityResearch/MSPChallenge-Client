using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MSP2050.Scripts
{
	public class WsServerPointer
	{
		private static GameObject m_Pointer = null;
		private readonly GameObject m_PointerInstance = null;

		private readonly GameObject m_Parent = null;

		public WsServerPointer(GameObject a_Parent)
		{
			m_Parent = a_Parent;
			if (m_Pointer == null) // need to find pointer game object in scene
			{
				m_Pointer = GameObject.Find("WsServerPointer");
			}
			if (m_Pointer == null) // could not find pointer game object
			{
				return;
			}
			// found pointer game object, make an instance of it
			m_PointerInstance = Object.Instantiate(m_Pointer, a_Parent.gameObject.transform);
		}

		public void Show()
		{
			if (m_PointerInstance == null)
			{
				return;
			}

			RectTransform parentRectTrans = m_Parent.GetComponent<RectTransform>();
			Vector3[] corners = new Vector3[4];
			parentRectTrans.GetWorldCorners(corners);
			m_PointerInstance.transform.position = corners[0]; // bottom left corner
			Rect parentRect = parentRectTrans.rect;
			LeanTween.moveLocalX(m_PointerInstance, parentRect.x + parentRect.width, 1f).setLoopPingPong();
			m_PointerInstance.GetComponent<Image>().enabled = true;
		}

		public void Hide()
		{
			if (m_PointerInstance == null)
			{
				return;
			}
			LeanTween.cancel(m_PointerInstance);
			m_PointerInstance.GetComponent<Image>().enabled = false;
		}
	}
}