using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericHelpWindow : MonoBehaviour
{
    [SerializeField]
    CustomButton m_closeButton;

    private void Awake()
    {
        m_closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnCloseButtonClick()
    {
        Destroy(gameObject);
    }
}
