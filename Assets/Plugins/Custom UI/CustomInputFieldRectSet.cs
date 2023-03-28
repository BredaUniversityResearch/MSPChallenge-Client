using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using ColourPalette;
using System;

[RequireComponent(typeof(CustomInputField))]
class CustomInputFieldRectSet : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] List<RectTransform> m_targetRects = null;
    [SerializeField] IRectData m_rectNormal = null;
    [SerializeField] bool m_useHighlightRect = false;
    [ShowIf("m_useHighlightRect")]
    [SerializeField] IRectData m_rectHighlight = null;
    [SerializeField] bool m_useDisabledRect = false;
    [ShowIf("m_useDisabledRect")]
    [SerializeField] IRectData m_rectDisabled = null;

    CustomInputField inputField;
    bool pointerOnButton = false;
    bool isDisabled = false;
    bool colorLocked = false;

    void Start()
    {
        inputField = GetComponent<CustomInputField>();
        if (!inputField.interactable)
            SetDisabled(true);
        if (!UseDisabledColor)
            SetRectsToData(m_rectNormal);
        inputField.interactabilityChangeCallback += HandleInteractabilityChange;
    }

    private void OnDestroy()
    {
        if (inputField != null)
            inputField.interactabilityChangeCallback -= HandleInteractabilityChange;
    }

    private void HandleInteractabilityChange(bool a_newState)
    {
        if (!a_newState && !isDisabled)
        {
            SetDisabled(true);
        }
        else if (a_newState && isDisabled)
        {
            SetDisabled(false);
        }
    }

    private void SetDisabled(bool a_value)
    {
        if (!colorLocked && isDisabled && !a_value)
        {
            //No longer disabled, set to previous color
            if (pointerOnButton && Highlight)
                SetRectsToData(m_rectHighlight);
            else
                SetRectsToData(m_rectNormal);
        }
        isDisabled = a_value;
        //Disabled and color change required
        if (!colorLocked && UseDisabledColor)
            SetRectsToData(m_rectDisabled);
    }

    public void OnPointerEnter(PointerEventData a_eventData)
    {
        pointerOnButton = true;
        if (!colorLocked && Highlight && inputField.interactable)
            SetRectsToData(m_rectHighlight);
    }

    public void OnPointerExit(PointerEventData a_eventData)
    {
        pointerOnButton = false;
        if (!colorLocked && Highlight && !UseDisabledColor)
            SetRectsToData(m_rectNormal);
    }

    void SetRectsToData(IRectData a_rectData)
    {
        foreach (RectTransform rect in m_targetRects)
            a_rectData.SetRectToData(rect);
    }

    bool Highlight
    {
        get
        {
            return m_useHighlightRect;
        }
    }

    bool UseDisabledColor
    {
        get
        {
            return isDisabled && m_useDisabledRect;
        }
    }
}

public interface IRectData
{
    public void SetRectToData(RectTransform a_rect);
}

[Serializable]
public class RectDataDeltaSize : IRectData
{
    [SerializeField] Vector2 m_sizeDelta;

	public void SetRectToData(RectTransform a_rect)
	{
        a_rect.sizeDelta = m_sizeDelta;
    }
}

[Serializable]
public class RectDataAnchorOffset : IRectData
{
    [SerializeField] Vector2 m_anchorMin;
    [SerializeField] Vector2 m_anchorMax;
    [SerializeField] Vector2 m_offsetMin;
    [SerializeField] Vector2 m_offsetMax;

    public void SetRectToData(RectTransform a_rect)
    {
        a_rect.anchorMin = m_anchorMin;
        a_rect.anchorMax = m_anchorMax;
        a_rect.offsetMin = m_offsetMin;
        a_rect.offsetMax = m_offsetMax;
    }
}

[Serializable]
public class RectDataPivotOffset : IRectData
{
    [SerializeField] Vector2 m_pivot;
    [SerializeField] Vector2 m_anchorMinMax;
    [SerializeField] Vector2 m_pivotToAnchorOffset;

    public void SetRectToData(RectTransform a_rect)
    {
        a_rect.pivot = m_pivot;
        a_rect.anchorMin = m_anchorMinMax;
        a_rect.anchorMax = m_anchorMinMax;
        a_rect.anchoredPosition = m_pivotToAnchorOffset;
    }
}