using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ColourPalette;

public class ToolBar : MonoBehaviour
{
    public enum DrawingMode { None, Create, Edit }
    [HideInInspector]
    public DrawingMode drawingMode;
    
    public Animator anim;
    
    public Button createButton, editButton;

    public List<GameObject> devOptions;
	public Image createButtonIcon;
	public Sprite pointCreateSprite, lineCreateSprite, polygonCreateSprite;

    public void Start()
    {
        drawingMode = DrawingMode.None;
        SetModeSprites();

        if (!Application.isEditor)
        {
            foreach (GameObject obj in devOptions)
                obj.SetActive(false);
        }

        // Open options menu via the toolbar
        //for (int i = 0; i < labelButton.Length; i++)
        //{
        //    labelButton[i].onClick.AddListener(delegate () { InterfaceCanvas.instance.options.gameObject.SetActive(true); });
        //}
    }

    public void ShowToolBar(bool show)
    {
        anim.SetBool("SlideIn", show);
    }

    // Tools
    public void CreateMode()
    {
        // drawingMode = (drawingMode != DrawingMode.Create) ? DrawingMode.Create : DrawingMode.None;
        drawingMode = DrawingMode.Create;
        SetModeSprites();

        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Create);
    }

    public void EditMode()
    {
        //drawingMode = (drawingMode != DrawingMode.Edit) ? DrawingMode.Edit : DrawingMode.None;
        drawingMode = DrawingMode.Edit;
        SetModeSprites();

        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Edit);
    }

    // History
    public void Undo()
    {
        if (Debug.isDebugBuild)
            Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Undo);
    }

    public void Redo()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Redo);
    }

    // Drawing
    public void Cut()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        //pressButton(FSM.ToolbarInput.Intersect);
    }

    public void Copy()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        //pressButton(FSM.ToolbarInput.Difference);
    }

    public void Paste()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        //pressButton(FSM.ToolbarInput.Create);
    }

    public void Delete()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Delete);
    }

    public void Recall()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Recall);
    }

    public void Abort()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Abort);
    }

    // Plan
    public void Cancel()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Cancel);
    }

    public void Accept()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Accept);
    }

    // Set Operations
    public void Union()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Union);
    }

    public void Intersect()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Intersect);
    }

    public void Difference()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.Difference);
    }

    // Dev Operations
    public void SelectAll()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.SelectAll);
    }

    //public void Simplify()
    //{
    //    //if (Debug.isDebugBuild)
    //    //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

    //    PressButton(FSM.ToolbarInput.Simplify);
    //}

    public void FixInvalid()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.FixInvalid);
    }

    public void RemoveHoles()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.RemoveHoles);
    }

    public void FindGaps()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.FindGaps);
    }

    public void SnapPoints()
    {
        //if (Debug.isDebugBuild)
        //    Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

        PressButton(FSM.ToolbarInput.SnapPoints);
    }
	
	public void ChangeDirection()
	{
		PressButton(FSM.ToolbarInput.ChangeDirection);
	}

    public void PressButton(FSM.ToolbarInput buttonType)
    {
        FSM.ToolbarButtonClicked(buttonType);
    }

    public void SetActive(Button button, bool toggle)
    {
        button.interactable = toggle;
    }

    public void SetModeSprites()
    {
        switch (drawingMode)
        {
            case DrawingMode.None:
				//editButton.GetComponent<CustomButtonColorSet>().UnlockColor();
				//createButton.GetComponent<CustomButtonColorSet>().UnlockColor();
				editButton.interactable = true;
				createButton.interactable = true;
				break;
            case DrawingMode.Create:
				//createButton.GetComponent<CustomButtonColorSet>().LockToColor(new ConstColour(Color.black));
				//editButton.GetComponent<CustomButtonColorSet>().UnlockColor();
				editButton.interactable = true;
				createButton.interactable = false;
				break;
            case DrawingMode.Edit:
				//editButton.GetComponent<CustomButtonColorSet>().LockToColor(new ConstColour(Color.black));
				//createButton.GetComponent<CustomButtonColorSet>().UnlockColor();
				editButton.interactable = false;
				createButton.interactable = true;
                break;
        }
    }

	public void SetCreateButtonSprite(AbstractLayer layer)
	{
		if (layer is PointLayer)
		{
			createButtonIcon.sprite = pointCreateSprite;
		}
		else if (layer is PolygonLayer)
		{
			createButtonIcon.sprite = polygonCreateSprite;
		}
		else 
		{
			createButtonIcon.sprite = lineCreateSprite;
		}
	}
}
