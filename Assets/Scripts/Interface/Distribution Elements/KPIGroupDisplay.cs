using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class KPIGroupDisplay : MonoBehaviour
    {
        [SerializeField] Transform m_entryParent;
        [SerializeField] Transform m_separatorLine;
        [SerializeField] TextMeshProUGUI m_name;

        public Transform EntryParent => m_entryParent;

        public void PositionSeparator()
        {
            m_separatorLine.SetSiblingIndex(m_separatorLine.parent.childCount - 2);
        }

        public void SetName(string a_name)
        {
            m_name.text = a_name;
        }
    }
}
