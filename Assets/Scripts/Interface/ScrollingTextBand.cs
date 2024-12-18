using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ScrollingTextBand : MonoBehaviour {

		public float scrollSpeed;
		public RectTransform textParent;
		float childSize;
		[SerializeField] float currentOffset;

		void Start()
		{
			childSize = textParent.GetChild(0).GetComponent<LayoutElement>().minWidth;
		}

		void Update()
		{
			currentOffset += scrollSpeed * Time.deltaTime;
			if (currentOffset > childSize)
				currentOffset -= childSize;
			//group.padding.left += (int)currentOffset;
			textParent.anchoredPosition = new Vector2(-currentOffset, textParent.anchoredPosition.y);

		}
	}
}
