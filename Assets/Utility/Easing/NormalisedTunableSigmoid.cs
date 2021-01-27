using UnityEngine;

public static class NormalisedTunableSigmoid
{
	/// <summary>
	/// Returns value on an S-curve that is defined by the constant a_Constant
	/// https://www.desmos.com/calculator/aksjkh9das
	/// </summary>
	/// <param name="timeUnit">Time unit -1.0 .. 1.0</param>
	/// <param name="constant">Tuning constant -1.0 .. 1.0</param>
	/// <returns></returns>
	public static float Evaluate(float timeUnit, float constant)
	{
		return (timeUnit - (timeUnit * constant)) / (constant - Mathf.Abs(timeUnit) * 2.0f * constant + 1);
	}
}
