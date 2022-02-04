using System;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.WsServerConnectionChangeBehaviour
{
	public class WsServerPointer
	{
		private static GameObject _pointer = null;
		private GameObject _pointerInstance = null;
		private LTDescr _ltDescr = null;

		private GameObject _parent = null;

		public WsServerPointer(GameObject parent)
		{
			_parent = parent;
			_pointer = GameObject.Find("WsServerPointer");
			if (_pointer == null)
			{
				return;
			}
			_pointerInstance = GameObject.Instantiate(_pointer, parent.gameObject.transform);
		}

		public void Show()
		{
			if (_pointerInstance == null)
			{
				return;
			}
			RectTransform parentRectTrans = _parent.GetComponent<RectTransform>();
			Vector3[] corners = new Vector3[4];
			parentRectTrans.GetWorldCorners(corners);
			_pointerInstance.transform.position = corners[0]; // bottom left corner
			Rect parentRect = parentRectTrans.rect;
			_ltDescr = LeanTween
				.moveLocalX(_pointerInstance, parentRect.x + parentRect.width, 1f).setLoopPingPong();
			_pointerInstance.GetComponent<Image>().enabled = true;
		}

		public void Hide()
		{
			if (_pointerInstance == null)
			{
				return;
			}
			LeanTween.cancel(_pointerInstance);
			_ltDescr = null;
			_pointerInstance.GetComponent<Image>().enabled = false;
		}
	}
}