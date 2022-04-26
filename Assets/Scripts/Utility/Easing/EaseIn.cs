using UnityEngine;

namespace MSP2050.Scripts
{
	public class EaseIn : IEasingFunction
	{
		private float exponent;

		public EaseIn(float easeExponent)
		{
			exponent = easeExponent;
		}

		public float Evaluate(float timeUnit)
		{
			return Mathf.Pow(timeUnit, exponent);
		}
	}
}
