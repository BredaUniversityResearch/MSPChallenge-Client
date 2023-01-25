using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPIValueDisplay: MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI value = null;

		[SerializeField]
		private TextMeshProUGUI unit = null;

		[SerializeField, Tooltip("Argument 0 will be the relative value when the tooltip is updated, formatted as string.")]
		private string tooltipFormat = "Current Value ({0})";

		private void UpdateValue(string valueText, string unitText)
		{
			value.text = valueText + " " + unitText;
			//unit.text = unitText;
		}

		public void UpdateValue(IConvertedUnit convertedUnit)
		{
			UpdateValue(convertedUnit.FormatValue(), convertedUnit.Unit);
		}

		public void UpdateTooltip(string relativeValueString)
		{
			AddTooltip tooltip = gameObject.GetComponent<AddTooltip>();
			tooltip.text = string.Format(tooltipFormat, relativeValueString);
		}
	}
}
