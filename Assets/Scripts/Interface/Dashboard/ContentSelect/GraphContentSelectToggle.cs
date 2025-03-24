using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectToggle : MonoBehaviour
	{
		public TextMeshProUGUI m_summaryText;
		public Toggle m_detailsToggle;
		public Transform m_detailsWindowParent;
	}
}