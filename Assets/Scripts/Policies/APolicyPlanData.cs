﻿using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class APolicyPlanData
	{
		public APolicyLogic logic;

		public APolicyPlanData(APolicyLogic a_logic)
		{
			logic = a_logic;
		}
	}
}