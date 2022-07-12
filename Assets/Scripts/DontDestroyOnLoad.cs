using UnityEngine;

namespace MSP2050.Scripts
{
	public class DontDestroyOnLoad : MonoBehaviour
	{
		// Use this for initialization
		void Start()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
