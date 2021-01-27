using UnityEngine;
using UnityEngine.UI;

public class ToggleOnDisable : MonoBehaviour {

    public Toggle toggle;
    public bool dir;

    void OnDisable()
    {
        toggle.isOn = dir;
    }
}