using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public abstract class ADashboardWidget : MonoBehaviour
	{
		public DashboardCategory m_category;
		public bool m_startingWidget;

		public virtual void Hide()
		{
			gameObject.SetActive(false);
		}

		public virtual void Show(bool a_favoriteLayout = false)
		{
			gameObject.SetActive(true);
		}
	}
}