using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using ColourPalette;

[RequireComponent(typeof(CustomButton))]
public class CustomButtonColorSet : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public List<Graphic> targetGraphics = null;
    public IColourContainer colorNormal = new ConstColour(Color.white);
    public bool useHighlightColor;
    [ShowIf("useHighlightColor")]
    public IColourContainer colorHighlight;
    public bool useDisabledColor;
    [ShowIf("useDisabledColor")]
    public IColourContainer colorDisabled;

    CustomButton button;
    bool pointerOnButton = false;
    bool isDisabled = false;
    bool colorLocked = false;

    void Start()
    {
        button = GetComponent<CustomButton>();
        SubscribeToAssetChange();
        if (!button.interactable)
            SetDisabled(true);
        if (!UseDisabledColor)
            SetGraphicsToNormal();
        button.interactabilityChangeCallback += HandleInteractabilityChange;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.interactabilityChangeCallback -= HandleInteractabilityChange;
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
            if (pointerOnButton && Highlight)
                SetGraphicsToHighlight();
            else
                SetGraphicsToNormal();
        }
        isDisabled = value;
        //Disabled and color change required
        if (UseDisabledColor)
            SetGraphicsToDisabled();
    }

	public void OnEnable()
	{
		pointerOnButton = false;
		if (Highlight && !UseDisabledColor)
		{
			SetGraphicsToNormal();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOnButton = true;
        if (Highlight && button.interactable)
            SetGraphicsToHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOnButton = false;
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
        if (UseDisabledColor)
	        SetGraphicsToDisabled();
        if (Highlight && pointerOnButton)
	        SetGraphicsToHighlight();
        else
	        SetGraphicsToNormal();
    }

    void SetGraphicsToNormal()
    {
        SetGraphicSetToColor(colorNormal);
    }

    void SetGraphicsToHighlight()
    {
        SetGraphicSetToColor(colorHighlight);
    }

    void SetGraphicsToDisabled()
    {
        SetGraphicSetToColor(colorDisabled);
    }

    void SetGraphicSetToColor(IColourContainer colourAsset)
    {
	    if (colorLocked)
		    return;
        foreach (Graphic g in targetGraphics)
            g.color = colourAsset.GetColour();
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
        if (!UseDisabledColor && !(Highlight && pointerOnButton))
            SetGraphicsToNormal();
    }

    void OnHighlightColourAssetChanged(Color newColour)
    {
        if (Highlight && pointerOnButton && !UseDisabledColor)
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

