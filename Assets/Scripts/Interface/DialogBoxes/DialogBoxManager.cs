using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace MSP2050.Scripts
{
	public class DialogBoxManager : MonoBehaviour
	{
		private static DialogBoxManager singleton;

		public static DialogBoxManager instance
		{
			get
			{
				if (singleton == null)
					singleton = (DialogBoxManager)FindObjectOfType(typeof(DialogBoxManager));
				return singleton;
			}
		}
    
		public DialogBox dialogBoxPrefab;
		private List<DialogBox> boxes = new List<DialogBox>(2);

		public bool CancelTopDialog()
		{
			if (boxes.Count > 0)
			{
				DialogBox box = boxes[boxes.Count - 1];
				if(box.lb.gameObject.activeInHierarchy)
					box.lb.onClick.Invoke();
				else
					box.rb.onClick.Invoke();
				return true;
			}
			return false;
		}

		public DialogBox ConfirmationWindow(string title, string description, UnityAction leftButton, UnityAction rightButton, string leftButtonText = "Cancel", string rightButtonText = "Confirm")
		{
			DialogBox dialogBox = (DialogBox)Instantiate(dialogBoxPrefab, transform, false);
			dialogBox.transform.SetAsLastSibling();

			dialogBox.title.text = title;
			dialogBox.description.text = description;
			dialogBox.lbDescriptor.text = leftButtonText;
			dialogBox.rbDescriptor.text = rightButtonText;
			dialogBox.lb.gameObject.SetActive(true);

			if (leftButton != null)
				dialogBox.lb.onClick.AddListener(leftButton);

			dialogBox.lb.onClick.AddListener(() => DestroyDialogBox(dialogBox));

			if (rightButton != null)
				dialogBox.rb.onClick.AddListener(rightButton);

			dialogBox.rb.onClick.AddListener(() => DestroyDialogBox(dialogBox));

			//dialogBox.modalBackground = CreateModalBackground(dialogBox.transform);
			boxes.Add(dialogBox);
			return dialogBox;
		}

		public DialogBox NotificationWindow(string title, string description, UnityAction button, string buttonText = "Continue")
		{
			DialogBox dialogBox = (DialogBox)Instantiate(dialogBoxPrefab, transform, false);
			dialogBox.transform.SetAsLastSibling();

			dialogBox.title.text = title;
			dialogBox.description.text = description;

			dialogBox.lb.gameObject.SetActive(false);
			dialogBox.rbDescriptor.text = buttonText;

			if (button != null)
				dialogBox.rb.onClick.AddListener(button);

			dialogBox.rb.onClick.AddListener(() => DestroyDialogBox(dialogBox));

			boxes.Add(dialogBox);
			return dialogBox;
		}

		public DialogBox NotificationListWindow(string title, string description, List<string> list, UnityAction button, string buttonText = "Continue")
		{
			DialogBox dialogBox = NotificationWindow(title, description, button, buttonText);

			foreach(string entry in list)
			{
				TextMeshProUGUI text = Instantiate(dialogBox.listPrefab, dialogBox.listParent).GetComponent<TextMeshProUGUI>();
				text.text = entry;
			}

			return dialogBox;
		}

		/// <summary>
		/// Destroy this
		/// </summary>
		public void DestroyDialogBox(DialogBox box)
		{
			boxes.Remove(box);
			Destroy(box.gameObject);
		}

		/// <summary>
		/// Create a modal background that prevents interacting with other siblings
		/// </summary>
		//public GameObject CreateModalBackground(Transform trans)
		//{

		//	GameObject modalBackground = Instantiate(modalBackgroundPrefab, transform);

		//	//// Set it to be behind the edit window
		//	modalBackground.transform.SetSiblingIndex(trans.GetSiblingIndex());
		//	return modalBackground;
		//}
	}
}