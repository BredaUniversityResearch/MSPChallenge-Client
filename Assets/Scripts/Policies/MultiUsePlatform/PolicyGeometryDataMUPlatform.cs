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

		public PolicyGeometryDataMUPlatform()
		{
			policy_type = PolicyManager.SEASONAL_CLOSURE_POLICY_NAME;
		}

		public PolicyGeometryDataMUPlatform(string a_jsonData)
		{
		}

		public override string GetJson()
		{
			//Convert from client format into server format
			MUPlatformData data = new MUPlatformData();

			//TODO: set data content

			return JsonConvert.SerializeObject(data);
		}

		public override bool ContentIdentical(APolicyData a_other)
		{
			PolicyGeometryDataMUPlatform other = (PolicyGeometryDataMUPlatform)a_other;
			//TODO
			return true;
		}

		private class MUPlatformData: APolicyData
		{
			public MUPlatformData()
			{
				policy_type = PolicyManager.MU_PLATFORM_POLICY_NAME;
			}
			public bool solar;
			public bool wave;
			public bool mussels;
			public bool seaweed;
		}
	}
}
