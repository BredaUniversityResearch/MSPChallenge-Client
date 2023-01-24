using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DistributionItem : MonoBehaviour
	{
		public const float SNAP_RANGE = 0.01f;

		[SerializeField] private Image m_teamBubble;
		[SerializeField] private TextMeshProUGUI m_teamNameText = null;
		[SerializeField] private TextMeshProUGUI valueText = null;
		[SerializeField] private CustomInputField valueTextInput = null;
		[SerializeField] protected DistributionSlider slider = null;
		[SerializeField] protected string valueTextFormat = "{0}";

		[HideInInspector] public bool changed;
		[HideInInspector] public AbstractDistributionGroup group;

		private bool initialised = false;

		public virtual void Initialise()
		{
			if (initialised)
				return;
			initialised = true;
		}

		//For returning results to energy/ecology
		public int Country
		{
			get;
			private set;
		}

		void Awake()
		{
			Initialise();
		}

		void Start()
		{
			if (valueTextInput != null)
			{
				valueTextInput.onEndEdit.AddListener(OnValueTextEditConfirm);
			}
		}

		private void OnDestroy()
		{
			if (valueTextInput != null)
			{
				valueTextInput.onEndEdit.RemoveListener(OnValueTextEditConfirm);
			}
		}

		public virtual void ToText(float val, float valueAtOne, bool onlyScaleDisplayValue = false)
		{
			SetValueText(string.Format(valueTextFormat, (val * valueAtOne).Abbreviated()));
		}

		public void SetValueText(string newText)
		{
			if (valueTextInput != null)
			{
				valueTextInput.text = newText;
			}
			else
			{
				valueText.text = newText;
			}
		}

		public void SetValueTooltip(string text)
		{
			AddTooltip tooltip = valueText.gameObject.GetComponent<AddTooltip>();
			tooltip.text = text;
		}

		protected virtual void OnValueTextEditConfirm(string newValueText)
		{
			float newValue;
			if (float.TryParse(newValueText, Localisation.FloatNumberStyle, Localisation.NumberFormatting, out newValue))
			{
				newValue = Mathf.Clamp(newValue / SimulationLogicMEL.Instance.fishingDisplayScale, 0.0f, 1.0f);
				slider.Value = newValue;
			}
		}

		public virtual void UpdateNewValueSlider()
		{
			MarkAsChanged(slider.IsChanged());

			group.UpdateDistributionItem(this, slider.Value);
		}

		/// <summary>
		/// Mark UI and slider as changed
		/// </summary>
		protected virtual void MarkAsChanged(bool isChanged)
		{
			if (isChanged)
			{
				valueText.fontStyle = FontStyles.Bold;
				changed = true;
				//slider.MarkAsChanged(true);
			}
			else
			{
				valueText.fontStyle = FontStyles.Normal;
				changed = false;
				//slider.MarkAsChanged(false);
			}
		}

		public virtual void SetSliderInteractability(bool value)
		{
			slider.SetInteractablity(value);
			if(valueTextInput != null)
				valueTextInput.interactable = value;
		}

		public virtual float GetDistributionValue()
		{
			return slider.Value;
		}

		public virtual void SetValue(float currentValue)
		{
			slider.ignoreSliderCallback = true;
			slider.Value = currentValue;
			slider.ignoreSliderCallback = false;
			MarkAsChanged(slider.IsChanged());
		}

		public void SetOldValue(float oldValue)
		{
			slider.SetOldValue(oldValue);
		}

		public void SetMaximum(float maxValue)
		{
			slider.MaxValue = maxValue;
		}

		public void SetTeam(Team a_team)
		{
			m_teamBubble.color = a_team.color;
			m_teamNameText.text = a_team.name;
			Country = a_team.ID;
		}
	}
}