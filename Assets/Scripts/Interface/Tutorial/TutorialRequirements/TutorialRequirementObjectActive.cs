using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementObjectActive : ATutorialRequirement
	{
		[SerializeField] private string m_objectName;
		
		public override bool EvaluateRequirement()
		{
			GameObject obj = InterfaceCanvas.Instance.GetUIObject(m_objectName);
			return obj != null && obj.activeInHierarchy;
		}
	}
}
