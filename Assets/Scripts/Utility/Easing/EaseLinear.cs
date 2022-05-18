
//THE best easing function out there
namespace MSP2050.Scripts
{
	class EaseLinear : IEasingFunction
	{
		public float Evaluate(float timeUnit)
		{
			return timeUnit;
		}
	}
}
