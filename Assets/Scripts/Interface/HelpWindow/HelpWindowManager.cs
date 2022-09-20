using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class HelpWindowManager : MonoBehaviour
    {
        private static HelpWindowManager m_instance;
        public static HelpWindowManager Instance => m_instance;

        public Button closeButton;
        public GameObject contentContainer;
        private GameObject helpPrefab;

        private void Awake()
        {
            m_instance = this;
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }

        private void OnDestroy()
        {
            m_instance = null;
        }

        public void ShowHelpWindow(GameObject helpWindowPrefab)
        {
            contentContainer.SetActive(true);
            helpPrefab = Instantiate(helpWindowPrefab, contentContainer.transform);
        }

        private void OnCloseButtonClick()
        {
            contentContainer.SetActive(false);
            Destroy(helpPrefab);
        }
    }
}
