using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollapsableHeader : MonoBehaviour
{
    [SerializeField] private CustomButton collapseButton = null;
    [SerializeField] private GameObject childContainer = null;
    [SerializeField] private Transform collapseArrow = null;

    void Start()
    {
        collapseButton.onClick.AddListener(ToggleExpand);
    }

    public void ToggleExpand()
    {
        SetExpand(!childContainer.activeSelf);
    }

    public void SetExpand(bool expanded)
    {
        childContainer.SetActive(expanded);
        collapseArrow.localEulerAngles = expanded ? Vector3.zero : new Vector3(0, 0, 90f);
    }
}
