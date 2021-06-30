using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public static class UIManager
{
    public static List<Button> ToolbarButtons = new List<Button>();

    private static InterfaceCanvas interfaceCanvas;
    private static LayerInterface layerInterface;

    public static bool ignoreLayerToggleCallback;//If this is true the layer callback labda functions will return immediately

    static UIManager()
    {
        interfaceCanvas = InterfaceCanvas.Instance;       
        layerInterface = interfaceCanvas.layerPanel.GetComponent<LayerInterface>();
    }

    public static InterfaceCanvas GetInterfaceCanvas()
    {
        return interfaceCanvas;
    }

    public static void StartEditingLayer(AbstractLayer layer)
    {
        ToolbarVisibility(true);
        GetToolBar().ShowToolBar(true);
        ToolbarTitleVisibility(true, FSM.ToolbarInput.Create);
        ToolbarTitleVisibility(true, FSM.ToolbarInput.Delete);
        ToolbarVisibility(false, FSM.ToolbarInput.Difference, FSM.ToolbarInput.Intersect, FSM.ToolbarInput.Union);
        ToolbarTitleVisibility(false, FSM.ToolbarInput.Union);
		GetToolBar().SetCreateButtonSprite(layer);
		ToolbarEnable(true);
    }

    public static void StopEditing()
    {
        ToolbarEnable(false); 
        GetToolBar().ShowToolBar(false);
    }

    public static void StartSetOperations()
    {
        ToolbarVisibility(true);
        ToolbarVisibility(false, FSM.ToolbarInput.Create, FSM.ToolbarInput.Edit, FSM.ToolbarInput.Delete, FSM.ToolbarInput.Abort);
        ToolbarTitleVisibility(false, FSM.ToolbarInput.Create);
        ToolbarTitleVisibility(false, FSM.ToolbarInput.Delete);
        ToolbarTitleVisibility(true, FSM.ToolbarInput.Union);
        GetToolBar().ShowToolBar(true);
        ToolbarEnable(true);
    }

    public static MenuBarToggle GetActiveLayerToggle()
    {
        return interfaceCanvas.menuBarActiveLayers;
    }

    public static TimeBar GetTimeBar()
    {
        return interfaceCanvas.timeBar;
    }

    public static MapScale GetMapScale()
    {
        return interfaceCanvas.mapScale;
    }

    public static ToolBar GetToolBar()
    {
        return interfaceCanvas.toolBar;
    }

    public static LayerPanel GetLayerPanel()
    {
        return interfaceCanvas.layerPanel;
    }

    public static GenericWindow GetLayerSelect()
    {
        return interfaceCanvas.layerSelect;
    }

    public static ActiveLayerWindow GetActiveLayerWindow()
    {
        return interfaceCanvas.activeLayers;
    }

    public static void ShowLayerBar(bool show)
    {
        if (interfaceCanvas != null)
        {
            interfaceCanvas.layerPanel.gameObject.SetActive(show);
        }
        else
        {
            Debug.LogError("Interface canvas not set!");
        }
    }

    public static void ShowTimeBar(bool show)
    {
        if (interfaceCanvas != null)
        {
            interfaceCanvas.timeBar.gameObject.SetActive(show);
        }
        else
        {
            Debug.LogError("Interface canvas not set!");
        }
    }

    public static void ToolbarTitleVisibility(bool enabled, FSM.ToolbarInput button)
    {
        for (int i = 0; i < ToolbarButtons.Count; i++)
        {
            if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == button)
            {
                ToolbarButtons[i].transform.parent.parent.Find("Label").gameObject.SetActive(enabled);
                ToolbarButtons[i].transform.parent.gameObject.SetActive(enabled);
            }
        }
    }

    public static void ToolbarVisibility(bool enabled, params FSM.ToolbarInput[] buttons)
    {
        for (int i = 0; i < ToolbarButtons.Count; i++)
        {
            if (buttons.Length <= 0)
            {
                ToolbarButtons[i].gameObject.SetActive(enabled);
            }
            else
            {
                for (int j = 0; j < buttons.Length; j++)
                {
                    if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == buttons[j])
                    {
                        ToolbarButtons[i].gameObject.SetActive(enabled);
                    }
                }
            }
        }
    }

    public static void SetToolbarMode(ToolBar.DrawingMode drawingMode)
    {
        if (drawingMode == ToolBar.DrawingMode.Create)
        {
            GetToolBar().CreateMode();
        }
        else if (drawingMode == ToolBar.DrawingMode.Edit)
        {
            GetToolBar().EditMode();
        }
    }

    public static void ToolbarEnable(bool enabled, params FSM.ToolbarInput[] buttons)
    {
        for (int i = 0; i < ToolbarButtons.Count; i++)
        {
            if (buttons.Length <= 0)
            {
                //ToolbarButtons[i].interactable = enabled;
                GetToolBar().SetActive(ToolbarButtons[i], enabled);
            }
            else
            {
                for (int j = 0; j < buttons.Length; j++)
                {
                    if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == buttons[j])
                    {
                        //ToolbarButtons[i].interactable = enabled;
                        GetToolBar().SetActive(ToolbarButtons[i], enabled);
                    }
                }
            }
        }
    }

    //public delegate void MultipleWindowValueDelegate(List<string> newValue);
    //public static void CreateMultipleValueWindow(string title, List<string> fieldTitle, List<string> fieldValue, float width, MultipleWindowValueDelegate confirmDelegate)
    //{
    //    GenericWindow editWindow = InterfaceCanvas.instance.CreateGenericWindow(true);
    //    editWindow.title.text = title;
    //    editWindow.SetWidth(width);
    //    editWindow.editEnabled = true;
    //    editWindow.SetPosition(editWindow.centered);

    //    GenericContent content = editWindow.CreateContentWindow(false);
    //    content.SetPrefHeight(32);

    //    List<GenericEntry> entries = new List<GenericEntry>();

    //    for (int i = 0; i < fieldTitle.Count; i++)
    //    {
    //        entries.Add(content.CreateEntry<string>(fieldTitle[i] + ": ", fieldValue[i], true));
    //    }

    //    editWindow.cancelButton.onClick.AddListener(() => { editWindow.Destroy(); });
    //    editWindow.acceptButton.onClick.AddListener(() =>
    //    {
    //        List<string> fields = new List<string>();

    //        foreach (GenericEntry entry in entries)
    //        {
    //            fields.Add(entry.inputField.text);
    //        }

    //        confirmDelegate(fields);


    //        editWindow.Destroy();
    //    });
    //}


    //public delegate void WindowValueDelegate(string newValue);
    //public static void CreateSingleValueWindow(string title, string fieldTitle, string fieldValue, float width, WindowValueDelegate confirmDelegate)
    //{
    //    GenericWindow editWindow = InterfaceCanvas.instance.CreateGenericWindow(true);
    //    editWindow.title.text = title;
    //    editWindow.SetWidth(width);
    //    editWindow.editEnabled = true;
    //    editWindow.SetPosition(editWindow.centered);

    //    GenericContent content = editWindow.CreateContentWindow(false);
    //    content.SetPrefHeight(32);
    //    GenericEntry entry = content.CreateEntry<string>(fieldTitle + ": ", fieldValue, true);

    //    editWindow.cancelButton.onClick.AddListener(() => { editWindow.Destroy(); });
    //    editWindow.acceptButton.onClick.AddListener(() =>
    //    {
    //        confirmDelegate(entry.inputField.text);
    //        editWindow.Destroy();
    //    });
    //}

    //public delegate void WindowConfirmDelegate();
    //public static void CreateConfirmWindow(string title, string message, float width, WindowConfirmDelegate confirmDelegate)
    //{
    //    GenericWindow editWindow = InterfaceCanvas.instance.CreateGenericWindow(true);
    //    editWindow.title.text = title;
    //    editWindow.SetWidth(width);
    //    editWindow.editEnabled = true;
    //    editWindow.SetPosition(editWindow.centered);

    //    GenericContent content = editWindow.CreateContentWindow(false);
    //    content.SetPrefHeight(32);

    //    GenericEntry entry = content.CreateEntry<string>(message, message, true);

    //    // This somehow works, Removes the value and centres the value field
    //    entry.label.GetComponent<LayoutElement>().enabled = false;
    //    entry.value.gameObject.SetActive(false);
    //    entry.inputField.gameObject.SetActive(false);
    //    entry.label.alignment = TextAnchor.MiddleCenter;
    //    Canvas.ForceUpdateCanvases();

    //    editWindow.cancelButton.onClick.AddListener(() => { editWindow.Destroy(); });
    //    editWindow.acceptButton.onClick.AddListener(() =>
    //    {
    //        confirmDelegate();
    //        editWindow.Destroy();
    //    });
    //}

    public static void RefreshActiveLayer(AbstractLayer layer)
    {
        layerInterface.RefreshActiveLayer(layer);
    }

    public static void OnShowLayer(AbstractLayer layer)
    {
        layerInterface.OnShowLayer(layer);
    }

    public static void OnHideLayer(AbstractLayer layer)
    {
        layerInterface.OnHideLayer(layer);
    }

	public static void SetLayerVisibilityLock(AbstractLayer layer, bool value)
	{
		layerInterface.SetLayerVisibilityLock(layer, value);
	}

    public static void CreatePropertiesWindow(SubEntity subentity, Vector3 worldSamplePosition, Vector3 windowPosition)
    {
		InterfaceCanvas.Instance.propertiesWindow.ShowPropertiesWindow(subentity, worldSamplePosition, windowPosition);
    }

    public static void CreateLayerProbeWindow(List<SubEntity> subentities, Vector3 worldSamplePosition, Vector3 windowPosition)
    {
		InterfaceCanvas.Instance.layerProbeWindow.ShowLayerProbeWindow(subentities, worldSamplePosition, windowPosition);
	}

	public static List<EntityType> GetCurrentEntityTypeSelection()
    {
        return interfaceCanvas.activePlanWindow.GetEntityTypeSelection();
	}

    public static int GetCurrentTeamSelection()
    {
        return interfaceCanvas.activePlanWindow.SelectedTeam;
    }

    public static void SetActiveplanWindowToSelection(List<List<EntityType>> entityTypes, int team, List<Dictionary<EntityPropertyMetaData, string>> selectedParams)
    {
		interfaceCanvas.activePlanWindow.SetSelectedEntityTypes(entityTypes);
		interfaceCanvas.activePlanWindow.SetSelectedParameters(selectedParams);
		if (TeamManager.AreWeGameMaster)
		{
			interfaceCanvas.activePlanWindow.SelectedTeam = team;
		}
    }

    public static void SetTeamAndTypeToBasicIfEmpty()
    {
        interfaceCanvas.activePlanWindow.SetEntityTypeToBasicIfEmpty();
        if (TeamManager.AreWeGameMaster)
            interfaceCanvas.activePlanWindow.SetTeamToBasicIfEmpty();
    }

    public static void SetActivePlanWindowInteractability(bool value, bool parameterValue = false)
    {
		interfaceCanvas.activePlanWindow.SetParameterInteractability(parameterValue);
		if (!value)
		{
			interfaceCanvas.activePlanWindow.DeselectAllEntityTypes();	
			if (TeamManager.AreWeGameMaster)
				interfaceCanvas.activePlanWindow.SelectedTeam = -2;
		}
    }

	public static void SetActivePlanWindowChangeable(bool value)
	{
		interfaceCanvas.activePlanWindow.SetObjectChangeInteractable(value);
	}
}
