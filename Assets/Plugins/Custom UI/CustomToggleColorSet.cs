using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using ColourPalette;

[RequireComponent(typeof(CustomToggle))]
public class CustomToggleColorSet : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ReSharper disable once InconsistentNaming // serialized by odin
    public List<Graphic> targetGraphics;
    // ReSharper disable once InconsistentNaming // serialized by odin
    public IColourContainer colorNormal;
    // ReSharper disable once InconsistentNaming // serialized by odin
    public IColourContainer colorSelected;
    // ReSharper disable once InconsistentNaming // serialized by odin
    public bool highlightIfOn;

    // ReSharper disable once InconsistentNaming // serialized by odin
    public bool useHighlightColor;
    [ShowIf("useHighlightColor")]
    // ReSharper disable once InconsistentNaming // serialized by odin
    public IColourContainer colorHighlight;
	[ShowIf("useHighlightColor")]
    // ReSharper disable once InconsistentNaming // serialized by odin
	public bool separateSelectedHighlight;
	[ShowIf("separateSelectedHighlight")]
    // ReSharper disable once InconsistentNaming // serialized by odin
	public IColourContainer colorSelectedHighlight;
    // ReSharper disable once InconsistentNaming // serialized by odin
	public bool useDisabledColor;
    [ShowIf("useDisabledColor")]
    // ReSharper disable once InconsistentNaming // serialized by odin
    public IColourContainer colorDisabled;

    private CustomToggle m_toggle;
    private bool m_pointerOnToggle = false;
    private bool m_isDisabled = false;
    private bool m_colorLocked = false;

    private bool Highlight => useHighlightColor;

    private bool UseDisabledColor => m_isDisabled && useDisabledColor;    
    
    private void Start()
    {
        m_toggle = GetComponent<CustomToggle>();
        SubscribeToAssetChange();
        if (!m_toggle.interactable)
            SetDisabled(true);
        if (!UseDisabledColor)
            SetGraphicSetToColor(m_toggle.isOn ? colorSelected : colorNormal);
        m_toggle.onValueChanged.AddListener((a_value) => {
            if (UseDisabledColor)
                return;
            if (!highlightIfOn || !(Highlight && m_pointerOnToggle))
                SetGraphicsToNormal();
            else if(highlightIfOn && separateSelectedHighlight && useHighlightColor && m_pointerOnToggle)
                SetGraphicsToHighlight(a_value);
        });
        m_toggle.interactabilityChangeCallback += HandleInteractabilityChange;
    }

    private void OnDestroy()
    {
        if (m_toggle != null)
            m_toggle.interactabilityChangeCallback -= HandleInteractabilityChange;
        UnSubscribeFromAssetChange();
    }

    private void HandleInteractabilityChange(bool a_newState)
    {
        if (!a_newState && !m_isDisabled)
        {
            SetDisabled(true);
        }
        else if (a_newState && m_isDisabled)
        {
            SetDisabled(false);
        }
    }

    private void SetDisabled(bool a_value)
    {
        if (m_isDisabled && !a_value)
        {
            //No longer disabled, set to previous color
            if (m_pointerOnToggle && Highlight)
                SetGraphicsToHighlight(m_toggle.isOn);
            else
                SetGraphicsToNormal();
        }
        m_isDisabled = a_value;
        //Disabled and color change required
        if (UseDisabledColor)
            SetGraphicsToDisabled();
    }

    public void OnPointerEnter(PointerEventData a_eventData)
    {
        m_pointerOnToggle = true;
        if (Highlight && (highlightIfOn || !m_toggle.isOn) && m_toggle.interactable)
            SetGraphicsToHighlight(m_toggle.isOn);
    }

    public void OnPointerExit(PointerEventData a_eventData)
    {
        m_pointerOnToggle = false;
        if (Highlight && !UseDisabledColor)
            SetGraphicsToNormal();
    }

    public void LockToColor(IColourContainer a_color)
    {
	    SetGraphicSetToColor(a_color);
	    m_colorLocked = true;
    }

    public void UnlockColor()
    {
	    m_colorLocked = false;
        if (m_toggle == null)
            return;
        if (UseDisabledColor)
            SetGraphicsToDisabled();
        if (Highlight && m_pointerOnToggle && (highlightIfOn || !m_toggle.isOn))
            SetGraphicsToHighlight(m_toggle.isOn);
        else
            SetGraphicsToNormal();
    }

    private void SetGraphicsToNormal()
    {
        SetGraphicSetToColor(m_toggle.isOn ? colorSelected : colorNormal);
    }

    private void SetGraphicsToHighlight(bool a_toggleOn)
    {
        if(separateSelectedHighlight && a_toggleOn)
            SetGraphicSetToColor(colorSelectedHighlight);
        else
            SetGraphicSetToColor(colorHighlight);
    }

    private void SetGraphicsToDisabled()
    {
        SetGraphicSetToColor(colorDisabled);
    }

    private void SetGraphicSetToColor(IColourContainer a_colorAsset)
    {
	    if (m_colorLocked)
		    return;
        foreach (Graphic g in targetGraphics)
            g.color = a_colorAsset.GetColour();
    }

    private void SubscribeToAssetChange()
    {
        if (!Application.isPlaying)
            return;
        colorNormal?.SubscribeToChanges(OnNormalColourAssetChanged);
        if (useHighlightColor)
            colorHighlight?.SubscribeToChanges(OnHighlightColourAssetChanged);
        if (separateSelectedHighlight)
            colorSelectedHighlight?.SubscribeToChanges(OnHighlightColourAssetChanged);
        if (useDisabledColor)
            colorDisabled?.SubscribeToChanges(OnDisabledColourAssetChanged);
    }

    private void UnSubscribeFromAssetChange()
    {
        if (!Application.isPlaying)
            return;
        colorNormal?.UnSubscribeFromChanges(OnNormalColourAssetChanged);
        if (useHighlightColor)
            colorHighlight?.UnSubscribeFromChanges(OnHighlightColourAssetChanged);
        if (separateSelectedHighlight)
            colorSelectedHighlight?.UnSubscribeFromChanges(OnHighlightColourAssetChanged);
        if (useDisabledColor)
            colorDisabled?.UnSubscribeFromChanges(OnDisabledColourAssetChanged);
    }

    private void OnNormalColourAssetChanged(Color a_newColour)
    {
        if (!UseDisabledColor && !(Highlight && m_pointerOnToggle) && !m_toggle.isOn)
            SetGraphicsToNormal();
    }

    private void OnSelectedColourAssetChanged(Color a_newColour)
    {
        if (!UseDisabledColor && (!highlightIfOn || !(Highlight && m_pointerOnToggle)) && m_toggle.isOn)
            SetGraphicsToNormal();
    }

    private void OnHighlightColourAssetChanged(Color a_newColour)
    {
        if (Highlight && m_pointerOnToggle && (highlightIfOn || !m_toggle.isOn) && !UseDisabledColor)
            SetGraphicsToHighlight(m_toggle.isOn);
    }

    private void OnDisabledColourAssetChanged(Color a_newColour)
    {
        if (UseDisabledColor)
            SetGraphicsToDisabled();
    }
}
