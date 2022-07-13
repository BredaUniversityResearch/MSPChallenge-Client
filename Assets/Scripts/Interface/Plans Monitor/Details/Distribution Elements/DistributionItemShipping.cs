using UnityEngine;

namespace MSP2050.Scripts
{
	public class DistributionItemShipping : DistributionItem
	{
		[SerializeField]
		ValueConversionUnit unitConversion;

		void Awake()
		{
			Initialise();
		}

		public override void ToText(float val, float valueAtOne, bool onlyScaleDisplayValue = false)
		{
			//SetValueText(string.Format(valueTextFormat, val * valueAtOne));
			SetValueText(unitConversion.ConvertUnit(val * valueAtOne).FormatAsString());
		}

		protected override void OnValueTextEditConfirm(string newValueText)
		{
			float newValue;
			unitConversion.ParseUnit(newValueText, out newValue);
			newValue = Mathf.Clamp(newValue / PlanManager.Instance.shippingDisplayScale, 0.0f, 1.0f);
			slider.Value = newValue;
		}
	}
}