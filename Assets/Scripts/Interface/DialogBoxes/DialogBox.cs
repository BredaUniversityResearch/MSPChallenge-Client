using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DialogBox : MonoBehaviour
	{
		public RectTransform thisRectTrans;
		public TextMeshProUGUI title;
		public TextMeshProUGUI description;
		public Button lb;
		public Button rb;
		public TextMeshProUGUI lbDescriptor;
		public TextMeshProUGUI rbDescriptor;
		public GameObject leftButtonContainer;
		public GameObject modalBackground;
	}
}