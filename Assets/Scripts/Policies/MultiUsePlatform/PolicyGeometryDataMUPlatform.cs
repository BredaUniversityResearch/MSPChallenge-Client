using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class PolicyGeometryDataMUPlatform : APolicyData
	{
		public bool[] options;

		public PolicyGeometryDataMUPlatform()
		{
			policy_type = PolicyManager.MU_PLATFORM_POLICY_NAME;
		}

		public PolicyGeometryDataMUPlatform(int a_options)
		{
			policy_type = PolicyManager.MU_PLATFORM_POLICY_NAME;
			options = new bool[a_options];
		}

		public override string GetJson()
		{
			//Convert from client format into server format
			return JsonConvert.SerializeObject(this);
		}

		public override bool ContentIdentical(APolicyData a_other)
		{
			PolicyGeometryDataMUPlatform other = (PolicyGeometryDataMUPlatform)a_other;
			for(int i = 0; i < options.Length; i++)
			{
				if (options[i] != other.options[i])
					return false;
			}
			return true;
		}
	}
}
