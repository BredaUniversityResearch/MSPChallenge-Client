namespace MSP2050.Scripts
{
	public class EaseOut : EaseIn
	{
		public EaseOut(float easeExponent)
			: base(1.0f / easeExponent)
		{
		}
	}
}
