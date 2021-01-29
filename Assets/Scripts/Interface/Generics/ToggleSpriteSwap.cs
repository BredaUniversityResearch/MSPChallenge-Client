using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof (Toggle))]
public class ToggleSpriteSwap : MonoBehaviour {

    [Header("Swap sprite on icon when toggle is used")]
    public Toggle toggle;
    public Image icon;
    public Sprite off, on;

    void Reset()
    {
        toggle = GetComponent<Toggle>();
    }

    void Start () {
        toggle.onValueChanged.AddListener((b) => icon.sprite = (b) ? on : off);
	}

    public void SetSpriteManually(bool state)
    {
        icon.sprite = state ? on : off;
    }
}