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
			if (SessionManager.Instance.MspGlobalData != null)
			{
				SetYear();
			}
			else
			{
				Main.OnGlobalDataLoaded += GlobalDataLoaded;
			}
		}

		void GlobalDataLoaded()
		{
			Main.OnGlobalDataLoaded -= GlobalDataLoaded;
			SetYear();
		}

		void SetYear()
		{
			GetComponent<TextMeshProUGUI>().text = (SessionManager.Instance.MspGlobalData.start + era * SessionManager.Instance.MspGlobalData.YearsPerEra + yearOffset).ToString();
		}
	}
}
