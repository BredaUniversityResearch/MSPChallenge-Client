using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ToolbarButtonType : MonoBehaviour
{
    public FSM.ToolbarInput buttonType;

    protected void Start()
    {
        UIManager.ToolbarButtons.Add(this.GetComponent<Button>());
		GetComponent<Button>().onClick.AddListener(() =>
		{
			UIManager.GetToolBar().PressButton(buttonType);
		});
    }
}
