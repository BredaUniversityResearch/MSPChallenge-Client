using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using ColourPalette;

[RequireComponent(typeof(CustomToggle))]
public class CustomToggleColorSet : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public List<Graphic> targetGraphics;
    public IColourContainer colorNormal;
    public IColourContainer colorSelected;
    public bool highlightIfOn;

    public bool useHighlightColor;
    [ShowIf("useHighlightColor")]
    public IColourContainer colorHighlight;
    public bool useDisabledColor;
    [ShowIf("useDisabledColor")]
    public IColourContainer colorDisabled;

    CustomToggle toggle;
    bool pointerOnToggle = false;
    bool isDisabled = false;
    private bool colorLocked = false;

    void Start()
    {
        toggle = GetComponent<CustomToggle>();
        SubscribeToAssetChange();
        if (!toggle.interactable)
            SetDisabled(true);
        if (!UseDisabledColor)
            SetGraphicSetToColor(toggle.isOn ? colorSelected : colorNormal);
        toggle.onValueChanged.AddListener((value) =>
        {
            if (!UseDisabledColor && (!highlightIfOn || !(Highlight && pointerOnToggle)))
                SetGraphicsToNormal();
        });
        toggle.interactabilityChangeCallback += HandleInteractabilityChange;
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.interactabilityChangeCallback -= HandleInteractabilityChange;
        UnSubscribeFromAssetChange();
    }

    private void HandleInteractabilityChange(bool newState)
    {
        if (!newState && !isDisabled)
        {
            SetDisabled(true);
        }
        else if (newState && isDisabled)
        {
            SetDisabled(false);
        }
    }

    private void SetDisabled(bool value)
    {
        if (isDisabled && !value)
        {
            //No longer disabled, set to previous color
            if (pointerOnToggle && Highlight)
                SetGraphicsToHighlight();
            else
                SetGraphicsToNormal();
        }
        isDisabled = value;
        //Disabled and color change required
        if (UseDisabledColor)
            SetGraphicsToDisabled();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOnToggle = true;
        if (Highlight && (highlightIfOn || !toggle.isOn) && toggle.interactable)
            SetGraphicsToHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOnToggle = false;
        if (Highlight && !UseDisabledColor)
            SetGraphicsToNormal();
    }

    public void LockToColor(IColourContainer a_color)
    {
	    SetGraphicSetToColor(a_color);
	    colorLocked = true;
    }

    public void UnlockColor()
    {
	    colorLocked = false;
	    if (toggle != null)
	    {
		    if (UseDisabledColor)
			    SetGraphicsToDisabled();
		    if (Highlight && pointerOnToggle && (highlightIfOn || !toggle.isOn))
			    SetGraphicsToHighlight();
		    else
			    SetGraphicsToNormal();
	    }
    }

	void SetGraphicsToNormal()
    {
        SetGraphicSetToColor(toggle.isOn ? colorSelected : colorNormal);
    }

    void SetGraphicsToHighlight()
    {
        SetGraphicSetToColor(colorHighlight);
    }

    void SetGraphicsToDisabled()
    {
        SetGraphicSetToColor(colorDisabled);
    }

    void SetGraphicSetToColor(IColourContainer colorAsset)
    {
	    if (colorLocked)
		    return;
        foreach (Graphic g in targetGraphics)
            g.color = colorAsset.GetColour();
    }

    void SubscribeToAssetChange()
    {
        if (Application.isPlaying)
        {
            colorNormal?.SubscribeToChanges(OnNormalColourAssetChanged);
            if (useHighlightColor)
                colorHighlight?.SubscribeToChanges(OnHighlightColourAssetChanged);
            if (useDisabledColor)
                colorDisabled?.SubscribeToChanges(OnDisabledColourAssetChanged);
        }
    }

    void UnSubscribeFromAssetChange()
    {
        if (Application.isPlaying)
        {
            colorNormal?.UnSubscribeFromChanges(OnNormalColourAssetChanged);
            if (useHighlightColor)
                colorHighlight?.UnSubscribeFromChanges(OnHighlightColourAssetChanged);
            if (useDisabledColor)
                colorDisabled?.UnSubscribeFromChanges(OnDisabledColourAssetChanged);
        }
    }

    void OnNormalColourAssetChanged(Color newColour)
    {
        if (!UseDisabledColor && !(Highlight && pointerOnToggle) && !toggle.isOn)
            SetGraphicsToNormal();
    }

    void OnSelectedColourAssetChanged(Color newColour)
    {
        if (!UseDisabledColor && (!highlightIfOn || !(Highlight && pointerOnToggle)) && toggle.isOn)
            SetGraphicsToNormal();
    }

    void OnHighlightColourAssetChanged(Color newColour)
    {
        if (Highlight && pointerOnToggle && (highlightIfOn || !toggle.isOn) && !UseDisabledColor)
            SetGraphicsToHighlight();
    }

    void OnDisabledColourAssetChanged(Color newColour)
    {
        if (UseDisabledColor)
            SetGraphicsToDisabled();
    }

    bool Highlight
    {
        get
        {
            return useHighlightColor;
        }
    }

    bool UseDisabledColor
    {
        get
        {
            return isDisabled && useDisabledColor;
        }
    }
}