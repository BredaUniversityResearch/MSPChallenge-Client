using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ShowHideManager : MonoBehaviour
	{
		public KeyCode keyCode = KeyCode.Escape;
		[Header("Closed after children, start at 0")]
		public GameObject[] fixedPriorityWindowsLow;
		[Header("Closed before children, start at 0")]
		public GenericWindow[] fixedPriorityWindowsHigh;

		public ToggleGroup m_kpiToggleGroup = null;
		
		void Update()
		{
			if(Input.GetKeyDown(keyCode))       
				CloseHighestPriorityWindow();       
		}

		private void CloseHighestPriorityWindow()
		{
			//Close open dialog windows first
			if (DialogBoxManager.instance.CancelTopDialog())
				return;

			//High priority
			if (fixedPriorityWindowsHigh != null)
			{
				for (int i = 0; i < fixedPriorityWindowsHigh.Length; i++)
				{
					if (fixedPriorityWindowsHigh[i].gameObject.activeSelf)
					{
						fixedPriorityWindowsHigh[i].Hide();
						return;
					}
				}
			}

			if (m_kpiToggleGroup != null && m_kpiToggleGroup.AnyTogglesOn())
			{
				m_kpiToggleGroup.SetAllTogglesOff();
				return;
			}

			//Dynamic priority
			for (int i = 1; i < gameObject.transform.childCount + 1; i++)
			{
				GameObject tChild = gameObject.transform.GetChild(gameObject.transform.childCount - i).gameObject;
				if (tChild.gameObject.activeSelf)
				{
					tChild.SetActive(false);
					return;
				}
			}

			//Low priority
			if (fixedPriorityWindowsLow != null)
			{
				for (int i = 0; i < fixedPriorityWindowsLow.Length; i++)
				{
					if (fixedPriorityWindowsLow[i].activeSelf)
					{
						fixedPriorityWindowsLow[i].SetActive(false);
						return;
					}
				}
			}
		}
	}
}
