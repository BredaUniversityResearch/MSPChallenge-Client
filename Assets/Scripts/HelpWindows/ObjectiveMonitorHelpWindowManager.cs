using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveMonitorHelpWindowManager : MonoBehaviour
{
    [SerializeField]
    CustomToggle m_defaultToggle, m_geometryToggle, m_KPIToggle;

    [SerializeField]
    GameObject m_defaultScreen, m_geometryScreen, m_KPIScreen;
    
    private void Awake()
    {
        m_defaultToggle.onValueChanged.AddListener((b) => m_defaultScreen.SetActive(b));
        m_geometryToggle.onValueChanged.AddListener((b) => m_geometryScreen.SetActive(b));
        m_KPIToggle.onValueChanged.AddListener((b) => m_KPIScreen.SetActive(b));
    }
}
