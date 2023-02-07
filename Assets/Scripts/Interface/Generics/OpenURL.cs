using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class OpenURL : MonoBehaviour
	{
		[SerializeField] string m_url;

		public void OpenURLInBrowser()
		{
			Application.OpenURL(m_url);
		}
	}
}
