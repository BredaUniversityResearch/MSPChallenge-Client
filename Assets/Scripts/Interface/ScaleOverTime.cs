using UnityEngine;
using System.Collections;

public class ScaleOverTime : MonoBehaviour {

	public float minScale, maxScale, duration;
	float time;
	
	void Update ()
	{
		time += Time.deltaTime;
		if (time > duration)
			time -= duration*2f;
		float scale = Mathf.Lerp(minScale, maxScale, Mathf.Abs(time) / duration);
		transform.localScale = new Vector3(scale, scale, 1f);
	}
}
