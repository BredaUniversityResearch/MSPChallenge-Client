using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpWindowsManager : MonoBehaviour
{
    private static HelpWindowsManager m_instance;
    public static HelpWindowsManager Instance => m_instance;

    private Button closeButton;
    private GameObject helpWindowObject;

    private void Awake()
	{
		m_instance = this;   
    }

	private void OnDestroy()
	{
		m_instance = null;
	}

    public void InstantiateHelpWindow(GameObject helpWindow)
    {
        helpWindowObject = Instantiate(helpWindow, this.transform);
        closeButton = GetComponentInChildren<CustomButton>();
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnCloseButtonClick()
    {
        Destroy(helpWindowObject);
    }
}
