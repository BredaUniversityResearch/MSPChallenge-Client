using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CradleImpactTool
{
	public static class GameObjectExtensions
	{
		// Get bounds of the current element, including children
		public static Bounds GetAbsoluteUIBounds(this RectTransform a_rectTransform)
		{
			Bounds resultBounds = new Bounds(a_rectTransform.rect.center, Vector3.zero);

			RectTransform[] children = a_rectTransform.GetComponentsInChildren<RectTransform>();
			foreach (RectTransform child in children)
			{
				if (child == a_rectTransform)
					continue;

				Bounds bounds = child.GetAbsoluteUIBounds();
				bounds.center = child.position - a_rectTransform.position;
				resultBounds.Encapsulate(bounds);
			}

			TMP_Text[] texts = a_rectTransform.GetComponentsInChildren<TMP_Text>();
			foreach (TMP_Text text in texts)
			{
				CalculateTextUIBounds(a_rectTransform, text, ref resultBounds);
			}

			UnityEngine.UI.Image[] images = a_rectTransform.GetComponentsInChildren<UnityEngine.UI.Image>();
			if (images.Length > 0)
			{
				Vector3 min = resultBounds.min;
				Vector3 max = resultBounds.max;
				foreach (UnityEngine.UI.Image img in images)
				{
					CalculateImageUIBounds(a_rectTransform, img, ref min, ref max);
				}

				resultBounds.center = (max + min) * 0.5f;
				resultBounds.size = (max - min);
			}

			return resultBounds;
		}

		// Get the bounds of just your own components
		public static Bounds GetOwnUIBounds(this RectTransform a_rectTransform)
		{
			Bounds resultBounds = new Bounds(a_rectTransform.rect.center, Vector3.zero);

			TMP_Text text = a_rectTransform.GetComponent<TMP_Text>();
			if (text != null)
			{
				CalculateTextUIBounds(a_rectTransform, text, ref resultBounds);
			}

			UnityEngine.UI.Image image = a_rectTransform.GetComponent<UnityEngine.UI.Image>();
			if (image != null)
			{
				Vector3 min = resultBounds.min;
				Vector3 max = resultBounds.max;

				CalculateImageUIBounds(a_rectTransform, image, ref min, ref max);

				resultBounds.center = (max + min) * 0.5f;
				resultBounds.size = (max - min);
			}

			return resultBounds;
		}

		static void CalculateTextUIBounds(RectTransform a_rectTransform, TMP_Text a_text, ref Bounds a_resultBounds)
		{
			a_text.ForceMeshUpdate(true, true);
			Bounds bounds = a_text.textBounds;
			Vector2 relPos = a_text.transform.position - a_rectTransform.position;
			bounds.center = relPos;

			Vector3 size = a_text.textBounds.size;
			size.x *= a_rectTransform.lossyScale.x;
			size.y *= a_rectTransform.lossyScale.y;
			size.z *= a_rectTransform.lossyScale.z;
			bounds.size = size;

			a_resultBounds.Encapsulate(bounds);
		}

		static void CalculateImageUIBounds(RectTransform a_rectTransform, UnityEngine.UI.Image a_img, ref Vector3 a_min, ref Vector3 a_max)
		{
			Vector2 relPos = a_img.rectTransform.position - a_rectTransform.position;
			Vector2 localMin = relPos + a_img.rectTransform.rect.min * a_rectTransform.lossyScale;
			Vector2 localMax = relPos + a_img.rectTransform.rect.max * a_rectTransform.lossyScale;
			if (a_min.x > localMin.x)
				a_min.x = localMin.x;
			if (a_max.x < localMax.x)
				a_max.x = localMax.x;
			if (a_min.y > localMin.y)
				a_min.y = localMin.y;
			if (a_max.y < localMax.y)
				a_max.y = localMax.y;
		}
	}
}