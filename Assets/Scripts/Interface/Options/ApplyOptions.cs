using UnityEngine;

public class ApplyOptions: MonoBehaviour
{
	public void Start()
	{
		GameSettings.ApplyCurrentSettings(false);
	}
}
