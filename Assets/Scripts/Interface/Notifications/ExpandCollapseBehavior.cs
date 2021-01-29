using UnityEngine;
using UnityEngine.UI;

namespace Interface.Notifications
{
	public class ExpandCollapseBehavior : MonoBehaviour
	{
		[SerializeField, Tooltip("Transform that is toggled on and off to achieve the toggle")]
		private RectTransform collapseTransform = null;

		[SerializeField, Tooltip("Arrow that rotates depending on the toggle state, can be null")]
		private RectTransform collapseArrow = null;

		[SerializeField, Tooltip("Toggle that controls the collapse / expand when clicked")]
		private Toggle collapseToggle = null;

		private void Awake()
		{
			collapseToggle.onValueChanged.AddListener(SetExpanded);
		}

		private void SetExpanded(bool value)
		{
			if (collapseTransform.gameObject.activeInHierarchy == value)
				return;

			collapseTransform.gameObject.SetActive(value);
			if (collapseArrow != null)
			{
				Vector3 oldRotation = collapseArrow.eulerAngles;
				collapseArrow.eulerAngles = !value ? new Vector3(oldRotation.x, oldRotation.y, 90f) : new Vector3(oldRotation.x, oldRotation.y, 0f);
			}
		}
	}
}
