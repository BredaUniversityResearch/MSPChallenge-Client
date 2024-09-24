﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public abstract class AGeometryPolicyWindowContent : MonoBehaviour
	{
		public abstract void SetContent(Dictionary<Entity, string> a_values, List<Entity> a_geometry, Action<Dictionary<Entity, string>> a_changedCallback);
	}
}
