using UnityEngine;

namespace MSP2050.Scripts
{
	public class ScaleOverTime : MonoBehaviour {

		public float minScale, maxScale, duration;
		public int maxRepetitions = 2;

		float time;
		int repetitions;
	
		void Update ()
		{
			time += Time.deltaTime;
			if (time > duration)
			{
				time -= duration*2f;
				repetitions++;
				if(repetitions >= maxRepetitions)
				{
					Destroy(gameObject);
					return;
				}
			}
			float scale = Mathf.Lerp(minScale, maxScale, Mathf.Abs(time) / duration);
			transform.localScale = new Vector3(scale, scale, 1f);
		}
	}
}
