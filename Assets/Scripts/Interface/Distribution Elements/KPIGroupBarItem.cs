using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class KPIGroupBarItem : MonoBehaviour {

		public Image teamGraphic;
		public TextMeshProUGUI numbers;
		public TextMeshProUGUI title;
		[HideInInspector]
		public float value;
		[HideInInspector]
		public int team;
	}
}