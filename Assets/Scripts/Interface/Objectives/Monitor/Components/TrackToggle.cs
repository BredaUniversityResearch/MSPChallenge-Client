using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof (Toggle))]
public class TrackToggle : MonoBehaviour {

    public Toggle toggle;
    public Image[] graphics;

    void Reset()
    {
        toggle = GetComponent<Toggle>();
    }

    void Awake()
    {
        toggle.onValueChanged.AddListener(b => Fade(b));
    }

    void Fade(bool dir)
    {
        for (int i = 0; i < graphics.Length; i++) {
            toggle.interactable = false;
            graphics[i].DOFade((dir) ? 1f : 0.5f, 0.5f).OnComplete(() => toggle.interactable = true);
        }
    }
}