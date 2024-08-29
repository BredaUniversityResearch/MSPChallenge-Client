using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_EcoGear : AP_PopoutWindow
	{
		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			//If plan has policy data: display data

			//Otherwise: get previous data for country, display that

			//TODO: Get state for country at plan time
			//TODO: get overrides of current plan
			RefreshContent(a_content);
		}

		public void RefreshContent(Plan a_content)
		{
			if (m_APWindow.Editing)
			{
				//TODO
			}
			//TODO
		}

		public override void ApplyContent()
		{
			//TODO: apply all toggle states, not just difference
		}
	}
}
