using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TimeManagerTimeline : MonoBehaviour
	{
		public TimeManagerEraBlock[] eraBlocks;
		public Image progress;

		public float Progress
		{
			get
			{
				return progress.fillAmount;
			}
			set
			{
				progress.fillAmount = value;

				//for (int i = 0; i < eraBlocks.Length; i++) {
				//    eraBlocks[i].IsActive = (progress.fillAmount < (i * 0.25f) + 0.25f) ? true : false;
				//}
			}
		}
	}
}