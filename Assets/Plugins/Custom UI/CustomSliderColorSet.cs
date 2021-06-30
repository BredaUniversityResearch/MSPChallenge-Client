using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using ColourPalette;

[RequireComponent(typeof(ICustomSlider))]
class CustomSliderColorSet : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public List<Graphic> targetGraphics = null;
	public IColourContainer colorNormal = null;
	public bool useHighlightColor = false;
	[ShowIf("useHighlightColor")]
	public IColourContainer colorHighlight = null;
	public bool useDisabledColor = false;
	[ShowIf("useDisabledColor")]
	public IColourContainer colorDisabled = null;

	ICustomSlider slider;
	bool pointerOnButton = false;
	bool isDisabled = false;
	bool colorLocked = false;

	void Start()
	{
		slider = GetComponent<ICustomSlider>();
		SubscribeToAssetChange();
		if (!slider.Interactable)
			SetDisabled(true);
		if (!UseDisabledColor)
			SetGraphicsToNormal();
		slider.AddInteractabilityChangeCallback(HandleInteractabilityChange);
	}

	private void OnDestroy()
	{
		if (slider != null)
			slider.RemoveInteractabilityChangeCallback(HandleInteractabilityChange);
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
		if (!colorLocked && isDisabled && !value)
		{
			//No longer disabled, set to previous color
			if (pointerOnButton && Highlight)
				SetGraphicsToHighlight();
			else
				SetGraphicsToNormal();
		}
		isDisabled = value;
		//Disabled and color change required
		if (!colorLocked && UseDisabledColor)
			SetGraphicsToDisabled();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerOnButton = true;
		if (!colorLocked && Highlight && slider.Interactable)
			SetGraphicsToHighlight();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pointerOnButton = false;
		if (!colorLocked && Highlight && !UseDisabledColor)
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

