using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class SetTextToYear : MonoBehaviour {

		public int era;
		public int yearOffset;

		private void Start()
		{
			GetComponent<TextMeshProUGUI>().text = (SessionManager.Instance.MspGlobalData.start + era * SessionManager.Instance.MspGlobalData.YearsPerEra + yearOffset).ToString();
		}
	}
}
