
public class EaseTunableSlowFastSlow : IEasingFunction
{
	private float tuningConstant = 0.0f;

	public EaseTunableSlowFastSlow(float tuneConstant)
	{
		tuningConstant = tuneConstant;
	}

	public float Evaluate(float timeUnit)
	{
		float convertedTimeUnit = -1.0f + (timeUnit * 2.0f); //Convert to -1..1 range.
		float value = NormalisedTunableSigmoid.Evaluate(convertedTimeUnit, tuningConstant);
		return 0.5f + (value * 0.5f); // Convert from -1 .. 1 to 0..1
	}
}
