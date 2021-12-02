using ColourPalette;
using UnityEngine;
using UnityEngine.UI;

public class LayerButton : MonoBehaviour {

    public DoubleClickButton button;
    public CustomButtonColorSet outlineColourSet;
    public ColourAsset accentColour;
    public Image icon;
	
    /// <summary>
    /// Hide the button
    /// </summary>
    public void SetVisibility(bool toggle)
    {
        gameObject.SetActive(toggle);
    }

    public void SetSelectedVisuals(bool selected)
    {
		if(selected)
			outlineColourSet.LockToColor(accentColour);
		else
			outlineColourSet.UnlockColor();
	}
}
