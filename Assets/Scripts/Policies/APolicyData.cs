using System.Collections;
using UnityEngine;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class APolicyData
	{
		public string policy_type;

		public APolicyData()
		{ }

		public virtual bool ContentIdentical(APolicyData a_other)
		{
			return false;
		}

		public virtual string GetJson()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
