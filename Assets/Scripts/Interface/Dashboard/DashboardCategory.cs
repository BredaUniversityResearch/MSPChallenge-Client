using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace MSP2050.Scripts
{
	[CreateAssetMenu(fileName = "DashboardCategory", menuName = "MSP2050/DashboardCategory")]
	public class DashboardCategory : SerializedScriptableObject
	{
		public string m_name;
		public string m_displayName;
		public Sprite m_icon;
		public bool m_favorite;
	}
}