using ColourPalette;
using UnityEngine;
using UnityEngine.UI;

public class MapScaleToolButton : MonoBehaviour
{
    public CustomButton button;
    public CustomButtonColorSet buttonColours;
    [SerializeField] private ColourAsset selectedColor = null;

    public bool selected { get; private set; }

    public void SetSelected(bool newSelected)
    {
        if (newSelected == selected)
            return;
        selected = newSelected;
        if (selected)
            buttonColours.LockToColor(selectedColor);
        else
            buttonColours.UnlockColor();
    }
}
