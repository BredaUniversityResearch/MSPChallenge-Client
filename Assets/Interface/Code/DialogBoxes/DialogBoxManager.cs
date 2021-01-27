using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    public GameObject modalBackgroundPrefab;
    private List<DialogBox> boxes;

    void Awake()
    {
	    boxes = new List<DialogBox>(2);
    }

    public bool CancelTopDialog()
    {
	    if (boxes.Count > 0)
	    {
		    DialogBox box = boxes[boxes.Count - 1];
			if(box.leftButtonContainer.activeInHierarchy)
				box.lb.onClick.Invoke();
			else
				box.rb.onClick.Invoke();
			return true;
	    }
	    return false;
    }

    public DialogBox ConfirmationWindow(string title, string description, UnityAction leftButton, UnityAction rightButton, string leftButtonText = "Cancel", string rightButtonText = "Confirm")
    {
        DialogBox dialogBox = (DialogBox)Instantiate(dialogBoxPrefab, /*InterfaceCanvas.Instance.*/transform, false);
        dialogBox.transform.SetAsLastSibling();

        dialogBox.title.text = title;
        dialogBox.description.text = description;
        dialogBox.lbDescriptor.text = leftButtonText;
        dialogBox.rbDescriptor.text = rightButtonText;
        dialogBox.leftButtonContainer.SetActive(true);

        if (leftButton != null)
            dialogBox.lb.onClick.AddListener(leftButton);

        dialogBox.lb.onClick.AddListener(() => DestroyDialogBox(dialogBox));

        if (rightButton != null)
            dialogBox.rb.onClick.AddListener(rightButton);

        dialogBox.rb.onClick.AddListener(() => DestroyDialogBox(dialogBox));

		dialogBox.modalBackground = CreateModalBackground(dialogBox.thisRectTrans);
		boxes.Add(dialogBox);
		return dialogBox;
    }

    public DialogBox NotificationWindow(string title, string description, UnityAction button, string buttonText = "")
    {
        DialogBox dialogBox = (DialogBox)Instantiate(dialogBoxPrefab, /*InterfaceCanvas.Instance.*/transform, false);
        dialogBox.transform.SetAsLastSibling();

        dialogBox.title.text = title;
        dialogBox.description.text = description;

        dialogBox.leftButtonContainer.SetActive(false);
        dialogBox.rbDescriptor.text = buttonText;

        if (button != null)
            dialogBox.rb.onClick.AddListener(button);

        dialogBox.rb.onClick.AddListener(() => DestroyDialogBox(dialogBox));

        dialogBox.modalBackground = CreateModalBackground(dialogBox.thisRectTrans);
		boxes.Add(dialogBox);
        return dialogBox;
    }

    /// <summary>
    /// Destroy this
    /// </summary>
    public void DestroyDialogBox(DialogBox box)
    {
	    if (box.modalBackground) {
            Destroy(box.modalBackground);
        }

	    boxes.Remove(box);
	    Destroy(box.gameObject);
    }

    /// <summary>
    /// Create a modal background that prevents interacting with other siblings
    /// </summary>
    public GameObject CreateModalBackground(RectTransform rectTrans)
    {

        // Instantiate prefab
        GameObject modalBackground = Instantiate(modalBackgroundPrefab);

        // Assign background parent
        modalBackground.transform.SetParent(/*InterfaceCanvas.Instance.*/transform, false);

        //// Set it to be behind the edit window
        modalBackground.transform.SetSiblingIndex(rectTrans.GetSiblingIndex());
        return modalBackground;
    }
}