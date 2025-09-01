using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ImmersiveSessionDetailsWindow : MonoBehaviour
	{


		public void Initialise()
		{

		}

		public void SetToSession(ImmersiveSession a_session)
		{ 
			//TODO
			gameObject.SetActive(true);
		}

		public void StartCreatingNewSession()
		{
			//TODO
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}
