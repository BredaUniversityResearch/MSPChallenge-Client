using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class PlanDetailsTab: MonoBehaviour
	{
		[SerializeField]
		protected Toggle tabToggle;
		[SerializeField]
		private GameObject tabContainer = null;
		[SerializeField]
		protected PlanDetails planDetails;
		[SerializeField]
		protected GameObject emptyContentOverlay;

		protected bool isActive { get; private set; }
		protected virtual PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Feedback;

		public virtual void Initialise()
		{
			tabToggle.onValueChanged.AddListener(SetTabActive);
			isActive = tabToggle.isOn;
			tabContainer.SetActive(isActive);
		}

		public virtual void UpdateTabAvailability()
		{
			tabToggle.interactable = isActive || (!Main.InEditMode && !Main.Instance.PreventPlanAndTabChange);
		}

		public virtual void UpdateTabContent()
		{ }

		public void SetTabActive(bool active)
		{
			if (isActive != active)
			{
				tabContainer.SetActive(active);
				isActive = active;
				tabToggle.isOn = active;
				if (active)
				{
					planDetails.TabSelect(tabType);
					OnTabActivate();
				}
				else
				{
					OnTabDeactivate();
				}
			}
		}

		protected virtual void OnTabActivate()
		{
			UpdateTabContent();
			UpdateTabAvailability();
		}

		protected virtual void OnTabDeactivate()
		{
		}
	}
}
