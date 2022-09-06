using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace MSP2050.Scripts
{
	[CreateAssetMenu(fileName ="TutorialData", menuName ="MSP2050/TutorialData")]
	public class TutorialData : SerializedScriptableObject
	{
		[ListDrawerSettings(NumberOfItemsPerPage = 50)]
		public ATutorialDataStep[] m_steps;
	}
}