using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TimeManagerEraDivision : MonoBehaviour
	{
		public TimeManagerWindow timeManagerWindow;

		public TextMeshProUGUI planningText;
		public TextMeshProUGUI simulationText;
		public Slider slider;
		public Transform notchParent;
		public GameObject yearMarkerPrefab;

		int days, hours, minutes;
	
		private bool sliderTriggersUpdate = true;

		void Start()
		{
			slider.onValueChanged.AddListener((f) => Slider(f));
			SetYearsPerEra();
		}

		void SetYearsPerEra()
		{
			int yearsPerEra = SessionManager.Instance.MspGlobalData.YearsPerEra;
			slider.maxValue = yearsPerEra;
			for (int i = 1; i < yearsPerEra; i++)
			{
				RectTransform rect = Instantiate(yearMarkerPrefab, notchParent).GetComponent<RectTransform>();
				float xPos = (float)i / yearsPerEra;
				rect.anchorMin = new Vector2(xPos, 0f);
				rect.anchorMax = new Vector2(xPos, 1f);
			}
		}

		private void Slider(float val)
		{
			int planningNumber = (int)val;
			//Don't ever have 0 years planning please.
			if (planningNumber == 0)
			{
				slider.value = 1.0f;
				return;
			}

			int simulationNumber = SessionManager.Instance.MspGlobalData.YearsPerEra - (int)val;

			planningText.text = planningNumber.ToString() + " year planning";
			simulationText.text = simulationNumber.ToString() + " year simulation";

			if(sliderTriggersUpdate)
				TimeManager.Instance.EraGameTimeChanged(planningNumber * 12);
		}

		public void SetSliderValue(int value)
		{
			sliderTriggersUpdate = false;
			slider.value = value;
			planningText.text = value.ToString() + " year planning";
			simulationText.text = (SessionManager.Instance.MspGlobalData.YearsPerEra - value).ToString() + " year simulation";
			sliderTriggersUpdate = true;
		}

		public void SetEraSimulationDivision(int era, float simulationValue)
		{
			timeManagerWindow.timeline.eraBlocks[era].divisionMask.fillAmount = simulationValue;
		}
	}
}