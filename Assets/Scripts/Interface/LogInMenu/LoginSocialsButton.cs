using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	[RequireComponent(typeof(Button))]
	public class LoginSocialsButton : MonoBehaviour
	{
		[SerializeField] private string m_link;
		void Start()
		{
			GetComponent<Button>().onClick.AddListener(OnButtonClick);
		}

		private void OnButtonClick()
		{
			Application.OpenURL(m_link);
		}
	}
}
