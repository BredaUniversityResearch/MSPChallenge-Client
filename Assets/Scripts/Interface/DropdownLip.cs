using System.Collections;
using System.Collections.Generic;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{
	public class DropdownLip : MonoBehaviour
	{
		void OnEnable()
		{
			StartCoroutine(SetPosition());
		}

		IEnumerator SetPosition()
		{
			yield return new WaitForEndOfFrame();
			RectTransform parent = transform.parent as RectTransform;
			if (parent.anchorMin.y > 0.5f)
			{
				//Dropdown expanded up, so lip should be on the bottom
				RectTransform rect = transform as RectTransform;
				rect.anchorMax = new Vector2(0.5f, 0f);
				rect.anchorMin = new Vector2(0.5f, 0f);
				rect.localScale = new Vector3(1f, -1f, 1f);
				rect.anchoredPosition = new Vector2(0f, 1f);
			}
			else
			{
				RectTransform rect = transform as RectTransform;
				rect.anchorMax = new Vector2(0.5f, 1f);
				rect.anchorMin = new Vector2(0.5f, 1f);
				rect.localScale = new Vector3(1f, 1f, 1f);
				rect.anchoredPosition = new Vector2(0f, -1f);
			}
		}
	}
}
