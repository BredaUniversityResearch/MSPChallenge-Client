using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class HelpWindowsManager : MonoBehaviour
    {
        private static HelpWindowsManager m_instance;
        public static HelpWindowsManager Instance => m_instance;

        private Button m_closeButton;
        public GameObject m_helpWindowObject;

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
            Instantiate(helpWindow, this.transform);
        }
    }
}
