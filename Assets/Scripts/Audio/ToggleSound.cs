using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Toggle))]
public class ToggleSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public AudioSource OnValueChangedSound;
    public AudioSource MouseEnterSound;
    public AudioSource MouseClickSound;

    void Start()
    {
        if (OnValueChangedSound != null)
        {
            Toggle toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener((b) => OnValueChangedSound.Play());
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (MouseEnterSound != null && GetComponent<Toggle>().IsInteractable())
        {
            MouseEnterSound.Play();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (MouseClickSound != null && GetComponent<Toggle>().IsInteractable())
        {
            MouseClickSound.Play();
        }
    }
}
