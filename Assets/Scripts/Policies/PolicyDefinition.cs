using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyDefinition
	{
		public string m_name;
		public string m_displayName;
		public bool m_geometryPolicy = false;
		public Type m_settingsType;
		public Type m_generalUpdateType;
		public Type m_planUpdateType;
		public Type m_logicType;
		public GameObject m_windowPrefab;
		public Sprite m_policyIcon;
	}
}