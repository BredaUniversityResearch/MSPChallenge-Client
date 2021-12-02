using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof (Selectable))]
public class SwitchSelectableColors : MonoBehaviour {

    public Selectable selectable;
    public Color normal;
    public Color highlighted;

    void Reset()
    {
        selectable = GetComponent<Selectable>();
    }

    void Start()
    {
        ColorBlock colBlock = selectable.colors;

        normal = colBlock.normalColor;
        highlighted = colBlock.highlightedColor;
    }

    public void SwitchColors(bool dir)
    {
        ColorBlock colBlock = selectable.colors;

        colBlock.normalColor = (dir) ? highlighted : normal;
        colBlock.highlightedColor = (dir) ? normal : highlighted;

        selectable.colors = colBlock;
    }
}