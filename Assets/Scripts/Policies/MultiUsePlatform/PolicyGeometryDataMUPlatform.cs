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
		public bool solar;
		public bool wave;
		public bool mussels;
		public bool seaweed;

		public PolicyGeometryDataMUPlatform()
		{
			policy_type = PolicyManager.MU_PLATFORM_POLICY_NAME;
		}

		public PolicyGeometryDataMUPlatform(string a_jsonData)
		{
		}

		public override string GetJson()
		{
			//Convert from client format into server format
			return JsonConvert.SerializeObject(this);
		}

		public override bool ContentIdentical(APolicyData a_other)
		{
			PolicyGeometryDataMUPlatform other = (PolicyGeometryDataMUPlatform)a_other;
			return solar == other.solar &&
				wave == other.wave &&
				mussels == other.mussels &&
				seaweed == other.seaweed;
		}
	}
}
