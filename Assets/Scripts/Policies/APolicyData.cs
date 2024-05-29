using System.Collections;
using UnityEngine;

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
	}
}