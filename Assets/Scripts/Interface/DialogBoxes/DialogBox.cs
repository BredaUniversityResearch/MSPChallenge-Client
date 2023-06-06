using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DialogBox : MonoBehaviour
	{
		public TextMeshProUGUI title;
		public TextMeshProUGUI description;
		public Button lb;
		public Button rb;
		public TextMeshProUGUI lbDescriptor;
		public TextMeshProUGUI rbDescriptor;
		public Transform listParent;
		public GameObject listPrefab;
	}
}