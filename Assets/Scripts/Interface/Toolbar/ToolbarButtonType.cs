using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ToolbarButtonType : MonoBehaviour
	{
		public FSM.ToolbarInput buttonType;

		protected void Start()
		{
			Button b = GetComponent<Button>();
			InterfaceCanvas.Instance.RegisterToolbarButton(b);
			b.onClick.AddListener(() =>
			{
				InterfaceCanvas.Instance.toolBar.PressButton(buttonType);
			});
		}
	}
}
