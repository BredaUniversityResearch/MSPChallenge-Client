using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpWindowsManager : MonoBehaviour
{
    private static HelpWindowsManager m_instance;
    public static HelpWindowsManager Instance => m_instance;

    private Button m_closeButton;
    private GameObject m_helpWindowObject;

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
        m_helpWindowObject = Instantiate(helpWindow, this.transform);
        m_closeButton = GetComponentInChildren<CustomButton>();
        m_closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnCloseButtonClick()
    {
        Destroy(m_helpWindowObject);
    }
}
