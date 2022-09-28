using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
    public class DynamicLogo : MonoBehaviour
    {
        [SerializeField] Image m_logo;
        [SerializeField] Image m_logoTag;
        [SerializeField] TextMeshProUGUI m_letter;

        public void SetContent(Color a_color, string a_letter)
        {
            m_logo.color = a_color;
			m_logoTag.color = a_color;
			m_letter.text = a_letter;
		}
    }
}
