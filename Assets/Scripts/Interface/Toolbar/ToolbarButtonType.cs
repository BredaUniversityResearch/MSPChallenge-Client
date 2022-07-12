using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ToolbarButtonType : MonoBehaviour
	{
		public FSM.ToolbarInput buttonType;

		protected void Start()
		{
			UIManager.ToolbarButtons.Add(this.GetComponent<Button>());
			GetComponent<Button>().onClick.AddListener(() =>
			{
				UIManager.GetToolBar().PressButton(buttonType);
			});
		}
	}
}
