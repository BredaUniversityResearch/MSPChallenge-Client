using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	[CreateAssetMenu(fileName ="TutorialData", menuName ="MSP2050/TutorialData")]
	public class TutorialData : ScriptableObject
	{
		public TutorialDataSequence[] m_steps;		
	}
}